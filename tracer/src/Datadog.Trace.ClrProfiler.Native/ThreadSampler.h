#pragma once
#include "clr_helpers.h"
#include <mutex>

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


       // FIXME map of threadId->threadstate and locking thereof (writes from profiler callbacks, reads from SamplingThreadMain
       std::unordered_map<ThreadID, ThreadState*> managedTid2state;
       std::mutex threadStateLock;

    };

}  // namespace trace