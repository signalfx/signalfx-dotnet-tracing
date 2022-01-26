#pragma once
#include "clr_helpers.h"
#include <mutex>
#include <cinttypes>
#include <vector>
#include <list>
#include <utility>
#include <unordered_map>

#define UNKNOWN_MANAGED_THREADID -1

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

    ThreadSpanContext() : traceIdHigh(0), traceIdLow(0), spanId(0), managedThreadId(UNKNOWN_MANAGED_THREADID)
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
    WSTRING threadName;
    ThreadState() : nativeId(0), threadName()
    {
    }
    ThreadState(ThreadState const& other) : nativeId(other.nativeId), threadName(other.threadName)
    {
    }
};

class ThreadSampler
{
public:
    void StartSampling(ICorProfilerInfo10* info10);
    ICorProfilerInfo10* info10;
    void ThreadCreated(ThreadID threadId);
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
    void StartBatch();
    void StartSample(ThreadID id, ThreadState* state, const ThreadSpanContext& spanContext);
    void RecordFrame(FunctionID fid, WSTRING& frame);
    void EndSample();
    void EndBatch();
    void WriteFinalStats(const SamplingStatistics& stats);

private:
    void writeCodedFrameString(FunctionID fid, WSTRING& str);
    void writeShort(int16_t val);
    void writeInt(int32_t val);
    void writeString(const WSTRING& str);
    void writeByte(unsigned char b);
    void writeInt64(int64_t val);
};

class NameCache
{
public:
    NameCache(size_t maximumSize);
    WSTRING* get(UINT_PTR key);
    void put(UINT_PTR key, WSTRING* val);

private:
    size_t maxSize;
    std::list<std::pair<FunctionID, WSTRING*>> list;
    std::unordered_map<FunctionID, std::list<std::pair<FunctionID, WSTRING*>>::iterator> map;
};



} // namespace trace

bool ThreadSampling_ShouldProduceThreadSample();
void ThreadSampling_RecordProducedThreadSample(std::vector<unsigned char>* buf);
// Can return 0 if none are pending
int32_t ThreadSampling_ConsumeOneThreadSample(int32_t len, unsigned char* buf);
