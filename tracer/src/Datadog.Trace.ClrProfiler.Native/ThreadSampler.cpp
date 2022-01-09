#include "ThreadSampler.h"
#include "logger.h"
#include <chrono>
#include <map>

#define MAX_FUNC_NAME_LEN 256
#define MAX_CLASS_NAME_LEN 512
#define MAX_STRING_LENGTH 512

#define MAX_CODES_PER_BUFFER (10 * 1000)

// If you change this, consider ThreadSampler.cs too
#define SAMPLES_BUFFER_MAXIMUM_SIZE (200 * 1024)

#define SAMPLES_BUFFER_DEFAULT_SIZE 20 * 1024

// If you change these, change ThreadSampler.cs too
#define DEFAULT_SAMPLE_PERIOD 1000
#define MINIMUM_SAMPLE_PERIOD 1000

// FIXME make configurable (hidden)?
// These numbers were chosen to keep total overhead under 1 MB of RAM in typical cases (name lengths being the biggest
// variable)
#define MAX_FUNCTION_NAME_CACHE_SIZE 6000
#define MAX_CLASS_NAME_CACHE_SIZE 1000

// If you squint you can make out that the original bones of this came from sample code provided by the dotnet project:
// https://github.com/dotnet/samples/blob/2cf486af936261b04a438ea44779cdc26c613f98/core/profiling/stacksampling/src/sampler.cpp
// That stacksampling project is worth reading for a simpler (though higher overhead) take on thread sampling.

static std::mutex bufferLock = std::mutex();
static std::vector<unsigned char>* bufferA;
static std::vector<unsigned char>* bufferB;

static std::mutex threadSpanContextLock;
static std::unordered_map<ThreadID, trace::ThreadSpanContext> threadSpanContextMap;

static ICorProfilerInfo* profilerInfo; // FIXME should really refactor and have a single static instance of ThreadSampler

// Dirt-simple backpressure system to save overhead if managed code is not reading fast enough
bool ThreadSampling_ShouldProduceThreadSample()
{
    std::lock_guard<std::mutex> guard(bufferLock);
    return bufferA == NULL || bufferB == NULL;
}
void ThreadSampling_RecordProducedThreadSample(std::vector<unsigned char>* buf)
{
    std::lock_guard<std::mutex> guard(bufferLock);
    if (bufferA == NULL) {
        bufferA = buf;
    } else if (bufferB == NULL) {
        bufferB = buf;
    } else {
        delete buf; // needs to be dropped now
    }
}
// Can return 0 if none are pending
int ThreadSampling_ConsumeOneThreadSample(int len, unsigned char* buf)
{
    std::vector<unsigned char>* toUse = NULL;
    {
        std::lock_guard<std::mutex> guard(bufferLock);
        if (bufferA != NULL)
        {
            toUse = bufferA;
            bufferA = NULL;
        }
        else if (bufferB != NULL)
        {
            toUse = bufferB;
            bufferB = NULL;
        }
    }
    if (toUse == NULL)
    {
        return 0;
    }
    int toUseLen = (int) fminl(toUse->size(), len);
    memcpy(buf, toUse->data(), toUseLen);
    delete toUse;
    return toUseLen;
}

namespace trace
{

template <class MetaInterface>
class COMPtrHolder
{
public:
    COMPtrHolder()
    {
        m_ptr = NULL;
    }

    COMPtrHolder(MetaInterface* ptr)
    {
        if (ptr != NULL)
        {
            ptr->AddRef();
        }
        m_ptr = ptr;
    }

    ~COMPtrHolder()
    {
        if (m_ptr != NULL)
        {
            m_ptr->Release();
            m_ptr = NULL;
        }
    }
    MetaInterface* operator->()
    {
        return m_ptr;
    }

    MetaInterface** operator&()
    {
        // _ASSERT(m_ptr == NULL);
        return &m_ptr;
    }

    operator MetaInterface*()
    {
        return m_ptr;
    }

private:
    MetaInterface* m_ptr;
};

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
* (e.g., 0x01 is StartSample, which is followed by an int versionNumber and a long captureStartTimeInMillis)
*
* The bulk of the data is an (unknown length) array of frame strings, which are represented as coded strings in each buffer.
* Each used string is given a code (starting at 1) - using an old old inline trick, codes are introduced by writing the code as a
* negative number followed by the definition of the string (length-prefixed) that maps to that code.  Later uses of the code
* simply use the 2-byte (positive) code, meaning frequently used strings will take only 2 bytes apiece.  0 is reserved for "end of list"
* since the number of frames is not known up-front.
*/

// defined opcodes
#define THREAD_SAMPLES_START_BATCH  0x01
#define THREAD_SAMPLES_START_SAMPLE 0x02
#define THREAD_SAMPLES_END_BATCH    0x06
#define THREAD_SAMPLES_FINAL_STATS  0x07

ThreadSamplesBuffer::ThreadSamplesBuffer(std::vector<unsigned char>* buf) : buffer(buf)
{
}
ThreadSamplesBuffer ::~ThreadSamplesBuffer()
{
    buffer = NULL; // specifically don't delete as ownership/lifecycle is complicated
}

#define CHECK_SAMPLES_BUFFER_LENGTH() {  if (buffer->size() >= SAMPLES_BUFFER_MAXIMUM_SIZE) { return; } }
void ThreadSamplesBuffer::StartBatch()
{
    CHECK_SAMPLES_BUFFER_LENGTH();
    writeByte(THREAD_SAMPLES_START_BATCH);
    writeInt(1); // version number
    std::chrono::milliseconds ms =
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch());
    writeInt64((int64_t) ms.count());
}

