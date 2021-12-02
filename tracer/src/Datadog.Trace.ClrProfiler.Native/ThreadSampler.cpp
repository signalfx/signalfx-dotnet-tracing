#include "ThreadSampler.h"
#include <cinttypes>
#include <map>
#include "logger.h"

#define MAX_FUNC_NAME_LEN 256
#define MAX_CLASS_NAME_LEN 512
#define MAX_STRING_LENGTH 512

#define MAX_CODES_PER_BUFFER (10 * 1000)

// If you change this, consider ThreadSampler.cs too
#define SAMPLES_BUFFER_SIZE (100 * 1024)

// If you change these, change ThreadSampler.cs too
#define DEFAULT_SAMPLE_PERIOD 1000
#define MINIMUM_SAMPLE_PERIOD 1000


// If you squint you can make out that the original bones of this came from sample code provided by the dotnet project:
// https://github.com/dotnet/samples/blob/2cf486af936261b04a438ea44779cdc26c613f98/core/profiling/stacksampling/src/sampler.cpp
// That stacksampling project is worth reading for a simpler (though higher overhead) take on thread sampling.

static std::mutex bufferLock = std::mutex();
static unsigned char* bufferA; // FIXME would like to use std::array, etc. if we can avoid extra copying
static int bufferALen;
static unsigned char* bufferB;
static int bufferBLen;
// Dirt-simple backpressure system to save overhead if managed code is not reading fast enough
bool ShouldProduceThreadSample()
{
    std::lock_guard<std::mutex> guard(bufferLock);
    return bufferA == NULL || bufferB == NULL;
}
void RecordProducedThreadSample(int len, unsigned char* buf)
{
    std::lock_guard<std::mutex> guard(bufferLock);
    if (bufferA == NULL)
    {
        bufferA = buf;
        bufferALen = len;
    }
    else if (bufferB == NULL)
    {
        bufferB = buf;
        bufferBLen = len;
    }
    else
    {
        delete[] buf; // needs to be dropped now
    }
}
// Can return 0 if none are pending
int ConsumeOneThreadSample(int len, unsigned char* buf)
{
    unsigned char* toUse = NULL;
    int toUseLen = 0;
    {
        std::lock_guard<std::mutex> guard(bufferLock);
        if (bufferA != NULL)
        {
            toUse = bufferA;
            toUseLen = bufferALen;
            bufferA = NULL;
            bufferALen = 0;            
        }
        else if (bufferB != NULL)
        {
            toUse = bufferB;
            toUseLen = bufferBLen;
            bufferB = NULL;
            bufferBLen = 0;
        }
    }
    if (toUse == NULL)
    {
        return 0;
    }
    if (len >= toUseLen)
    {
        memcpy(buf, toUse, toUseLen);
    }
    delete[] toUse;
    return toUseLen;
}



namespace trace {

template <class MetaInterface>
class COMPtrHolder {
 public:
  COMPtrHolder() { m_ptr = NULL; }

  COMPtrHolder(MetaInterface* ptr) {
      if (ptr != NULL)
      {
          ptr->AddRef();
      }
    m_ptr = ptr;
  }

  ~COMPtrHolder() {
    if (m_ptr != NULL) {
      m_ptr->Release();
      m_ptr = NULL;
    }
  }
  MetaInterface* operator->() { return m_ptr; }

  MetaInterface** operator&() {
    // _ASSERT(m_ptr == NULL);
    return &m_ptr;
  }

  operator MetaInterface*() { return m_ptr; }

 private:
  MetaInterface* m_ptr;
};

class ThreadSamplesBuffer {
 public:
  unsigned char* buffer;
  unsigned int pos; // FIXME 
  std::unordered_map<FunctionID, int> codes;

  ThreadSamplesBuffer(unsigned char* buf): buffer(buf), pos(0) {
  }
  ~ThreadSamplesBuffer() { 
    buffer = NULL; // specifically don't delete[] as ownership/lifecycle is complicated
  }
  void StartBatch() {
      // FIXME include current time, basic version nunmber, etc. etc.
    writeByte(0x01);
  }

  void StartSample(ThreadID id, ThreadState* state) { 
    writeByte(0x02);
    writeInt(id);
    writeInt(state->nativeId);
    writeString(state->threadName);
  }
  // FIXME line numbers
  void RecordFrame(FunctionID fid, WSTRING &frame) { 
    writeCodedFrameString(fid, frame);
  }
  void EndSample() { 
      writeShort(0);
  }
  void EndBatch() { 
      writeByte(0x06); 
      // FIXME include metadata, like overhead time
      printf("end batch in %i bytes\n", pos);
  }

