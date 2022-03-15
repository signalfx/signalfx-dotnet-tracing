// We want to use std::min, not the windows.h macro
#define NOMINMAX
#include "always_on_profiler.h"
#include "logger.h"
#include <chrono>
#include <map>
#include <algorithm>
#ifndef _WIN32
  #include <pthread.h>
#endif

constexpr auto max_func_name_len = 256UL;
constexpr auto max_class_name_len = 512UL;
constexpr auto max_string_length = 512UL;


constexpr auto max_codes_per_buffer = 10 * 1000;

// If you change this, consider ThreadSampler.cs too
constexpr auto samples_buffer_maximum_size = 200 * 1024;

constexpr auto samples_buffer_default_size = 20 * 1024;

// If you change these, change ThreadSampler.cs too
constexpr auto default_sample_period = 10000;
constexpr auto minimum_sample_period = 1000;

// FIXME make configurable (hidden)?
// These numbers were chosen to keep total overhead under 1 MB of RAM in typical cases (name lengths being the biggest
// variable)
constexpr auto max_function_name_cache_size = 6000;
constexpr auto max_class_name_cache_size = 1000;

// If you squint you can make out that the original bones of this came from sample code provided by the dotnet project:
// https://github.com/dotnet/samples/blob/2cf486af936261b04a438ea44779cdc26c613f98/core/profiling/stacksampling/src/sampler.cpp
// That stacksampling project is worth reading for a simpler (though higher overhead) take on thread sampling.

static std::mutex bufferLock = std::mutex();
static std::vector<unsigned char>* bufferA;
static std::vector<unsigned char>* bufferB;

static std::mutex threadSpanContextLock;
static std::unordered_map<ThreadID, trace::ThreadSpanContext> threadSpanContextMap;

static ICorProfilerInfo* profilerInfo; // After feature sets settle down, perhaps this should be refactored and have a single static instance of ThreadSampler

// Dirt-simple backpressure system to save overhead if managed code is not reading fast enough
bool ThreadSampling_ShouldProduceThreadSample()
{
    std::lock_guard<std::mutex> guard(bufferLock);
    return bufferA == nullptr || bufferB == nullptr;
}
void ThreadSampling_RecordProducedThreadSample(std::vector<unsigned char>* buf)
{
    std::lock_guard<std::mutex> guard(bufferLock);
    if (bufferA == nullptr) {
        bufferA = buf;
    } else if (bufferB == nullptr) {
        bufferB = buf;
    } else {
        trace::Logger::Warn("Unexpected buffer drop in ThreadSampling_RecordProducedThreadSample");
        delete buf; // needs to be dropped now
    }
}
// Can return 0 if none are pending
int32_t ThreadSampling_ConsumeOneThreadSample(int32_t len, unsigned char* buf)
{
    if (len <= 0 || buf == nullptr)
    {
        trace::Logger::Warn("Unexpected 0/null buffer to ThreadSampling_ConsumeOneThreadSample");
        return 0;
    }
    std::vector<unsigned char>* toUse = nullptr;
    {
        std::lock_guard<std::mutex> guard(bufferLock);
        if (bufferA != nullptr)
        {
            toUse = bufferA;
            bufferA = nullptr;
        }
        else if (bufferB != nullptr)
        {
            toUse = bufferB;
            bufferB = nullptr;
        }
    }
    if (toUse == nullptr)
    {
        return 0;
    }
    const size_t toUseLen = static_cast<int>(std::min(toUse->size(), static_cast<size_t>(len)));
    memcpy(buf, toUse->data(), toUseLen);
    delete toUse;
    return static_cast<int32_t>(toUseLen);
}

