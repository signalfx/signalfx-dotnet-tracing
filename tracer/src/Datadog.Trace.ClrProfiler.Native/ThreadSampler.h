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

}  // namespace trace