#pragma once
#include "clr_helpers.h"
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
    EXPORTTHIS void SignalFxSetNativeContext(uint64_t traceIdHigh, uint64_t traceIdLow, uint64_t spanId, int32_t managedThreadId);
}

namespace trace
{
struct SamplingStatistics {
    int microsSuspended;
    int numThreads;
    int totalFrames;
    int nameCacheMisses;
    SamplingStatistics() : microsSuspended(0), numThreads(0), totalFrames(0), nameCacheMisses(0)
    {
    }
    SamplingStatistics(SamplingStatistics const& other) :
        microsSuspended(other.microsSuspended),
        numThreads(other.numThreads),
        totalFrames(other.totalFrames),
        nameCacheMisses(other.nameCacheMisses)
    {
    }
};

class ThreadSpanContext
{
public:
    uint64_t traceIdHigh;
    uint64_t traceIdLow;
    uint64_t spanId;
    int32_t managedThreadId;

    ThreadSpanContext() : traceIdHigh(0), traceIdLow(0), spanId(0), managedThreadId(unknown_managed_thread_id)
    {
    }
    ThreadSpanContext(uint64_t _traceIdHigh, uint64_t _traceIdLow, uint64_t _spanId, int32_t managedThreadId) :
        traceIdHigh(_traceIdHigh), traceIdLow(_traceIdLow), spanId(_spanId), managedThreadId(managedThreadId)
    {
    }
    ThreadSpanContext(ThreadSpanContext const& other) :
        traceIdHigh(other.traceIdHigh), traceIdLow(other.traceIdLow), spanId(other.spanId), managedThreadId(other.managedThreadId)
    {
    }
};

class ThreadState
{
public:
    DWORD nativeId;
    shared::WSTRING threadName;
    ThreadState() : nativeId(0)
    {
    }
    ThreadState(ThreadState const& other) : nativeId(other.nativeId), threadName(other.threadName)
    {
    }
};

class ThreadSampler
{
public:
    void StartSampling(ICorProfilerInfo10* cor_profiler_info10);
    ICorProfilerInfo10* info10;
    static void ThreadCreated(ThreadID threadId);
    void ThreadDestroyed(ThreadID threadId);
    void ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId);
    void ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]);

    std::unordered_map<ThreadID, ThreadState*> managedTid2state;
    std::mutex threadStateLock;
};

class ThreadSamplesBuffer
{
public:
    std::unordered_map<FunctionID, int> codes;
    std::vector<unsigned char>* buffer;

    ThreadSamplesBuffer(std::vector<unsigned char>* buf);
    ~ThreadSamplesBuffer();
    void StartBatch() const;
    void StartSample(ThreadID id, const ThreadState* state, const ThreadSpanContext& spanContext) const;
    void RecordFrame(FunctionID fid, const shared::WSTRING& frame);
    void EndSample() const;
    void EndBatch() const;
    void WriteFinalStats(const SamplingStatistics& stats) const;

private:
    void writeCodedFrameString(FunctionID fid, const shared::WSTRING& str);
    void writeShort(int16_t val) const;
    void writeInt(int32_t val) const;
    void writeString(const shared::WSTRING& str) const;
    void writeByte(unsigned char b) const;
    void writeUInt64(uint64_t val) const;
};

class NameCache
{
public:
    NameCache(size_t maximumSize);
    shared::WSTRING* get(UINT_PTR key);
    void put(UINT_PTR key, shared::WSTRING* val);

private:
    size_t maxSize;
    std::list<std::pair<FunctionID, shared::WSTRING*>> list;
    std::unordered_map<FunctionID, std::list<std::pair<FunctionID, shared::WSTRING*>>::iterator> map;
};



} // namespace trace

bool ThreadSampling_ShouldProduceThreadSample();
void ThreadSampling_RecordProducedThreadSample(std::vector<unsigned char>* buf);
// Can return 0 if none are pending
int32_t ThreadSampling_ConsumeOneThreadSample(int32_t len, unsigned char* buf);