  private:
  void writeCodedFrameString(FunctionID fid, WSTRING& str) {
    auto found = codes.find(fid);
    if (found != codes.end()) {
        writeShort(found->second);
    } else {
        int code = codes.size() + 1;
        if (codes.size() + 1 < MAX_CODES_PER_BUFFER)
        {
            codes[fid] = code;
        }
        writeShort( -code );  // note negative sign indiciating definition of code
        writeString(str);
    }
  }
  void writeShort(int16_t val) {
    if (pos + 2 >= SAMPLES_BUFFER_SIZE) {
      return;
    }
    int16_t bigEnd = _byteswap_ushort(val);
    memcpy(&buffer[pos], &bigEnd, 2);
    pos += 2;
  }
  void writeInt(int32_t val) {
    if (pos + 4 >= SAMPLES_BUFFER_SIZE) {
      return;
    }
    int32_t bigEnd = _byteswap_ulong(val);
    memcpy(&buffer[pos], &bigEnd, 4);
    pos += 4;
  }
  void writeString(WSTRING & str) {
      // limit strings to a max length overall; this prevents (e.g.) thread names or 
      // any other miscellaneous strings that come along from blowing things out
      size_t usedLen = min(str.length(), MAX_STRING_LENGTH);
      if (pos + 4 + 2 * usedLen >= SAMPLES_BUFFER_SIZE)
      {
        return;
      }
      writeInt(str.length());
      memcpy(&buffer[pos], &str[0], 2 * usedLen);
      pos += 2 * usedLen;
  }
  void writeByte(unsigned char b) {
      if (pos + 1 >= SAMPLES_BUFFER_SIZE) {
      return;
    }
      buffer[pos] = b;
      pos++;
  }
 };


    class SamplingHelper {
     public:
      ICorProfilerInfo10* info10 = NULL;
      ThreadSamplesBuffer* curWriter = NULL;
      unsigned char* curBuffer = NULL;
      bool AllocateBuffer() { 
          bool should = ShouldProduceThreadSample();
          if (!should)
          {
              return should;
          }
          curBuffer = new unsigned char[SAMPLES_BUFFER_SIZE];
          curWriter = new ThreadSamplesBuffer(curBuffer);
          return should;
      }
      void PublishBuffer() {
          RecordProducedThreadSample((int)curWriter->pos, curBuffer);
          delete curWriter;
          curWriter = NULL;
          curBuffer = NULL;
      }

      private:
       std::unordered_map<FunctionID, WSTRING*> nameCache; // FIXME replace with real multi-level LRU anbd also figure out string ownership to not leak memory forever

          
      // FIXME audit this copy-and-paste, clean it up (e.g., prints), improve performance (even after LRU), doc its origins in the sample code
       // FIXME quick prototype (based on disabling the cache) suggests that this can be sped up around 20% by not double/triple-copying the class names and just using a single WCHAR buffer
      WSTRING GetClassName(ClassID classId) {
        ModuleID modId;
        mdTypeDef classToken;
        ClassID parentClassID;
        HRESULT hr = S_OK;

        if (classId == NULL) {
          Logger::Debug("NULL classId passed to GetClassName");
          return WStr("Unknown");
        }

        hr = info10->GetClassIDInfo2(classId, &modId, &classToken,
                                              &parentClassID, 0,
                                              NULL, NULL);
        if (CORPROF_E_CLASSID_IS_ARRAY == hr) {
          // We have a ClassID of an array.
          return WStr("ArrayClass");
        } else if (CORPROF_E_CLASSID_IS_COMPOSITE == hr) {
          // We have a composite class
          return WStr("CompositeClass");
        } else if (CORPROF_E_DATAINCOMPLETE == hr) {
          // type-loading is not yet complete. Cannot do anything about it.
          return WStr("DataIncomplete");
        } else if (FAILED(hr)) {
          Logger::Debug("GetClassIDInfo failed: ", hr);
          return WStr("Unknown");
        }

        COMPtrHolder<IMetaDataImport> pMDImport;
        hr = info10->GetModuleMetaData(modId, (ofRead | ofWrite),
                                                IID_IMetaDataImport,
                                                (IUnknown**)&pMDImport);
        if (FAILED(hr)) {
          Logger::Debug("GetModuleMetaData failed: ", hr);
          return WStr("Unknown");
        }

        WCHAR wName[MAX_CLASS_NAME_LEN];
        DWORD dwTypeDefFlags = 0;
        hr = pMDImport->GetTypeDefProps(classToken, wName, MAX_CLASS_NAME_LEN, NULL,
                                        &dwTypeDefFlags, NULL);
        if (FAILED(hr)) {
          Logger::Debug("GetTypeDefProps failed: ", hr);
          return WStr("Unknown");
        }

        WSTRING name = WStr("");
        name += wName;

        return name;
      }