void ThreadSamplesBuffer::StartSample(ThreadID id, ThreadState* state, ThreadSpanContext context)
{
    CHECK_SAMPLES_BUFFER_LENGTH();
    writeByte(THREAD_SAMPLES_START_SAMPLE);
    writeInt((int)id); // FIXME not really sure how to map this to anything; needs more research
    writeInt(state->nativeId);
    writeString(state->threadName);
    writeInt64(context.traceIdHigh);
    writeInt64(context.traceIdLow);
    writeInt64(context.spanId);
    // Feature possibilities: (managed/native) thread priority, cpu/wait times, etc.
}
void ThreadSamplesBuffer::RecordFrame(FunctionID fid, WSTRING& frame)
{
    CHECK_SAMPLES_BUFFER_LENGTH();
    writeCodedFrameString(fid, frame);
}
void ThreadSamplesBuffer::EndSample()
{
    CHECK_SAMPLES_BUFFER_LENGTH();
    writeShort(0);
}
void ThreadSamplesBuffer::EndBatch()
{
    CHECK_SAMPLES_BUFFER_LENGTH();
    writeByte(THREAD_SAMPLES_END_BATCH);
}
void ThreadSamplesBuffer::WriteFinalStats(int microsSuspended)
{
    CHECK_SAMPLES_BUFFER_LENGTH();
    writeByte(THREAD_SAMPLES_FINAL_STATS);
    writeInt(microsSuspended);
}