namespace trace
{

/*
* The thread samples buffer format is optimized for single-pass and efficient writing by the native sampling thread (which
* has paused the CLR)
*
* It uses a simple byte-opcode format with fairly standard binary encoding of values.  It is entirely positional but is at least versioned
* so that mismatched components (native writer and managed reader) will not emit nonsense.
*
* ints, shorts, and 64-bit longs are written in big-endian format; strings are written as 2-byte-length-prefixed standard windows utf-16 strings
*
* I would write out the "spec" for this format here, but it essentially maps to the code
* (e.g., 0x01 is StartBatch, which is followed by an int versionNumber and a long captureStartTimeInMillis)
*
* The bulk of the data is an (unknown length) array of frame strings, which are represented as coded strings in each buffer.
* Each used string is given a code (starting at 1) - using an old old inline trick, codes are introduced by writing the code as a
* negative number followed by the definition of the string (length-prefixed) that maps to that code.  Later uses of the code
* simply use the 2-byte (positive) code, meaning frequently used strings will take only 2 bytes apiece.  0 is reserved for "end of list"
* since the number of frames is not known up-front.
* 
* Each buffer can be parsed/decoded independently; the codes and the LRU NameCache are not related.
*/

// defined op codes
constexpr auto thread_samples_start_batch = 0x01;
constexpr auto thread_samples_start_sample = 0x02;
constexpr auto thread_samples_end_batch = 0x06;
constexpr auto thread_samples_final_stats = 0x07;

constexpr auto current_thread_samples_buffer_version = 1;

ThreadSamplesBuffer::ThreadSamplesBuffer(std::vector<unsigned char>* buf) : buffer(buf)
{
}
ThreadSamplesBuffer ::~ThreadSamplesBuffer()
{
    buffer = nullptr; // specifically don't delete as this is done by RecordProduced/ConsumeOneThreadSample
}

#define CHECK_SAMPLES_BUFFER_LENGTH() {  if (buffer->size() >= samples_buffer_maximum_size) { return; } }

void ThreadSamplesBuffer::StartBatch()
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeByte(thread_samples_start_batch);
    writeInt(current_thread_samples_buffer_version);
    const auto ms =
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch());
    writeInt64((int64_t) ms.count());
}

void ThreadSamplesBuffer::StartSample(ThreadID id, ThreadState* state, const ThreadSpanContext& context)
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeByte(thread_samples_start_sample);
    writeInt(context.managedThreadId);
    writeInt(state->nativeId);
    writeString(state->threadName);
    writeInt64(context.traceIdHigh);
    writeInt64(context.traceIdLow);
    writeInt64(context.spanId);
    // Feature possibilities: (managed/native) thread priority, cpu/wait times, etc.
}
void ThreadSamplesBuffer::RecordFrame(FunctionID fid, shared::WSTRING& frame)
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeCodedFrameString(fid, frame);
}
void ThreadSamplesBuffer::EndSample()
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeShort(0);
}
void ThreadSamplesBuffer::EndBatch()
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeByte(thread_samples_end_batch);
}
void ThreadSamplesBuffer::WriteFinalStats(const SamplingStatistics& stats)
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeByte(thread_samples_final_stats);
    writeInt(stats.microsSuspended);
    writeInt(stats.numThreads);
    writeInt(stats.totalFrames);
    writeInt(stats.nameCacheMisses);
}

void ThreadSamplesBuffer::writeCodedFrameString(FunctionID fid, shared::WSTRING& str)
{
    const auto found = codes.find(fid);
    if (found != codes.end())
    {
        writeShort(found->second);
    }
    else
    {
        const int code = static_cast<int>(codes.size()) + 1;
        if (codes.size() + 1 < max_codes_per_buffer)
        {
            codes[fid] = code;
        }
        writeShort(-code); // note negative sign indicating definition of code
        writeString(str);
    }
}
void ThreadSamplesBuffer::writeShort(int16_t val)
{
    buffer->push_back(((val >> 8) & 0xFF));
    buffer->push_back(val & 0xFF);
}
void ThreadSamplesBuffer::writeInt(int32_t val)
{
    buffer->push_back(((val >> 24) & 0xFF));
    buffer->push_back(((val >> 16) & 0xFF));
    buffer->push_back(((val >> 8) & 0xFF));
    buffer->push_back(val & 0xFF);
}
void ThreadSamplesBuffer::writeString(const shared::WSTRING& str)
{
    // limit strings to a max length overall; this prevents (e.g.) thread names or
    // any other miscellaneous strings that come along from blowing things out
    const short usedLen = static_cast<short>(std::min(str.length(), static_cast<size_t>(max_string_length)));
    writeShort(usedLen);
    // odd bit of casting since we're copying bytes, not wchars
    const auto strBegin = reinterpret_cast<const unsigned char*>(&str.c_str()[0]);
    // possible endian-ness assumption here; unclear how the managed layer would decode on big endian platforms
    buffer->insert(buffer->end(), strBegin, strBegin + usedLen * 2);
}
void ThreadSamplesBuffer::writeByte(unsigned char b)
{
    buffer->push_back(b);
}
void ThreadSamplesBuffer::writeInt64(int64_t val)
{
    buffer->push_back(((val >> 56) & 0xFF));
    buffer->push_back(((val >> 48) & 0xFF));
    buffer->push_back(((val >> 40) & 0xFF));
    buffer->push_back(((val >> 32) & 0xFF));
    buffer->push_back(((val >> 24) & 0xFF));
    buffer->push_back(((val >> 16) & 0xFF));
    buffer->push_back(((val >> 8) & 0xFF));
    buffer->push_back(val & 0xFF);
}