      WSTRING GetFunctionName(FunctionID funcID,
                                       const COR_PRF_FRAME_INFO frameInfo) {
        if (funcID == NULL) {
          return WStr("Unknown_Native_Function");
        }

        ClassID classId = NULL;
        ModuleID moduleId = NULL;
        mdToken token = NULL;

        HRESULT hr = info10->GetFunctionInfo2(
            funcID, frameInfo, &classId, &moduleId, &token, 0,
            NULL,  NULL);
        if (FAILED(hr)) {
          Logger::Debug("GetFunctionInfo2 failed: ", hr);
        }

        COMPtrHolder<IMetaDataImport> pIMDImport;
        hr = info10->GetModuleMetaData(
            moduleId, ofRead, IID_IMetaDataImport, (IUnknown**)&pIMDImport);
        if (FAILED(hr)) {
          Logger::Debug("GetModuleMetaData failed: ", hr);
        }

        WCHAR funcName[MAX_FUNC_NAME_LEN];
        funcName[0] = 0;
        hr = pIMDImport->GetMethodProps(token, NULL, funcName, MAX_FUNC_NAME_LEN, 0,
                                        0, NULL, NULL, NULL, NULL);
        if (FAILED(hr)) {
          Logger::Debug("GetMethodProps failed: ", hr);
        }

        WSTRING name;

        // If the ClassID returned from GetFunctionInfo is 0, then the function
        // is a shared generic function.
        if (classId != 0) {
          name += GetClassName(classId);
        } else {
          name += WStr("SharedGenericFunction");
        }

        name += WStr("::");

        name += funcName;

        // FIXME What about method signature to differentiate overloaded methods?

        return name;
      }

     public:

      WSTRING* Lookup(FunctionID fid, COR_PRF_FRAME_INFO frame) {
        WSTRING* answer = NULL;
        auto found = nameCache.find(fid);
        if (found != nameCache.end()) {
          answer = found->second;
        } else {
          answer = new WSTRING(
              this->GetFunctionName(fid, frame));  // FIXME again memory leak
          nameCache[fid] = answer; 
        }
        return answer;
      }
    };



    HRESULT __stdcall FrameCallback(
        _In_ FunctionID funcId, 
        _In_ UINT_PTR ip, 
        _In_ COR_PRF_FRAME_INFO frameInfo,
        _In_ ULONG32 contextSize,
        _In_ BYTE context[],
        _In_ void* clientData) {
      SamplingHelper* helper = (SamplingHelper*)clientData;
        WSTRING* name = helper->Lookup(funcId, frameInfo);
      helper->curWriter->RecordFrame(funcId, *name);
      return S_OK;
    }

    // Factored out from the loop to a separate function for easier auditing and control of the threadstate lock
    // FIXME return type void in the future; metadata written to output structure instead
    int CaptureSamples(ThreadSampler* ts, ICorProfilerInfo10* info10, SamplingHelper& helper) {
      ICorProfilerThreadEnum* threadEnum = NULL;
      HRESULT hr = info10->EnumThreads(&threadEnum);
      // FIXME check hr
      int totalThreads = 0;

      ThreadID threadID;
      ULONG numReturned = 0;

      helper.curWriter->StartBatch();

      std::lock_guard<std::mutex> guard(ts->threadStateLock);

      while ((hr = threadEnum->Next(1, &threadID, &numReturned)) == S_OK) {
        auto found = ts->managedTid2state.find(threadID);
        if (found != ts->managedTid2state.end() && found->second != NULL) {
            helper.curWriter->StartSample(threadID, found->second);
        } else {
          auto unknown = ThreadState();
            helper.curWriter->StartSample(threadID, &unknown);
        }

        totalThreads++;
        HRESULT localHr = info10->DoStackSnapshot(
            threadID, &FrameCallback, COR_PRF_SNAPSHOT_DEFAULT,
            &helper,
            NULL, 0);
        if (FAILED(hr))
        {
            Logger::Debug("DoStackSnapshot failed: ", hr);
        }
        helper.curWriter->EndSample();
      }
      helper.curWriter->EndBatch();
      return totalThreads;

    }