void ThreadSamplesBuffer::writeCodedFrameString(FunctionID fid, WSTRING& str)
{
    auto found = codes.find(fid);
    if (found != codes.end())
    {
        writeShort(found->second);
    }
    else
    {
        int code = (int) codes.size() + 1;
        if (codes.size() + 1 < MAX_CODES_PER_BUFFER)
        {
            codes[fid] = code;
        }
        writeShort(-code); // note negative sign indiciating definition of code
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
void ThreadSamplesBuffer::writeString(WSTRING& str)
{
    // limit strings to a max length overall; this prevents (e.g.) thread names or
    // any other miscellaneous strings that come along from blowing things out
    short usedLen = (short) fminl(str.length(), MAX_STRING_LENGTH);
    writeShort(usedLen);
    // odd bit of casting since we're copying bytes, not wchars
    unsigned char* strBegin = (unsigned char*)(&str.c_str()[0]);
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
    ICorProfilerInfo10* info10 = NULL;
    ThreadSamplesBuffer* curWriter = NULL;
    std::vector<unsigned char>* curBuffer = NULL;
    NameCache functionNameCache;
    NameCache classNameCache;
    SamplingHelper() : functionNameCache(MAX_FUNCTION_NAME_CACHE_SIZE), classNameCache(MAX_CLASS_NAME_CACHE_SIZE)
    {
    }

    bool AllocateBuffer()
    {
        bool should = ThreadSampling_ShouldProduceThreadSample();
        if (!should)
        {
            return should;
        }
        curBuffer = new std::vector<unsigned char>();
        curBuffer->reserve(SAMPLES_BUFFER_DEFAULT_SIZE);
        curWriter = new ThreadSamplesBuffer(curBuffer);
        return should;
    }
    void PublishBuffer()
    {
        ThreadSampling_RecordProducedThreadSample(curBuffer);
        delete curWriter;
        curWriter = NULL;
        curBuffer = NULL;
    }

private:
    void GetClassName(ClassID classId, WSTRING& result)
    {
        ModuleID modId;
        mdTypeDef classToken;
        ClassID parentClassID;
        HRESULT hr = S_OK;

        if (classId == NULL)
        {
            Logger::Debug("NULL classId passed to GetClassName");
            result.append(WStr("Unknown"));
            return;
        }

        hr = info10->GetClassIDInfo2(classId, &modId, &classToken, &parentClassID, 0, NULL, NULL);
        if (CORPROF_E_CLASSID_IS_ARRAY == hr)
        {
            // We have a ClassID of an array.
            result.append(WStr("ArrayClass"));
            return;
        }
        else if (CORPROF_E_CLASSID_IS_COMPOSITE == hr)
        {
            // We have a composite class
            result.append(WStr("CompositeClass"));
            return;
        }
        else if (CORPROF_E_DATAINCOMPLETE == hr)
        {
            // type-loading is not yet complete. Cannot do anything about it.
            result.append(WStr("DataIncomplete"));
            return;
        }
        else if (FAILED(hr))
        {
            Logger::Debug("GetClassIDInfo failed: ", hr);
            result.append(WStr("Unknown"));
            return;
        }

        COMPtrHolder<IMetaDataImport> pMDImport;
        hr = info10->GetModuleMetaData(modId, (ofRead | ofWrite), IID_IMetaDataImport, (IUnknown**) &pMDImport);
        if (FAILED(hr))
        {
            Logger::Debug("GetModuleMetaData failed: ", hr);
            result.append(WStr("Unknown"));
            return;
        }

        WCHAR wName[MAX_CLASS_NAME_LEN];
        DWORD dwTypeDefFlags = 0;
        hr = pMDImport->GetTypeDefProps(classToken, wName, MAX_CLASS_NAME_LEN, NULL, &dwTypeDefFlags, NULL);
        if (FAILED(hr))
        {
            Logger::Debug("GetTypeDefProps failed: ", hr);
            result.append(WStr("Unknown"));
            return;
        }

        result.append(wName);
    }

    void GetFunctionName(FunctionID funcID, const COR_PRF_FRAME_INFO frameInfo, WSTRING& result)
    {
        if (funcID == NULL)
        {
            result.append(WStr("Unknown_Native_Function"));
            return;
        }

        ClassID classId = NULL;
        ModuleID moduleId = NULL;
        mdToken token = NULL;

        HRESULT hr = info10->GetFunctionInfo2(funcID, frameInfo, &classId, &moduleId, &token, 0, NULL, NULL);
        if (FAILED(hr))
        {
            Logger::Debug("GetFunctionInfo2 failed: ", hr);
        }

        COMPtrHolder<IMetaDataImport> pIMDImport;
        hr = info10->GetModuleMetaData(moduleId, ofRead, IID_IMetaDataImport, (IUnknown**) &pIMDImport);
        if (FAILED(hr))
        {
            Logger::Debug("GetModuleMetaData failed: ", hr);
        }

        WCHAR funcName[MAX_FUNC_NAME_LEN];
        funcName[0] = 0;
        hr = pIMDImport->GetMethodProps(token, NULL, funcName, MAX_FUNC_NAME_LEN, 0, 0, NULL, NULL, NULL, NULL);
        if (FAILED(hr))
        {
            Logger::Debug("GetMethodProps failed: ", hr);
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

        result.append(WStr("::"));

        result.append(funcName);

        // FIXME What about method signature to differentiate overloaded methods?
    }

public:
    WSTRING* Lookup(FunctionID fid, COR_PRF_FRAME_INFO frame)
    {
        WSTRING* answer = functionNameCache.get(fid);
        if (answer != NULL)
        {
            return answer;
        }
        answer = new WSTRING();
        this->GetFunctionName(fid, frame, *answer);
        functionNameCache.put(fid, answer);
        return answer;
    }

    void LookupClassName(ClassID cid, WSTRING& result)
    {
        WSTRING* answer = functionNameCache.get(cid);
        if (answer != NULL)
        {
            result.append(*answer);
            return;
        }
        answer = new WSTRING();
        this->GetClassName(cid, *answer);
        result.append(*answer);
        functionNameCache.put(cid, answer);
    }
};

HRESULT __stdcall FrameCallback(_In_ FunctionID funcId, _In_ UINT_PTR ip, _In_ COR_PRF_FRAME_INFO frameInfo,
                                _In_ ULONG32 contextSize, _In_ BYTE context[], _In_ void* clientData)
{
    SamplingHelper* helper = (SamplingHelper*) clientData;
    WSTRING* name = helper->Lookup(funcId, frameInfo);
    // This is where line numbers could be calculated
    helper->curWriter->RecordFrame(funcId, *name);
    return S_OK;
}

// Factored out from the loop to a separate function for easier auditing and control of the threadstate lock
void CaptureSamples(ThreadSampler* ts, ICorProfilerInfo10* info10, SamplingHelper& helper)
{
    ICorProfilerThreadEnum* threadEnum = NULL;
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
        ThreadSpanContext spanContext = threadSpanContextMap[threadID];
        auto found = ts->managedTid2state.find(threadID);
        if (found != ts->managedTid2state.end() && found->second != NULL)
        {
            helper.curWriter->StartSample(threadID, found->second, spanContext);
        }
        else
        {
            auto unknown = ThreadState();
            helper.curWriter->StartSample(threadID, &unknown, spanContext);
        }

        HRESULT localHr = info10->DoStackSnapshot(threadID, &FrameCallback, COR_PRF_SNAPSHOT_DEFAULT, &helper, NULL, 0);
        if (FAILED(hr))
        {
            Logger::Debug("DoStackSnapshot failed: ", hr);
        }
        helper.curWriter->EndSample();
    }
    helper.curWriter->EndBatch();
}

int GetSamplingPeriod()
{
    const WSTRING val = GetEnvironmentValue(environment::thread_sampling_period);
    if (val.empty())
    {
        return DEFAULT_SAMPLE_PERIOD;
    }
    try
    {
#ifdef _WIN32
        int ival = std::stoi(val);
#else
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        std::string str = convert.to_bytes(val);
        int ival = std::stoi(str);
#endif
        return (int) fmaxl(MINIMUM_SAMPLE_PERIOD, ival);
    }
    catch (...)
    {
        return DEFAULT_SAMPLE_PERIOD;
    }
}

void PauseClrAndCaptureSamples(ThreadSampler* ts, ICorProfilerInfo10* info10, SamplingHelper & helper)
{
    std::lock_guard<std::mutex> threadStateGuard(ts->threadStateLock);
    std::lock_guard<std::mutex> spanContextGuard(threadSpanContextLock);

    LARGE_INTEGER start, end, elapsedMicros, frequency;
    QueryPerformanceFrequency(&frequency);
    QueryPerformanceCounter(&start);

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

    QueryPerformanceCounter(&end);
    elapsedMicros.QuadPart = end.QuadPart - start.QuadPart;
    elapsedMicros.QuadPart *= 1000000;
    elapsedMicros.QuadPart /= frequency.QuadPart;
    printf("Resuming runtime after %i micros\n", (int) elapsedMicros.QuadPart);
    helper.curWriter->WriteFinalStats((int) (elapsedMicros.QuadPart));

    helper.PublishBuffer();
}

DWORD WINAPI SamplingThreadMain(_In_ LPVOID param)
{
    int sleepMillis = GetSamplingPeriod();
    ThreadSampler* ts = (ThreadSampler*) param;
    ICorProfilerInfo10* info10 = ts->info10;
    SamplingHelper helper;
    helper.info10 = info10;

    while (1)
    {
        Sleep(sleepMillis);
        bool shouldSample = helper.AllocateBuffer();
        if (!shouldSample) {
            Logger::Warn("Skipping a thread sample period, buffers are full");
            continue;
        } else {
            PauseClrAndCaptureSamples(ts, info10, helper);
        }
    }

    return 0;
}

void ThreadSampler::StartSampling(ICorProfilerInfo10* info10)
{
    Logger::Info("ThreadSampler::StartSampling");
    profilerInfo = info10;
    this->info10 = info10;
    HANDLE bgThread = CreateThread(NULL, 0, &SamplingThreadMain, this, 0, NULL);
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

        ThreadState* state = managedTid2state[threadId];
        if (state != NULL)
        {
            delete state;
        }
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
    if (state == NULL)
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
    if (state == NULL)
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

WSTRING* NameCache::get(UINT_PTR key)
{
    auto found = map.find(key);
    if (found == map.end())
    {
        return NULL;
    }
    // This voodoo moves the single item in the iterator to the front of the list
    // (as it is now the most-recently-used)
    list.splice(list.begin(), list, found->second);
    return found->second->second;
}

void NameCache::put(UINT_PTR key, WSTRING* val)
{
    auto pair = std::pair<FunctionID, WSTRING*>(key, val);
    list.push_front(pair);
    map[key] = list.begin();

    if (map.size() > maxSize)
    {
        auto lru = list.end();
        lru--;
        delete lru->second; // FIXME consider using WSTRING directly instead of WSTRING*
        list.pop_back();
        map.erase(lru->first);
    }
}

} // namespace trace

extern "C"
{
    __declspec(dllexport) int SignalFxReadThreadSamples(int len, unsigned char* buf)
    {
        return ThreadSampling_ConsumeOneThreadSample(len, buf);
    }
    __declspec(dllexport) void SignalFxSetNativeContext(uint64_t traceIdHigh, uint64_t traceIdLow, uint64_t spanId)
    {
        ThreadID threadId;
        HRESULT hr = profilerInfo->GetCurrentThreadID(&threadId);
        if (FAILED(hr)) {
            trace::Logger::Debug("GetCurrentThreadID failed: ", hr);
            return;
        }

        std::lock_guard<std::mutex> guard(threadSpanContextLock);

        threadSpanContextMap[threadId] = trace::ThreadSpanContext(traceIdHigh, traceIdLow, spanId);
    }
}