class SamplingHelper
{
public:
    // These are permanent parts of the helper object
    ICorProfilerInfo10* info10 = nullptr;
    NameCache functionNameCache;
    NameCache classNameCache;
    // These cycle every sample and/or are owned externally
    ThreadSamplesBuffer* curWriter = nullptr;
    std::vector<unsigned char>* curBuffer = nullptr;
    SamplingStatistics stats;

    SamplingHelper() : functionNameCache(max_function_name_cache_size), classNameCache(max_class_name_cache_size)
    {
    }

    bool AllocateBuffer()
    {
        const bool should = ThreadSampling_ShouldProduceThreadSample();
        if (!should)
        {
            return should;
        }
        stats = SamplingStatistics();
        curBuffer = new std::vector<unsigned char>();
        curBuffer->reserve(samples_buffer_default_size);
        curWriter = new ThreadSamplesBuffer(curBuffer);
        return should;
    }
    void PublishBuffer()
    {
        ThreadSampling_RecordProducedThreadSample(curBuffer);
        delete curWriter;
        curWriter = nullptr;
        curBuffer = nullptr;
        stats = SamplingStatistics();
    }

private:
    void GetClassName(ClassID classId, shared::WSTRING& result)
    {
        ModuleID modId;
        mdTypeDef classToken;
        ClassID parentClassID;

        if (classId == 0)
        {
            Logger::Debug("Zero (0) classId passed to GetClassName");
            result.append(WStr("Unknown"));
            return;
        }

        HRESULT hr = info10->GetClassIDInfo2(classId, &modId, &classToken, &parentClassID, 0, nullptr, nullptr);
        if (CORPROF_E_CLASSID_IS_ARRAY == hr)
        {
            // We have a ClassID of an array.
            result.append(WStr("ArrayClass"));
            return;
        }
        if (CORPROF_E_CLASSID_IS_COMPOSITE == hr)
        {
            // We have a composite class
            result.append(WStr("CompositeClass"));
            return;
        }
        if (CORPROF_E_DATAINCOMPLETE == hr)
        {
            Logger::Warn("Type loading is not yet complete; cannot decode ClassID");
            result.append(WStr("DataIncomplete"));
            return;
        }
        if (FAILED(hr))
        {
            Logger::Debug("GetClassIDInfo failed: ", hr);
            result.append(WStr("Unknown"));
            return;
        }

        ComPtr<IMetaDataImport> pMDImport;
        hr = info10->GetModuleMetaData(modId, (ofRead | ofWrite), IID_IMetaDataImport, reinterpret_cast<IUnknown**>(&pMDImport));
        if (FAILED(hr))
        {
            Logger::Debug("GetModuleMetaData failed: ", hr);
            result.append(WStr("Unknown"));
            return;
        }

        WCHAR wName[max_class_name_len];
        DWORD dwTypeDefFlags = 0;
        hr = pMDImport->GetTypeDefProps(classToken, wName, max_class_name_len, nullptr, &dwTypeDefFlags, nullptr);
        if (FAILED(hr))
        {
            Logger::Debug("GetTypeDefProps failed: ", hr);
            result.append(WStr("Unknown"));
            return;
        }

        result.append(wName);
    }

