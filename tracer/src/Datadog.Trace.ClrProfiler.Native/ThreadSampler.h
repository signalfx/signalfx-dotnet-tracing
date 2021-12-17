#pragma once
#include "clr_helpers.h"
#include <mutex>
#include <cinttypes>
#include <vector>
#include <list>
#include <utility>
#include <unordered_map>

extern "C"
{
    __declspec(dllexport) int SignalFx_read_thread_samples(int len, unsigned char* buf);
    __declspec(dllexport) void SignalFx_set_native_context(uint64_t traceIdHigh, uint64_t traceIdLow, uint64_t spanId);
}

namespace trace
{
class ThreadSpanContext
{
public:
    uint64_t traceIdHigh;
    uint64_t traceIdLow;
    uint64_t spanId;

    ThreadSpanContext() : traceIdHigh(0), traceIdLow(0), spanId(0)
    {
    }
    ThreadSpanContext(uint64_t _traceIdHigh, uint64_t _traceIdLow, uint64_t _spanId) :
        traceIdHigh(_traceIdHigh), traceIdLow(_traceIdLow), spanId(_spanId)
    {
    }
    ThreadSpanContext(ThreadSpanContext const& other) :
        traceIdHigh(other.traceIdHigh), traceIdLow(other.traceIdLow), spanId(other.spanId)
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
    void StartSampling(ICorProfilerInfo3* info3);
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
    void StartSample(ThreadID id, ThreadState* state, ThreadSpanContext spanContext);
    void RecordFrame(FunctionID fid, WSTRING& frame);
    void EndSample();
    void EndBatch();
    void WriteFinalStats(int microsSuspended);

private:
    void writeCodedFrameString(FunctionID fid, WSTRING& str);
    void writeShort(int16_t val);
    void writeInt(int32_t val);
    void writeString(WSTRING& str);
    void writeByte(unsigned char b);
    void writeInt64(int64_t val);
};

class NameCache
{
public:
    NameCache(int maximumSize);
    WSTRING* get(UINT_PTR key);
    void put(UINT_PTR key, WSTRING* val);

private:
    int maxSize;
    std::list<std::pair<FunctionID, WSTRING*>> list;
    std::unordered_map<FunctionID, std::list<std::pair<FunctionID, WSTRING*>>::iterator> map;
};



} // namespace trace

bool ThreadSampling_ShouldProduceThreadSample();
void ThreadSampling_RecordProducedThreadSample(std::vector<unsigned char>* buf);
// Can return 0 if none are pending
int ThreadSampling_ConsumeOneThreadSample(int len, unsigned char* buf);