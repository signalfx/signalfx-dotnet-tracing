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

constexpr auto kMaxStringLength = 512UL;

constexpr auto kMaxCodesPerBuffer = 10 * 1000;

// If you change this, consider ThreadSampler.cs too
constexpr auto kSamplesBufferMaximumSize = 200 * 1024;

constexpr auto kSamplesBufferDefaultSize = 20 * 1024;

// If you change these, change ThreadSampler.cs too
constexpr auto kDefaultSamplePeriod = 10000;
constexpr auto kMinimumSamplePeriod = 1000;

// FIXME make configurable (hidden)?
// These numbers were chosen to keep total overhead under 1 MB of RAM in typical cases (name lengths being the biggest
// variable)
constexpr auto kMaxFunctionNameCacheSize = 5000;
constexpr auto kMaxVolatileFunctionNameCacheSize = 2000;


// If you squint you can make out that the original bones of this came from sample code provided by the dotnet project:
// https://github.com/dotnet/samples/blob/2cf486af936261b04a438ea44779cdc26c613f98/core/profiling/stacksampling/src/sampler.cpp
// That stack sampling project is worth reading for a simpler (though higher overhead) take on thread sampling.

static std::mutex buffer_lock = std::mutex();
static std::vector<unsigned char>* buffer_a;
static std::vector<unsigned char>* buffer_b;

static std::mutex thread_span_context_lock;
static std::unordered_map<ThreadID, always_on_profiler::thread_span_context> thread_span_context_map;

static ICorProfilerInfo10* profiler_info; // After feature sets settle down, perhaps this should be refactored and have a single static instance of ThreadSampler

// Dirt-simple back pressure system to save overhead if managed code is not reading fast enough
bool ThreadSamplingShouldProduceThreadSample()
{
    std::lock_guard<std::mutex> guard(buffer_lock);
    return buffer_a == nullptr || buffer_b == nullptr;
}
void ThreadSamplingRecordProducedThreadSample(std::vector<unsigned char>* buf)
{
    std::lock_guard<std::mutex> guard(buffer_lock);
    if (buffer_a == nullptr) {
        buffer_a = buf;
    } else if (buffer_b == nullptr) {
        buffer_b = buf;
    } else {
        trace::Logger::Warn("Unexpected buffer drop in ThreadSampling_RecordProducedThreadSample");
        delete buf; // needs to be dropped now
    }
}
// Can return 0 if none are pending
int32_t ThreadSamplingConsumeOneThreadSample(int32_t len, unsigned char* buf)
{
    if (len <= 0 || buf == nullptr)
    {
        trace::Logger::Warn("Unexpected 0/null buffer to ThreadSampling_ConsumeOneThreadSample");
        return 0;
    }
    std::vector<unsigned char>* to_use = nullptr;
    {
        std::lock_guard<std::mutex> guard(buffer_lock);
        if (buffer_a != nullptr)
        {
            to_use = buffer_a;
            buffer_a = nullptr;
        }
        else if (buffer_b != nullptr)
        {
            to_use = buffer_b;
            buffer_b = nullptr;
        }
    }
    if (to_use == nullptr)
    {
        return 0;
    }
    const size_t to_use_len = static_cast<int>(std::min(to_use->size(), static_cast<size_t>(len)));
    memcpy(buf, to_use->data(), to_use_len);
    delete to_use;
    return static_cast<int32_t>(to_use_len);
}

namespace always_on_profiler
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
constexpr auto kThreadSamplesStartBatch = 0x01;
constexpr auto kThreadSamplesStartSample = 0x02;
constexpr auto kThreadSamplesEndBatch = 0x06;
constexpr auto kThreadSamplesFinalStats = 0x07;

constexpr auto kCurrentThreadSamplesBufferVersion = 1;

always_on_profiler::ThreadSamplesBuffer::ThreadSamplesBuffer(std::vector<unsigned char>* buf) : buffer_(buf)
{
}
ThreadSamplesBuffer ::~ThreadSamplesBuffer()
{
    buffer_ = nullptr; // specifically don't delete as this is done by RecordProduced/ConsumeOneThreadSample
}

#define CHECK_SAMPLES_BUFFER_LENGTH() {  if (buffer_->size() >= kSamplesBufferMaximumSize) { return; } }