    void GetFunctionName(FunctionID funcID, const COR_PRF_FRAME_INFO frameInfo, shared::WSTRING& result)
    {
        if (funcID == 0)
        {
            result.append(WStr("Unknown_Native_Function()"));
            return;
        }

        ClassID classId = 0;
        ModuleID moduleId = 0;
        mdToken token = 0;

        // theoretically there is a possibility to use GetFunctionInfo method, but it does not support generic methods
        HRESULT hr = info10->GetFunctionInfo2(funcID, frameInfo, &classId, &moduleId, &token, 0, nullptr, nullptr);
        if (FAILED(hr))
        {
            Logger::Debug("GetFunctionInfo2 failed: ", hr);
            result.append(WStr("Unknown"));
            return;
        }

        ComPtr<IMetaDataImport2> pIMDImport;
        hr = info10->GetModuleMetaData(moduleId, ofRead, IID_IMetaDataImport, reinterpret_cast<IUnknown**>(&pIMDImport));
        if (FAILED(hr))
        {
            Logger::Debug("GetModuleMetaData failed: ", hr);
            result.append(WStr("Unknown"));
            return;
        }

        WCHAR funcName[max_func_name_len];
        funcName[0] = '\0';
        PCCOR_SIGNATURE pSig;
        ULONG cbSig;
        hr = pIMDImport->GetMethodProps(token, nullptr, funcName, max_func_name_len, nullptr, nullptr, &pSig, &cbSig, nullptr, nullptr);
        if (FAILED(hr))
        {
            Logger::Debug("GetMethodProps failed: ", hr);
            result.append(WStr("Unknown"));
            return;
        }

        // If the ClassID returned from GetFunctionInfo is 0, then the function
        // is a shared generic function.
        if (classId != 0)
        {
            LookupClassName(classId, result);
        }
        else
        {
            result.append(WStr("SharedGenericFunction"));
        }

        result.append(WStr("."));

        result.append(funcName);

        // try to list arguments type
        auto function_method_signature = FunctionMethodSignature(pSig, cbSig);
        hr = function_method_signature.TryParse();
        if (FAILED(hr))
        {
            result.append(WStr("(unknown)"));
            Logger::Debug("FunctionMethodSignature parsing failed: ", hr);
        }
        else
        {
            const auto& arguments = function_method_signature.GetMethodArguments();
            result.append(WStr("("));
            for (ULONG i = 0; i < arguments.size(); i++)
            {
                if (i != 0)
                {
                    result.append(WStr(", "));
                }

                result.append(arguments[i].GetTypeTokName(pIMDImport));
            }
            result.append(WStr(")"));
        }
    }

public:
    shared::WSTRING* Lookup(FunctionID fid, COR_PRF_FRAME_INFO frame)
    {
        shared::WSTRING* answer = functionNameCache.get(fid);
        if (answer != nullptr)
        {
            return answer;
        }
        stats.nameCacheMisses++;
        answer = new shared::WSTRING();
        this->GetFunctionName(fid, frame, *answer);
        functionNameCache.put(fid, answer);
        return answer;
    }

    void LookupClassName(ClassID cid, shared::WSTRING& result)
    {
        shared::WSTRING* answer = functionNameCache.get(cid);
        if (answer != nullptr)
        {
            result.append(*answer);
            return;
        }
        answer = new shared::WSTRING();
        this->GetClassName(cid, *answer);
        result.append(*answer);
        functionNameCache.put(cid, answer);
    }
};

HRESULT __stdcall FrameCallback(_In_ FunctionID funcId, _In_ UINT_PTR ip, _In_ COR_PRF_FRAME_INFO frameInfo,
                                _In_ ULONG32 contextSize, _In_ BYTE context[], _In_ void* clientData)
{
    const auto helper = static_cast<SamplingHelper*>(clientData);
    helper->stats.totalFrames++;
    shared::WSTRING* name = helper->Lookup(funcId, frameInfo);
    // This is where line numbers could be calculated
    helper->curWriter->RecordFrame(funcId, *name);
    return S_OK;
}

// Factored out from the loop to a separate function for easier auditing and control of the threadstate lock
void CaptureSamples(ThreadSampler* ts, ICorProfilerInfo10* info10, SamplingHelper& helper)
{
    ICorProfilerThreadEnum* threadEnum = nullptr;
    HRESULT hr = info10->EnumThreads(&threadEnum);
    if (FAILED(hr))
    {
        Logger::Debug("Could not EnumThreads: ", hr);
        return;
    }
    ThreadID threadID;
    ULONG numReturned = 0;

    helper.curWriter->StartBatch();

    while ((hr = threadEnum->Next(1, &threadID, &numReturned)) == S_OK)
    {
        helper.stats.numThreads++;
        ThreadSpanContext spanContext = threadSpanContextMap[threadID];
        auto found = ts->managedTid2state.find(threadID);
        if (found != ts->managedTid2state.end() && found->second != nullptr)
        {
            helper.curWriter->StartSample(threadID, found->second, spanContext);
        }
        else
        {
            auto unknown = ThreadState();
            helper.curWriter->StartSample(threadID, &unknown, spanContext);
        }

        // Don't reuse the hr being used for the thread enum, especially since a failed snapshot isn't fatal
        HRESULT snapshotHr = info10->DoStackSnapshot(threadID, &FrameCallback, COR_PRF_SNAPSHOT_DEFAULT, &helper, nullptr, 0);
        if (FAILED(snapshotHr))
        {
            Logger::Debug("DoStackSnapshot failed: ", snapshotHr);
        }
        helper.curWriter->EndSample();
    }
    helper.curWriter->EndBatch();
}

