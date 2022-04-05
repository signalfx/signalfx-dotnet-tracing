// We want to use std::min, not the windows.h macro
#define NOMINMAX
#include "always_on_profiler.h"
#include "logger.h"
#include <chrono>
#include <map>
#include <algorithm>
#ifndef _WIN32
  #include <pthread.h>
  #include <codecvt>
#endif

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
constexpr auto max_function_name_cache_size = 7000;

constexpr auto param_name_max_len = 260;
constexpr auto generic_params_max_len = 20;
constexpr auto unknown = WStr("Unknown");
constexpr auto params_separator = WStr(", ");
constexpr auto generic_params_opening_brace = WStr("[");
constexpr auto generic_params_closing_brace = WStr("]");
constexpr auto function_params_opening_brace = WStr("(");
constexpr auto function_params_closing_brace = WStr(")");

// If you squint you can make out that the original bones of this came from sample code provided by the dotnet project:
// https://github.com/dotnet/samples/blob/2cf486af936261b04a438ea44779cdc26c613f98/core/profiling/stacksampling/src/sampler.cpp
// That stack sampling project is worth reading for a simpler (though higher overhead) take on thread sampling.

static std::mutex bufferLock = std::mutex();
static std::vector<unsigned char>* bufferA;
static std::vector<unsigned char>* bufferB;

static std::mutex threadSpanContextLock;
static std::unordered_map<ThreadID, trace::ThreadSpanContext> threadSpanContextMap;

static ICorProfilerInfo* profilerInfo; // After feature sets settle down, perhaps this should be refactored and have a single static instance of ThreadSampler

// Dirt-simple back pressure system to save overhead if managed code is not reading fast enough
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

void ThreadSamplesBuffer::StartBatch() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeByte(thread_samples_start_batch);
    writeInt(current_thread_samples_buffer_version);
    const auto ms =
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch());
    writeUInt64(ms.count());
}

void ThreadSamplesBuffer::StartSample(ThreadID id, const ThreadState* state, const ThreadSpanContext& spanContext) const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeByte(thread_samples_start_sample);
    writeInt(spanContext.managedThreadId);
    writeInt(static_cast<int32_t>(state->nativeId));
    writeString(state->threadName);
    writeUInt64(spanContext.traceIdHigh);
    writeUInt64(spanContext.traceIdLow);
    writeUInt64(spanContext.spanId);
    // Feature possibilities: (managed/native) thread priority, cpu/wait times, etc.
}
void ThreadSamplesBuffer::RecordFrame(FunctionID fid, const shared::WSTRING& frame)
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeCodedFrameString(fid, frame);
}
void ThreadSamplesBuffer::EndSample() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeShort(0);
}
void ThreadSamplesBuffer::EndBatch() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeByte(thread_samples_end_batch);
}
void ThreadSamplesBuffer::WriteFinalStats(const SamplingStatistics& stats) const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    writeByte(thread_samples_final_stats);
    writeInt(stats.microsSuspended);
    writeInt(stats.numThreads);
    writeInt(stats.totalFrames);
    writeInt(stats.nameCacheMisses);
}