void ThreadSamplesBuffer::StartBatch() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kThreadSamplesStartBatch);
    WriteInt(kCurrentThreadSamplesBufferVersion);
    const auto ms =
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch());
    WriteUInt64(ms.count());
}

void ThreadSamplesBuffer::StartSample(ThreadID id, const ThreadState* state, const thread_span_context& span_context) const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kThreadSamplesStartSample);
    WriteInt(span_context.managed_thread_id_);
    WriteInt(static_cast<int32_t>(state->native_id_));
    WriteString(state->thread_name_);
    WriteUInt64(span_context.trace_id_high_);
    WriteUInt64(span_context.trace_id_low_);
    WriteUInt64(span_context.span_id_);
    // Feature possibilities: (managed/native) thread priority, cpu/wait times, etc.
}
void ThreadSamplesBuffer::RecordFrame(FunctionID fid, const shared::WSTRING& frame)
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteCodedFrameString(fid, frame);
}
void ThreadSamplesBuffer::EndSample() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteShort(0);
}
void ThreadSamplesBuffer::EndBatch() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kThreadSamplesEndBatch);
}
void ThreadSamplesBuffer::WriteFinalStats(const SamplingStatistics& stats) const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kThreadSamplesFinalStats);
    WriteInt(stats.micros_suspended);
    WriteInt(stats.num_threads);
    WriteInt(stats.total_frames);
    WriteInt(stats.name_cache_misses);
}

void ThreadSamplesBuffer::WriteCodedFrameString(FunctionID fid, const shared::WSTRING& str)
{
    const auto found = codes_.find(fid);
    if (found != codes_.end())
    {
        WriteShort(static_cast<int16_t>(found->second));
    }
    else
    {
        const int code = static_cast<int>(codes_.size()) + 1;
        if (codes_.size() + 1 < kMaxCodesPerBuffer)
        {
            codes_[fid] = code;
        }
        WriteShort(static_cast<int16_t>(-code)); // note negative sign indicating definition of code
        WriteString(str);
    }
}
void ThreadSamplesBuffer::WriteShort(int16_t val) const
{
    buffer_->push_back(((val >> 8) & 0xFF));
    buffer_->push_back(val & 0xFF);
}
void ThreadSamplesBuffer::WriteInt(int32_t val) const
{
    buffer_->push_back(((val >> 24) & 0xFF));
    buffer_->push_back(((val >> 16) & 0xFF));
    buffer_->push_back(((val >> 8) & 0xFF));
    buffer_->push_back(val & 0xFF);
}
void ThreadSamplesBuffer::WriteString(const shared::WSTRING& str) const
{
    // limit strings to a max length overall; this prevents (e.g.) thread names or
    // any other miscellaneous strings that come along from blowing things out
    const short used_len = static_cast<short>(std::min(str.length(), static_cast<size_t>(kMaxStringLength)));
    WriteShort(used_len);
    // odd bit of casting since we're copying bytes, not wchars
    const auto str_begin = reinterpret_cast<const unsigned char*>(&str.c_str()[0]);
    // possible endian-ness assumption here; unclear how the managed layer would decode on big endian platforms
    buffer_->insert(buffer_->end(), str_begin, str_begin + used_len * 2);
}
void ThreadSamplesBuffer::WriteByte(unsigned char b) const
{
    buffer_->push_back(b);
}
void ThreadSamplesBuffer::WriteUInt64(uint64_t val) const
{
    buffer_->push_back(((val >> 56) & 0xFF));
    buffer_->push_back(((val >> 48) & 0xFF));
    buffer_->push_back(((val >> 40) & 0xFF));
    buffer_->push_back(((val >> 32) & 0xFF));
    buffer_->push_back(((val >> 24) & 0xFF));
    buffer_->push_back(((val >> 16) & 0xFF));
    buffer_->push_back(((val >> 8) & 0xFF));
    buffer_->push_back(val & 0xFF);
}

class SamplingHelper
{
public:
    // These are permanent parts of the helper object
    ICorProfilerInfo10* info10_ = nullptr;
    NameCache<FunctionIdentifier> function_name_cache_;
    NameCache<FunctionID> volatile_function_name_cache_;
    // These cycle every sample and/or are owned externally
    ThreadSamplesBuffer* cur_writer_ = nullptr;
    std::vector<unsigned char>* cur_buffer_ = nullptr;
    SamplingStatistics stats_;