    int GetSamplingPeriod()
    {
        WSTRING val = GetEnvironmentValue(environment::thread_sampling_period);
        if (val.empty()) {
            return DEFAULT_SAMPLE_PERIOD;
        }
        try {
            return max(MINIMUM_SAMPLE_PERIOD, std::stoi(val));
        } catch (...) {
            return DEFAULT_SAMPLE_PERIOD;
        }
    }

    DWORD WINAPI SamplingThreadMain(_In_ LPVOID param) { 
        int sleepMillis = GetSamplingPeriod();
        ThreadSampler* ts = (ThreadSampler*)param;
        ICorProfilerInfo10* info10 = ts->info10;
        HRESULT hr;
        SamplingHelper helper;
        helper.info10 = info10;

        while (1) {
          Sleep(sleepMillis);
          bool shouldSample = helper.AllocateBuffer();
          if (!shouldSample) {
              Logger::Warn("Skipping a thread sample period, buffers are full");
              // FIXME might like stats on how often this happens
              continue; 
          }
          int totalThreads = 0;

          LARGE_INTEGER start, end, elapsedMicros, frequency;
          QueryPerformanceFrequency(&frequency);
          QueryPerformanceCounter(&start);

          hr = info10->SuspendRuntime();
          if (FAILED(hr)) {
              Logger::Warn("Could not suspend runtime to sample threads: ", hr);
          } else {
              totalThreads = CaptureSamples(ts, info10, helper);
          }
          // I don't have any proof but I sure hope that if suspending fails then it's still ok to ask to resume, with no ill effects
          hr = info10->ResumeRuntime();

          QueryPerformanceCounter(&end);
          helper.PublishBuffer();

          elapsedMicros.QuadPart = end.QuadPart - start.QuadPart;
          elapsedMicros.QuadPart *= 1000000;
          elapsedMicros.QuadPart /= frequency.QuadPart;
          printf("Resuming runtime (%i threads) after %lli micros\n", (int) totalThreads, (long long) elapsedMicros.QuadPart);
        }

        return 0;
    }

    void ThreadSampler::StartSampling(ICorProfilerInfo3* info3) { 
      Logger::Info("ThreadSampler::StartSampling");
      HRESULT hr = info3->QueryInterface<ICorProfilerInfo10>(&this->info10);
      if (FAILED(hr)) {
          Logger::Error("Can't get ICorProfilerInfo10; thread sampling will not run: ", hr);
          return;
      }
      HANDLE bgThread =
          CreateThread(NULL, 0, &SamplingThreadMain, this, 0, NULL);
    }

    void ThreadSampler::ThreadCreated(ThreadID threadId) {
        // FIXME why do the thread* things arrive out of order?
        // deliberate nop since the actual value carriers will fault in.
        // hopefully the destroyed event is not called out of order with the others...
    }
    void ThreadSampler::ThreadDestroyed(ThreadID threadId) {
      std::lock_guard<std::mutex> guard(threadStateLock);

      ThreadState* state = managedTid2state[threadId];
      if (state != NULL) {
        delete state;
      }
      managedTid2state.erase(threadId);
    }
    void ThreadSampler::ThreadAssignedToOSThread(ThreadID threadId,
        DWORD osThreadId) {
      std::lock_guard<std::mutex> guard(threadStateLock);
  
      ThreadState* state = managedTid2state[threadId];
      if (state == NULL) {
        state = new ThreadState();
        managedTid2state[threadId] = state;
      }
      state->nativeId = osThreadId;
    }
    void ThreadSampler::ThreadNameChanged(ThreadID threadId, ULONG cchName,
        WCHAR _name[]) {
      std::lock_guard<std::mutex> guard(threadStateLock);

      ThreadState* state = managedTid2state[threadId];
      if (state == NULL) {
        state = new ThreadState();
        managedTid2state[threadId] = state;
      }
      state->threadName.append(_name, cchName);
    }
 }

 extern "C"
 {
     __declspec(dllexport) int signalfx_read_thread_samples(int len, unsigned char* buf)
     {
         return ConsumeOneThreadSample(len, buf);
     }
 }