void ThreadSamplesBuffer::writeCodedFrameString(FunctionID fid, const shared::WSTRING& str)
{
    const auto found = codes.find(fid);
    if (found != codes.end())
    {
        writeShort(static_cast<int16_t>(found->second));
    }
    else
    {
        const int code = static_cast<int>(codes.size()) + 1;
        if (codes.size() + 1 < max_codes_per_buffer)
        {
            codes[fid] = code;
        }
        writeShort(static_cast<int16_t>(-code)); // note negative sign indicating definition of code
        writeString(str);
    }
}
void ThreadSamplesBuffer::writeShort(int16_t val) const
{
    buffer->push_back(((val >> 8) & 0xFF));
    buffer->push_back(val & 0xFF);
}
void ThreadSamplesBuffer::writeInt(int32_t val) const
{
    buffer->push_back(((val >> 24) & 0xFF));
    buffer->push_back(((val >> 16) & 0xFF));
    buffer->push_back(((val >> 8) & 0xFF));
    buffer->push_back(val & 0xFF);
}
void ThreadSamplesBuffer::writeString(const shared::WSTRING& str) const
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
void ThreadSamplesBuffer::writeByte(unsigned char b) const
{
    buffer->push_back(b);
}
void ThreadSamplesBuffer::writeUInt64(uint64_t val) const
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
    // These cycle every sample and/or are owned externally
    ThreadSamplesBuffer* curWriter = nullptr;
    std::vector<unsigned char>* curBuffer = nullptr;
    SamplingStatistics stats;

    SamplingHelper() : functionNameCache(max_function_name_cache_size)
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
    void GetFunctionName(FunctionID funcID, const COR_PRF_FRAME_INFO frameInfo, shared::WSTRING& result)
    {
        constexpr auto unknown_list_of_arguments = WStr("(unknown)");
        constexpr auto unknown_function_name = WStr("Unknown(unknown)");
        constexpr auto name_separator = WStr(".");

        if (funcID == 0)
        {
            constexpr auto unknown_native_function_name = WStr("Unknown_Native_Function(unknown)");
            result.append(unknown_native_function_name);
            return;
        }

        ModuleID moduleId = 0;
        mdToken functionToken = 0;

        // theoretically there is a possibility to use GetFunctionInfo method, but it does not support generic methods
        HRESULT hr = info10->GetFunctionInfo2(funcID, frameInfo, nullptr, &moduleId, &functionToken, 0, nullptr, nullptr);
        if (FAILED(hr))
        {
            Logger::Debug("GetFunctionInfo2 failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
            result.append(unknown_function_name);
            return;
        }

        ComPtr<IMetaDataImport2> pIMDImport;
        hr = info10->GetModuleMetaData(moduleId, ofRead, IID_IMetaDataImport2, reinterpret_cast<IUnknown**>(&pIMDImport));
        if (FAILED(hr))
        {
            Logger::Debug("GetModuleMetaData failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
            result.append(unknown_function_name);
            return;
        }

        const auto function_info = GetFunctionInfo(pIMDImport, functionToken);

        if (!function_info.IsValid())
        {
            Logger::Debug("GetFunctionInfo failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
            result.append(unknown_function_name);
            return;
        }

        if (function_info.type.parent_type != nullptr)
        {
            // parent class is available only for internal classes.
            // See the class in the test application:  My.Custom.Test.Namespace.ClassA.InternalClassB.DoubleInternalClassB.TripleInternalClassB
            std::shared_ptr<TypeInfo> parent_type = function_info.type.parent_type;
            shared::WSTRING prefix = parent_type->name;
            while (parent_type->parent_type != nullptr)
            {
                // TODO splunk: address warning
                prefix = parent_type->parent_type->name + name_separator + prefix;
                parent_type = parent_type->parent_type;
            }

            result.append(prefix);
            result.append(name_separator);
        }

        result.append(function_info.type.name);
        result.append(name_separator);
        result.append(function_info.name);

        
        HCORENUM functionGenParamsIter = nullptr;
        HCORENUM classGenParamsIter = nullptr;
        mdGenericParam functionGenericParams[generic_params_max_len]{};
        mdGenericParam classGenericParams[generic_params_max_len]{};
        ULONG functionGenParamsCount = 0;
        ULONG classGenParamsCount = 0;

        mdTypeDef classToken = function_info.type.id;

        hr = pIMDImport->EnumGenericParams(&classGenParamsIter, classToken, classGenericParams, generic_params_max_len,
                                     &classGenParamsCount);
        pIMDImport->CloseEnum(classGenParamsIter);
        if (FAILED(hr))
        {
            Logger::Debug("Class generic parameters enumeration failed. HRESULT=0x", std::setfill('0'), std::setw(8),
                          std::hex, hr);
            result.append(unknown_list_of_arguments);
            return;
        }
        
        hr = pIMDImport->EnumGenericParams(&functionGenParamsIter, functionToken, functionGenericParams,
                                     generic_params_max_len,
                                      &functionGenParamsCount);
        pIMDImport->CloseEnum(functionGenParamsIter);
        if (FAILED(hr))
        {
            Logger::Debug("Method generic parameters enumeration failed. HRESULT=0x", std::setfill('0'), std::setw(8),
                          std::hex, hr);
            result.append(unknown_list_of_arguments);
            return;
        }

        if (functionGenParamsCount > 0)
        {
            result.append(generic_params_opening_brace);
            for (ULONG i = 0; i < functionGenParamsCount; ++i)
            {
                if (i != 0)
                {
                    result.append(params_separator);
                }

                WCHAR param_type_name[param_name_max_len]{};
                ULONG pch_name = 0;
                const auto hr =
                    pIMDImport->GetGenericParamProps(functionGenericParams[i], nullptr, nullptr, nullptr, nullptr,
                                                     param_type_name, param_name_max_len, &pch_name);
                if (FAILED(hr))
                {
                    Logger::Debug("GetGenericParamProps failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
                    result.append(unknown);
                }
                else
                {
                    result.append(param_type_name);
                }
            }
            result.append(generic_params_closing_brace);
        }
        
        // try to list arguments type
        FunctionMethodSignature functionMethodSignature = function_info.method_signature;
        hr = functionMethodSignature.TryParse();
        if (FAILED(hr))
        {
            result.append(unknown_list_of_arguments);
            Logger::Debug("FunctionMethodSignature parsing failed. HRESULT=0x", std::setfill('0'), std::setw(8),std::hex, hr);
        }
        else
        {
            const auto& arguments = functionMethodSignature.GetMethodArguments();
            result.append(function_params_opening_brace);
            for (ULONG i = 0; i < arguments.size(); i++)
            {
                if (i != 0)
                {
                    result.append(params_separator);
                }

                auto& currentArg = arguments[i];
                PCCOR_SIGNATURE pbCur = &currentArg.pbBase[currentArg.offset];
                result.append(GetSigTypeTokName(pbCur, pIMDImport, classGenericParams, functionGenericParams));
            }
            result.append(function_params_closing_brace);
        }
    }

    shared::WSTRING ExtractParameterName(PCCOR_SIGNATURE& pbCur, const ComPtr<IMetaDataImport2>& pImport,
                                         const mdGenericParam* genericParameters) const
    {
        pbCur++;
        ULONG num = 0;
        pbCur += CorSigUncompressData(pbCur, &num);
        if (num >= generic_params_max_len)
        {
            return unknown;
        }
        WCHAR param_type_name[param_name_max_len]{};
        ULONG pch_name = 0;
        const auto hr = pImport->GetGenericParamProps(genericParameters[num], nullptr, nullptr, nullptr, nullptr,
                                                      param_type_name, param_name_max_len, &pch_name);
        if (FAILED(hr))
        {
            Logger::Debug("GetGenericParamProps failed. HRESULT=0x", std::setfill('0'), std::setw(8),
                          std::hex, hr);
            return unknown;
        }
        return param_type_name;
    }

    shared::WSTRING GetSigTypeTokName(PCCOR_SIGNATURE& pbCur, const ComPtr<IMetaDataImport2>& pImport,
                              mdGenericParam classParams[], mdGenericParam methodParams[])
    {
        shared::WSTRING tokenName = shared::EmptyWStr;
        bool ref_flag = false;
        if (*pbCur == ELEMENT_TYPE_BYREF)
        {
            pbCur++;
            ref_flag = true;
        }

        switch (*pbCur)
        {
            case ELEMENT_TYPE_BOOLEAN:
                tokenName = SystemBoolean;
                pbCur++;
                break;
            case ELEMENT_TYPE_CHAR:
                tokenName = SystemChar;
                pbCur++;
                break;
            case ELEMENT_TYPE_I1:
                tokenName = SystemSByte;
                pbCur++;
                break;
            case ELEMENT_TYPE_U1:
                tokenName = SystemByte;
                pbCur++;
                break;
            case ELEMENT_TYPE_U2:
                tokenName = SystemUInt16;
                pbCur++;
                break;
            case ELEMENT_TYPE_I2:
                tokenName = SystemInt16;
                pbCur++;
                break;
            case ELEMENT_TYPE_I4:
                tokenName = SystemInt32;
                pbCur++;
                break;
            case ELEMENT_TYPE_U4:
                tokenName = SystemUInt32;
                pbCur++;
                break;
            case ELEMENT_TYPE_I8:
                tokenName = SystemInt64;
                pbCur++;
                break;
            case ELEMENT_TYPE_U8:
                tokenName = SystemUInt64;
                pbCur++;
                break;
            case ELEMENT_TYPE_R4:
                tokenName = SystemSingle;
                pbCur++;
                break;
            case ELEMENT_TYPE_R8:
                tokenName = SystemDouble;
                pbCur++;
                break;
            case ELEMENT_TYPE_I:
                tokenName = SystemIntPtr;
                pbCur++;
                break;
            case ELEMENT_TYPE_U:
                tokenName = SystemUIntPtr;
                pbCur++;
                break;
            case ELEMENT_TYPE_STRING:
                tokenName = SystemString;
                pbCur++;
                break;
            case ELEMENT_TYPE_OBJECT:
                tokenName = SystemObject;
                pbCur++;
                break;
            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_VALUETYPE:
            {
                pbCur++;
                mdToken token;
                pbCur += CorSigUncompressToken(pbCur, &token);
                tokenName = GetTypeInfo(pImport, token).name;
                break;
            }
            case ELEMENT_TYPE_SZARRAY:
            {
                pbCur++;
                tokenName = GetSigTypeTokName(pbCur, pImport, classParams, methodParams) + WStr("[]");
                break;
            }
            case ELEMENT_TYPE_GENERICINST:
            {
                pbCur++;
                tokenName = GetSigTypeTokName(pbCur, pImport, classParams, methodParams);
                tokenName += generic_params_opening_brace;
                ULONG num = 0;
                pbCur += CorSigUncompressData(pbCur, &num);
                for (ULONG i = 0; i < num; i++)
                {
                    tokenName += GetSigTypeTokName(pbCur, pImport, classParams, methodParams);
                    if (i != num - 1)
                    {
                        tokenName += params_separator;
                    }
                }
                tokenName += generic_params_closing_brace;
                break;
            }
            case ELEMENT_TYPE_MVAR:
            {
                tokenName += ExtractParameterName(pbCur, pImport, methodParams);
                break;
            }
            case ELEMENT_TYPE_VAR:
            {
                tokenName += ExtractParameterName(pbCur, pImport, classParams);
                break;
            }
            default:
                break;
        }

        if (ref_flag)
        {
            tokenName += WStr("&");
        }
        return tokenName;
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
};

HRESULT __stdcall FrameCallback(_In_ FunctionID funcId, _In_ UINT_PTR ip, _In_ COR_PRF_FRAME_INFO frameInfo,
                                _In_ ULONG32 contextSize, _In_ BYTE context[], _In_ void* clientData)
{
    const auto helper = static_cast<SamplingHelper*>(clientData);
    helper->stats.totalFrames++;
    const shared::WSTRING* name = helper->Lookup(funcId, frameInfo);
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
        Logger::Debug("Could not EnumThreads. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
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
            Logger::Debug("DoStackSnapshot failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, snapshotHr);
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
        const int parsedValue = std::stoi(val);
#else
        std::wstring_convert<std::codecvt_utf8_utf16<char16_t>, char16_t> convert;
        std::string str = convert.to_bytes(val);
        int parsedValue = std::stoi(str);
#endif
        return (int) std::max(minimum_sample_period, parsedValue);
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

    auto start = std::chrono::steady_clock::now();

    HRESULT hr = info10->SuspendRuntime();
    if (FAILED(hr))
    {
        Logger::Warn("Could not suspend runtime to sample threads. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
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
        Logger::Error("Could not resume runtime? HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
    }

    auto end = std::chrono::steady_clock::now(); 
    auto elapsedMicros = std::chrono::duration_cast<std::chrono::microseconds>(end - start).count();
    helper.stats.microsSuspended = static_cast<int>(elapsedMicros);
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
            trace::Logger::Debug("GetCurrentThreadID failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
            return;
        }

        std::lock_guard<std::mutex> guard(threadSpanContextLock);

        threadSpanContextMap[threadId] = trace::ThreadSpanContext(traceIdHigh, traceIdLow, spanId, managedThreadId);
    }
}