    SamplingHelper() : function_name_cache_(kMaxFunctionNameCacheSize), volatile_function_name_cache_(kMaxVolatileFunctionNameCacheSize)
    {
    }

    bool AllocateBuffer()
    {
        const bool should = ThreadSamplingShouldProduceThreadSample();
        if (!should)
        {
            return should;
        }
        stats_ = SamplingStatistics();
        cur_buffer_ = new std::vector<unsigned char>();
        cur_buffer_->reserve(kSamplesBufferDefaultSize);
        cur_writer_ = new ThreadSamplesBuffer(cur_buffer_);
        return should;
    }
    void PublishBuffer()
    {
        ThreadSamplingRecordProducedThreadSample(cur_buffer_);
        delete cur_writer_;
        cur_writer_ = nullptr;
        cur_buffer_ = nullptr;
        stats_ = SamplingStatistics();
    }

private:
    [[nodiscard]] FunctionIdentifier GetFunctionIdentifier(const FunctionID func_id, const COR_PRF_FRAME_INFO frame_info) const
    {
        if (func_id == 0)
        {
            constexpr auto zero_valid_function_identifier = FunctionIdentifier{0, 0, true};
            return zero_valid_function_identifier;
        }

        ModuleID module_id = 0;
        mdToken function_token = 0;
        // theoretically there is a possibility to use GetFunctionInfo method, but it does not support generic methods
        const HRESULT hr = info10_->GetFunctionInfo2(func_id, frame_info, nullptr, &module_id, &function_token, 0, nullptr, nullptr);
        if (FAILED(hr))
        {
            trace::Logger::Debug("GetFunctionInfo2 failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
            constexpr auto zero_invalid_function_identifier = FunctionIdentifier{0, 0, false};
            return zero_invalid_function_identifier;
        }

        return FunctionIdentifier{function_token, module_id, true};
    }

    void GetFunctionName(FunctionIdentifier function_identifier, shared::WSTRING& result)
    {
        constexpr auto unknown_list_of_arguments = WStr("(unknown)");
        constexpr auto unknown_function_name = WStr("Unknown(unknown)");

        if (!function_identifier.is_valid)
        {
            result.append(unknown_function_name);
            return;
        }

        if (function_identifier.function_token == 0)
        {
            constexpr auto unknown_native_function_name = WStr("Unknown_Native_Function(unknown)");
            result.append(unknown_native_function_name);
            return;
        }

        ComPtr<IMetaDataImport2> metadata_import;
        HRESULT hr = info10_->GetModuleMetaData(function_identifier.module_id, ofRead, IID_IMetaDataImport2,
                                              reinterpret_cast<IUnknown**>(&metadata_import));
        if (FAILED(hr))
        {
            trace::Logger::Debug("GetModuleMetaData failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
            result.append(unknown_function_name);
            return;
        }

        const auto function_info = GetFunctionInfo(metadata_import, function_identifier.function_token);

        if (!function_info.IsValid())
        {
            trace::Logger::Debug("GetFunctionInfo failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
            result.append(unknown_function_name);
            return;
        }

        result.append(function_info.type.name);
        result.append(name_separator);
        result.append(function_info.name);

        
        HCORENUM function_gen_params_enum = nullptr;
        HCORENUM class_gen_params_enum = nullptr;
        mdGenericParam function_generic_params[kGenericParamsMaxLen]{};
        mdGenericParam class_generic_params[kGenericParamsMaxLen]{};
        ULONG function_gen_params_count = 0;
        ULONG class_gen_params_count = 0;

        mdTypeDef class_token = function_info.type.id;

        hr = metadata_import->EnumGenericParams(&class_gen_params_enum, class_token, class_generic_params, kGenericParamsMaxLen,
                                     &class_gen_params_count);
        metadata_import->CloseEnum(class_gen_params_enum);
        if (FAILED(hr))
        {
            trace::Logger::Debug("Class generic parameters enumeration failed. HRESULT=0x", std::setfill('0'), std::setw(8),
                          std::hex, hr);
            result.append(unknown_list_of_arguments);
            return;
        }
        
        hr = metadata_import->EnumGenericParams(&function_gen_params_enum, function_identifier.function_token, function_generic_params,
                                     kGenericParamsMaxLen,
                                      &function_gen_params_count);
        metadata_import->CloseEnum(function_gen_params_enum);
        if (FAILED(hr))
        {
            trace::Logger::Debug("Method generic parameters enumeration failed. HRESULT=0x", std::setfill('0'), std::setw(8),
                          std::hex, hr);
            result.append(unknown_list_of_arguments);
            return;
        }

        if (function_gen_params_count > 0)
        {
            result.append(kGenericParamsOpeningBrace);
            for (ULONG i = 0; i < function_gen_params_count; ++i)
            {
                if (i != 0)
                {
                    result.append(kParamsSeparator);
                }

                WCHAR param_type_name[kParamNameMaxLen]{};
                ULONG pch_name = 0;
                hr = metadata_import->GetGenericParamProps(function_generic_params[i], nullptr, nullptr, nullptr,
                                                           nullptr, param_type_name, kParamNameMaxLen, &pch_name);
                if (FAILED(hr))
                {
                    trace::Logger::Debug("GetGenericParamProps failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
                    result.append(kUnknown);
                }
                else
                {
                    result.append(param_type_name);
                }
            }
            result.append(kGenericParamsClosingBrace);
        }
        
        // try to list arguments type
        FunctionMethodSignature function_method_signature = function_info.method_signature;
        hr = function_method_signature.TryParse();
        if (FAILED(hr))
        {
            result.append(unknown_list_of_arguments);
            trace::Logger::Debug("FunctionMethodSignature parsing failed. HRESULT=0x", std::setfill('0'), std::setw(8),std::hex, hr);
        }
        else
        {
            const auto& arguments = function_method_signature.GetMethodArguments();
            result.append(kFunctionParamsOpeningBrace);
            for (ULONG i = 0; i < arguments.size(); i++)
            {
                if (i != 0)
                {
                    result.append(kParamsSeparator);
                }

                result.append(arguments[i].GetTypeTokName(metadata_import, class_generic_params, function_generic_params));
            }
            result.append(kFunctionParamsClosingBrace);
        }
    }

public:
    shared::WSTRING* Lookup(FunctionID fid, COR_PRF_FRAME_INFO frame)
    {
        // this method is using two layers cache
        // 1st layer depends on FunctionID which is volatile (and valid only within one thread suspension)
        // 2nd layers depends on mdToken for function (which is stable) and ModuleId which could be volatile,
        // but the pair should be stable enough to avoid any overlaps.
        shared::WSTRING* answer = volatile_function_name_cache_.Get(fid);
        if (answer != nullptr)
        {
            return answer;
        }

        const auto function_identifier = this->GetFunctionIdentifier(fid, frame);

        answer = function_name_cache_.Get(function_identifier);
        if (answer != nullptr)
        {
            volatile_function_name_cache_.Put(fid, answer);
            return answer;
        }
        stats_.name_cache_misses++;
        answer = new shared::WSTRING();
        this->GetFunctionName(function_identifier, *answer);
        function_name_cache_.Put(function_identifier, answer);
        volatile_function_name_cache_.Put(fid, answer);
        return answer;
    }
};

HRESULT __stdcall FrameCallback(_In_ FunctionID func_id, _In_ UINT_PTR ip, _In_ COR_PRF_FRAME_INFO frame_info,
                                _In_ ULONG32 context_size, _In_ BYTE context[], _In_ void* client_data)
{
    const auto helper = static_cast<SamplingHelper*>(client_data);
    helper->stats_.total_frames++;
    const shared::WSTRING* name = helper->Lookup(func_id, frame_info);
    // This is where line numbers could be calculated
    helper->cur_writer_->RecordFrame(func_id, *name);
    return S_OK;
}

// Factored out from the loop to a separate function for easier auditing and control of the thread state lock
void CaptureSamples(ThreadSampler* ts, ICorProfilerInfo10* info10, SamplingHelper& helper)
{
    ICorProfilerThreadEnum* thread_enum = nullptr;
    HRESULT hr = info10->EnumThreads(&thread_enum);
    if (FAILED(hr))
    {
        trace::Logger::Debug("Could not EnumThreads. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
        return;
    }
    ThreadID thread_id;
    ULONG num_returned = 0;

    helper.volatile_function_name_cache_.Clear();
    helper.cur_writer_->StartBatch();

    while ((hr = thread_enum->Next(1, &thread_id, &num_returned)) == S_OK)
    {
        helper.stats_.num_threads++;
        thread_span_context spanContext = thread_span_context_map[thread_id];
        auto found = ts->managed_tid_to_state_.find(thread_id);
        if (found != ts->managed_tid_to_state_.end() && found->second != nullptr)
        {
            helper.cur_writer_->StartSample(thread_id, found->second, spanContext);
        }
        else
        {
            auto unknown = ThreadState();
            helper.cur_writer_->StartSample(thread_id, &unknown, spanContext);
        }

        // Don't reuse the hr being used for the thread enum, especially since a failed snapshot isn't fatal
        HRESULT snapshotHr = info10->DoStackSnapshot(thread_id, &FrameCallback, COR_PRF_SNAPSHOT_DEFAULT, &helper, nullptr, 0);
        if (FAILED(snapshotHr))
        {
            trace::Logger::Debug("DoStackSnapshot failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, snapshotHr);
        }
        helper.cur_writer_->EndSample();
    }
    helper.cur_writer_->EndBatch();
}

int GetSamplingPeriod()
{
    const shared::WSTRING val = shared::GetEnvironmentValue(trace::environment::thread_sampling_period);
    if (val.empty())
    {
        return kDefaultSamplePeriod;
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
        return (int) std::max(kMinimumSamplePeriod, parsedValue);
    }
    catch (...)
    {
        return kDefaultSamplePeriod;
    }
}

void PauseClrAndCaptureSamples(ThreadSampler* ts, ICorProfilerInfo10* info10, SamplingHelper & helper)
{
    // These locks are in use by managed threads; Acquire locks before suspending the runtime to prevent deadlock
    std::lock_guard<std::mutex> thread_state_guard(ts->thread_state_lock_);
    std::lock_guard<std::mutex> span_context_guard(thread_span_context_lock);

    const auto start = std::chrono::steady_clock::now();

    HRESULT hr = info10->SuspendRuntime();
    if (FAILED(hr))
    {
        trace::Logger::Warn("Could not suspend runtime to sample threads. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
    }
    else
    {
        try {
            CaptureSamples(ts, info10, helper);
        } catch (const std::exception& e) {
            trace::Logger::Warn("Could not capture thread samples: ", e.what());
        } catch (...) {
            trace::Logger::Warn("Could not capture thread sample for unknown reasons");
        }
    }
    // I don't have any proof but I sure hope that if suspending fails then it's still ok to ask to resume, with no
    // ill effects
    hr = info10->ResumeRuntime();
    if (FAILED(hr))
    {
        trace::Logger::Error("Could not resume runtime? HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
    }

    const auto end = std::chrono::steady_clock::now();
    const auto elapsed_micros = std::chrono::duration_cast<std::chrono::microseconds>(end - start).count();
    helper.stats_.micros_suspended = static_cast<int>(elapsed_micros);
    helper.cur_writer_->WriteFinalStats(helper.stats_);
    trace::Logger::Debug("Threads sampled in ", elapsed_micros, " micros. threads=", helper.stats_.num_threads,
                  " frames=", helper.stats_.total_frames, " misses=", helper.stats_.name_cache_misses);

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
    const int sleep_millis = GetSamplingPeriod();
    const auto ts = static_cast<ThreadSampler*>(param);
    ICorProfilerInfo10* info10 = ts->info10;
    SamplingHelper helper;
    helper.info10_ = info10;

    info10->InitializeCurrentThread();

    while (true)
    {
        SleepMillis(sleep_millis);
        const bool shouldSample = helper.AllocateBuffer();
        if (!shouldSample) {
            trace::Logger::Warn("Skipping a thread sample period, buffers are full. ** THIS WILL RESULT IN LOSS OF PROFILING DATA **");
        } else {
            PauseClrAndCaptureSamples(ts, info10, helper);
        }
    }
}

void ThreadSampler::StartSampling(ICorProfilerInfo10* cor_profiler_info10)
{
    trace::Logger::Info("ThreadSampler::StartSampling");
    profiler_info = cor_profiler_info10;
    this->info10 = cor_profiler_info10;
#ifdef _WIN32
    CreateThread(nullptr, 0, &SamplingThreadMain, this, 0, nullptr);
#else
    pthread_t thr;
    pthread_create(&thr, NULL, (void *(*)(void *)) &SamplingThreadMain, this);
#endif
}

void ThreadSampler::ThreadCreated(ThreadID thread_id)
{
    // So it seems the Thread* items can be/are called out of order.  ThreadCreated doesn't carry any valuable
    // ThreadState information so this is a deliberate nop.  The other methods will fault in ThreadStates
    // as needed.
    // Hopefully the destroyed event is not called out of order with the others... if so, the worst that happens
    // is we get an empty name string and a 0 in the native ID column
}
void ThreadSampler::ThreadDestroyed(ThreadID thread_id)
{
    {
        std::lock_guard<std::mutex> guard(thread_state_lock_);

        const ThreadState* state = managed_tid_to_state_[thread_id];

        delete state;

        managed_tid_to_state_.erase(thread_id);
    }
    {
        std::lock_guard<std::mutex> guard(thread_span_context_lock);

        thread_span_context_map.erase(thread_id);
    }
}
void ThreadSampler::ThreadAssignedToOsThread(ThreadID thread_id, DWORD os_thread_id)
{
    std::lock_guard<std::mutex> guard(thread_state_lock_);

    ThreadState* state = managed_tid_to_state_[thread_id];
    if (state == nullptr)
    {
        state = new ThreadState();
        managed_tid_to_state_[thread_id] = state;
    }
    state->native_id_ = os_thread_id;
}
void ThreadSampler::ThreadNameChanged(ThreadID thread_id, ULONG cch_name, WCHAR name[])
{
    std::lock_guard<std::mutex> guard(thread_state_lock_);

    ThreadState* state = managed_tid_to_state_[thread_id];
    if (state == nullptr)
    {
        state = new ThreadState();
        managed_tid_to_state_[thread_id] = state;
    }
    state->thread_name_.clear();
    state->thread_name_.append(name, cch_name);
}

template <typename TFunctionIdentifier>
NameCache<TFunctionIdentifier>::NameCache(const size_t maximum_size) : max_size_(maximum_size)
{
}

template <typename TFunctionIdentifier>
shared::WSTRING* NameCache<TFunctionIdentifier>::Get(TFunctionIdentifier key)
{
    const auto found = map_.find(key);
    if (found == map_.end())
    {
        return nullptr;
    }
    // This voodoo moves the single item in the iterator to the front of the list
    // (as it is now the most-recently-used)
    list_.splice(list_.begin(), list_, found->second);
    return found->second->second;
}

template <typename TFunctionIdentifier>
void NameCache<TFunctionIdentifier>::Put(TFunctionIdentifier key, shared::WSTRING* val)
{
    const auto pair = std::pair(key, val);
    list_.push_front(pair);
    map_[key] = list_.begin();

    if (map_.size() > max_size_)
    {
        const auto &lru = list_.back();
        delete lru.second; // FIXME consider using WSTRING directly instead of WSTRING*
        map_.erase(lru.first);
        list_.pop_back();
    }
}

template <typename TFunctionIdentifier>
void NameCache<TFunctionIdentifier>::Clear()
{
    map_.clear();
    list_.clear();
}

} // namespace always_on_profiler

extern "C"
{
    EXPORTTHIS int32_t SignalFxReadThreadSamples(int32_t len, unsigned char* buf)
    {
        return ThreadSamplingConsumeOneThreadSample(len, buf);
    }
    EXPORTTHIS void SignalFxSetNativeContext(uint64_t traceIdHigh, uint64_t traceIdLow, uint64_t spanId,
                                             int32_t managedThreadId)
    {
        ThreadID threadId;
        const HRESULT hr = profiler_info->GetCurrentThreadID(&threadId);
        if (FAILED(hr)) {
            trace::Logger::Debug("GetCurrentThreadID failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
            return;
        }

        std::lock_guard<std::mutex> guard(thread_span_context_lock);

        thread_span_context_map[threadId] = always_on_profiler::thread_span_context(traceIdHigh, traceIdLow, spanId, managedThreadId);
    }
}