int GetSamplingPeriod()
{
    const shared::WSTRING val = shared::GetEnvironmentValue(environment::thread_sampling_period);
    if (val.empty())
    {
        return default_sample_period;
    }
    try
    {
#ifdef _WIN32
        const int ival = std::stoi(val);
#else
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        std::string str = convert.to_bytes(val);
        int ival = std::stoi(str);
#endif
        return (int) std::max(minimum_sample_period, ival);
    }
    catch (...)
    {
        return default_sample_period;
    }
}

void PauseClrAndCaptureSamples(ThreadSampler* ts, ICorProfilerInfo10* info10, SamplingHelper & helper)
{
    // These locks are in use by managed threads; Acquire locks before suspending the runtime to prevent deadlock
    std::lock_guard<std::mutex> threadStateGuard(ts->threadStateLock);
    std::lock_guard<std::mutex> spanContextGuard(threadSpanContextLock);

#ifdef _WIN32
    LARGE_INTEGER start, end, elapsed, frequency;
    QueryPerformanceFrequency(&frequency);
    QueryPerformanceCounter(&start);
#else
    timespec start, end, elapsed;
    clock_gettime(CLOCK_MONOTONIC, &start);
#endif

    HRESULT hr = info10->SuspendRuntime();
    if (FAILED(hr))
    {
        Logger::Warn("Could not suspend runtime to sample threads: ", hr);
    }
    else
    {
        try {
            CaptureSamples(ts, info10, helper);
        } catch (const std::exception& e) {
            Logger::Warn("Could not capture thread samples: ", e.what());
        } catch (...) {
            Logger::Warn("Could not capture thread sample for unknown reasons");
        }
    }
    // I don't have any proof but I sure hope that if suspending fails then it's still ok to ask to resume, with no
    // ill effects
    hr = info10->ResumeRuntime();
    if (FAILED(hr))
    {
        Logger::Error("Could not resume runtime? : ", hr);
    }

    int elapsedMicros;
#ifdef _WIN32
    QueryPerformanceCounter(&end);
    elapsed.QuadPart = end.QuadPart - start.QuadPart;
    elapsed.QuadPart *= 1000000;
    elapsed.QuadPart /= frequency.QuadPart;
    elapsedMicros = static_cast<int>(elapsed.QuadPart);
#else
    // FLoating around several places as "microsecond timer on linux c"
    clock_gettime(CLOCK_MONOTONIC, &end);
    if ((end.tv_nsec - start.tv_nsec) < 0) {
        elapsed.tv_sec = end.tv_sec - start.tv_sec - 1;
        elapsed.tv_nsec = 1000000000 + end.tv_nsec - start.tv_nsec;
    } else {
        elapsed.tv_sec = end.tv_sec - start.tv_sec;
        elapsed.tv_nsec = end.tv_nsec - start.tv_nsec;
    }
    elapsedMicros = elapsed.tv_sec * 1000000 + elapsed.tv_nsec/1000;
#endif
    helper.stats.microsSuspended = elapsedMicros;
    helper.curWriter->WriteFinalStats(helper.stats);
    Logger::Debug("Threads sampled in ", elapsedMicros, " micros. threads=", helper.stats.numThreads,
                  " frames=", helper.stats.totalFrames, " misses=", helper.stats.nameCacheMisses);

    helper.PublishBuffer();
}

void SleepMillis(int millis) {
#ifdef _WIN32
    Sleep(millis);
#else
    usleep(millis * 1000); // micros
#endif
}

