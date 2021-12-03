#pragma once
#include "clr_helpers.h"
#include <mutex>

extern "C"
{
    __declspec(dllexport) int signalfx_read_thread_samples(int len, unsigned char* buf);
}

namespace trace {
    class ThreadState {
 public:
      DWORD nativeId;
        WSTRING threadName;
      ThreadState() : nativeId(0), threadName() {}
        ThreadState(ThreadState const& other)
            : nativeId(other.nativeId), threadName(other.threadName) {}
    };

    class ThreadSampler {
     public:
      void StartSampling(ICorProfilerInfo3* info3);
      ICorProfilerInfo10* info10;
      void ThreadCreated(ThreadID threadId);
      void ThreadDestroyed(ThreadID threadId);
      void ThreadAssignedToOSThread(
          ThreadID managedThreadId, DWORD osThreadId);
      void ThreadNameChanged(ThreadID threadId,
                                                  ULONG cchName,
                                                  WCHAR name[]);


       std::unordered_map<ThreadID, ThreadState*> managedTid2state;
       std::mutex threadStateLock;

    };


    class ThreadSamplesBuffer
    {
    public:
        unsigned char* buffer;
        unsigned int pos; // FIXME would prefer a buffer class of some type here
        std::unordered_map<FunctionID, int> codes;

        ThreadSamplesBuffer(unsigned char* buf);
        ~ThreadSamplesBuffer();
        void StartBatch();
        void StartSample(ThreadID id, ThreadState* state);
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

  }  // namespace trace