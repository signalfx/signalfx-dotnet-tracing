#pragma once
#include "always_on_profiler_clr_helpers.h"
#include <mutex>
#include <cinttypes>
#include <vector>
#include <list>
#include <utility>
#include <unordered_map>

constexpr auto unknown_managed_thread_id = -1;

#ifdef _WIN32
#define EXPORTTHIS __declspec(dllexport)
#else
#define EXPORTTHIS __attribute__((visibility("default")))
#endif


extern "C"
{
    EXPORTTHIS int32_t SignalFxReadThreadSamples(int32_t len, unsigned char* buf);
    EXPORTTHIS int32_t SignalFxReadAllocationSamples(int32_t len, unsigned char* buf);
    // ReSharper disable CppInconsistentNaming
    EXPORTTHIS void SignalFxSetNativeContext(uint64_t traceIdHigh, uint64_t traceIdLow, uint64_t spanId, int32_t managedThreadId);
    // ReSharper restore CppInconsistentNaming
}

namespace always_on_profiler
{
struct SamplingStatistics
{
    int micros_suspended;
    int num_threads;
    int total_frames;
    int name_cache_misses;
    SamplingStatistics() : micros_suspended(0), num_threads(0), total_frames(0), name_cache_misses(0)
    {
    }
    SamplingStatistics(SamplingStatistics const& other) :
        micros_suspended(other.micros_suspended),
        num_threads(other.num_threads),
        total_frames(other.total_frames),
        name_cache_misses(other.name_cache_misses)
    {
    }
};

class thread_span_context
{
public:
    uint64_t trace_id_high_;
    uint64_t trace_id_low_;
    uint64_t span_id_;
    int32_t managed_thread_id_;

    thread_span_context() : trace_id_high_(0), trace_id_low_(0), span_id_(0), managed_thread_id_(unknown_managed_thread_id)
    {
    }
    thread_span_context(uint64_t _traceIdHigh, uint64_t _traceIdLow, uint64_t _spanId, int32_t managedThreadId) :
        trace_id_high_(_traceIdHigh), trace_id_low_(_traceIdLow), span_id_(_spanId), managed_thread_id_(managedThreadId)
    {
    }
    thread_span_context(thread_span_context const& other) :
        trace_id_high_(other.trace_id_high_), trace_id_low_(other.trace_id_low_), span_id_(other.span_id_), managed_thread_id_(other.managed_thread_id_)
    {
    }
};

class ThreadState
{
public:
    DWORD native_id_;
    shared::WSTRING thread_name_;
    ThreadState() : native_id_(0)
    {
    }
    ThreadState(ThreadState const& other) : native_id_(other.native_id_), thread_name_(other.thread_name_)
    {
    }
};

class ThreadSampler
{
public:
    void StartThreadSampling();
    void StartAllocationSampling(ICorProfilerInfo12* info12);
    void AllocationTick(ULONG dataLen, LPCBYTE data);
    ICorProfilerInfo10* info10;
    static void ThreadCreated(ThreadID thread_id);
    void ThreadDestroyed(ThreadID thread_id);
    void ThreadAssignedToOsThread(ThreadID managedThreadId, DWORD os_thread_id);
    void ThreadNameChanged(ThreadID thread_id, ULONG cch_name, WCHAR name[]);

    void SetGlobalInfo10(ICorProfilerInfo10* info10);
    ThreadState* GetCurrentThreadState(ThreadID tid);

    std::unordered_map<ThreadID, ThreadState*> managed_tid_to_state_;
    std::mutex thread_state_lock_;
};

class ThreadSamplesBuffer
{
public:
    std::unordered_map<FunctionID, int> codes_;
    std::vector<unsigned char>* buffer_;

    explicit ThreadSamplesBuffer(std::vector<unsigned char>* buf);
    ~ThreadSamplesBuffer();
    void StartBatch() const;
    void StartSample(ThreadID id, const ThreadState* state, const thread_span_context& span_context) const;
    void RecordFrame(FunctionID fid, const shared::WSTRING& frame);
    void EndSample() const;
    void EndBatch() const;
    void WriteFinalStats(const SamplingStatistics& stats) const;

private:
    void WriteCodedFrameString(FunctionID fid, const shared::WSTRING& str);
    void WriteShort(int16_t val) const;
    void WriteInt(int32_t val) const;
    void WriteString(const shared::WSTRING& str) const;
    void WriteByte(unsigned char b) const;
    void WriteUInt64(uint64_t val) const;
};

struct FunctionIdentifier
{
    mdToken function_token;
    ModuleID module_id;
    bool is_valid;

    bool operator==(const FunctionIdentifier& p) const
    {
        return function_token == p.function_token && module_id == p.module_id && is_valid == p.is_valid;
    }
};
} // namespace always_on_profiler

template <>
struct std::hash<always_on_profiler::FunctionIdentifier>
{
    std::size_t operator()(const always_on_profiler::FunctionIdentifier& k) const noexcept
    {
        using std::hash;
        using std::size_t;
        using std::string;

        const std::size_t h1 = std::hash<mdToken>()(k.function_token);
        const std::size_t h2 = std::hash<ModuleID>()(k.module_id);

        return h1 ^ h2;
    }
};

namespace always_on_profiler
{
template <typename TKey, typename TValue>
class NameCache
{
// ModuleID is volatile but it is unlikely to have exactly same pair of Function Token and ModuleId after changes.
// If fails we should end up we Unknown(unknown) as a result
public:
    explicit NameCache(size_t maximum_size, TValue default_value);
    TValue Get(TKey key);
    void Refresh(TKey key);
    // if max cache size is exceeded it return value which should be disposed
    TValue Put(TKey key, TValue val);
    void Clear();

private:
    TValue default_value_;
    size_t max_size_;
    std::list<std::pair<TKey, TValue>> list_;
    std::unordered_map<TKey, typename std::list<std::pair<TKey, TValue>>::iterator> map_;
};
} // namespace always_on_profiler

void AllocationSamplingAppendToBuffer(int32_t appendLen, unsigned char* appendBuf);

bool ThreadSamplingShouldProduceThreadSample();
void ThreadSamplingRecordProducedThreadSample(std::vector<unsigned char>* buf);
// Can return 0 if none are pending
int32_t ThreadSamplingConsumeOneThreadSample(int32_t len, unsigned char* buf);