DWORD WINAPI SamplingThreadMain(_In_ LPVOID param)
{
    const int sleepMillis = GetSamplingPeriod();
    const auto ts = static_cast<ThreadSampler*>(param);
    ICorProfilerInfo10* info10 = ts->info10;
    SamplingHelper helper;
    helper.info10 = info10;

    info10->InitializeCurrentThread();

    while (true)
    {
        SleepMillis(sleepMillis);
        const bool shouldSample = helper.AllocateBuffer();
        if (!shouldSample) {
            Logger::Warn("Skipping a thread sample period, buffers are full. ** THIS WILL RESULT IN LOSS OF PROFILING DATA **");
        } else {
            PauseClrAndCaptureSamples(ts, info10, helper);
        }
    }
}

void ThreadSampler::StartSampling(ICorProfilerInfo10* cor_profiler_info10)
{
    Logger::Info("ThreadSampler::StartSampling");
    profilerInfo = cor_profiler_info10;
    this->info10 = cor_profiler_info10;
#ifdef _WIN32
    CreateThread(nullptr, 0, &SamplingThreadMain, this, 0, nullptr);
#else
    pthread_t thr;
    pthread_create(&thr, NULL, (void *(*)(void *)) &SamplingThreadMain, this);
#endif
}

void ThreadSampler::ThreadCreated(ThreadID threadId)
{
    // So it seems the Thread* items can be/are called out of order.  ThreadCreated doesn't carry any valuable
    // ThreadState information so this is a deliberate nop.  The other methods will fault in ThreadStates
    // as needed.
    // Hopefully the destroyed event is not called out of order with the others... if so, the worst that happens
    // is we get an empty name string and a 0 in the native ID column
}
void ThreadSampler::ThreadDestroyed(ThreadID threadId)
{
    {
        std::lock_guard<std::mutex> guard(threadStateLock);

        const ThreadState* state = managedTid2state[threadId];

        delete state;

        managedTid2state.erase(threadId);
    }
    {
        std::lock_guard<std::mutex> guard(threadSpanContextLock);

        threadSpanContextMap.erase(threadId);
    }
}
void ThreadSampler::ThreadAssignedToOSThread(ThreadID threadId, DWORD osThreadId)
{
    std::lock_guard<std::mutex> guard(threadStateLock);

    ThreadState* state = managedTid2state[threadId];
    if (state == nullptr)
    {
        state = new ThreadState();
        managedTid2state[threadId] = state;
    }
    state->nativeId = osThreadId;
}
void ThreadSampler::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR _name[])
{
    std::lock_guard<std::mutex> guard(threadStateLock);

    ThreadState* state = managedTid2state[threadId];
    if (state == nullptr)
    {
        state = new ThreadState();
        managedTid2state[threadId] = state;
    }
    state->threadName.clear();
    state->threadName.append(_name, cchName);
}

NameCache::NameCache(size_t maximumSize) : maxSize(maximumSize)
{
}

shared::WSTRING* NameCache::get(UINT_PTR key)
{
    const auto found = map.find(key);
    if (found == map.end())
    {
        return nullptr;
    }
    // This voodoo moves the single item in the iterator to the front of the list
    // (as it is now the most-recently-used)
    list.splice(list.begin(), list, found->second);
    return found->second->second;
}

void NameCache::put(UINT_PTR key, shared::WSTRING* val)
{
    const auto pair = std::pair<FunctionID, shared::WSTRING*>(key, val);
    list.push_front(pair);
    map[key] = list.begin();

    if (map.size() > maxSize)
    {
        const auto &lru = list.back();
        delete lru.second; // FIXME consider using WSTRING directly instead of WSTRING*
        map.erase(lru.first);
        list.pop_back();
    }
}

} // namespace trace

extern "C"
{
    EXPORTTHIS int32_t SignalFxReadThreadSamples(int32_t len, unsigned char* buf)
    {
        return ThreadSampling_ConsumeOneThreadSample(len, buf);
    }
    EXPORTTHIS void SignalFxSetNativeContext(uint64_t traceIdHigh, uint64_t traceIdLow, uint64_t spanId,
                                                        int32_t managedThreadId)
    {
        ThreadID threadId;
        const HRESULT hr = profilerInfo->GetCurrentThreadID(&threadId);
        if (FAILED(hr)) {
            trace::Logger::Debug("GetCurrentThreadID failed: ", hr);
            return;
        }

        std::lock_guard<std::mutex> guard(threadSpanContextLock);

        threadSpanContextMap[threadId] = trace::ThreadSpanContext(traceIdHigh, traceIdLow, spanId, managedThreadId);
    }
}
