#include "pch.h"

#include <codecvt>
#include <filesystem>
#include <fstream>
#include <locale>
#include <sstream>
#include <string>
#include <vector>

#include "../../src/Datadog.Trace.ClrProfiler.Native/ThreadSampler.h"

using namespace trace;

TEST(ThreadSamplerTest, ThreadStateTracking)
{
    ThreadSampler ts; // Do NOT call StartSampling on this, which will create a background thread, etc.
    ts.ThreadAssignedToOSThread(1, 1001);
    ts.ThreadNameChanged(1, 6, (WCHAR*) L"Sample");
    ts.ThreadCreated(1);
    ts.ThreadAssignedToOSThread(1, 1002);
    ts.ThreadNameChanged(2, 7, (WCHAR*) L"Thread1");
    ts.ThreadNameChanged(2, 6, (WCHAR*) L"thread");
    ts.ThreadCreated(2);
    EXPECT_EQ(1002, ts.managedTid2state[1]->nativeId);
    EXPECT_EQ(L"Sample", ts.managedTid2state[1]->threadName);
    EXPECT_EQ(L"thread", ts.managedTid2state[2]->threadName);
    ts.ThreadDestroyed(1);
    ts.ThreadDestroyed(2);
    EXPECT_EQ(0, ts.managedTid2state.size());
}

TEST(ThreadSamplerTest, BasicBufferBehavior)
{
    auto buf = std::vector<unsigned char>();
    WSTRING longThreadName = WStr("");
    for (int i = 0; i < 400; i++) {
        longThreadName.append(WStr("blah blah "));
    }
    WSTRING frame1 = WStr("SomeFairlyLongClassName::SomeMildlyLongMethodName");
    WSTRING frame2 = WStr("SomeFairlyLongClassName::ADifferentMethodName");
    ThreadSamplesBuffer tsb(&buf);
    ThreadState threadState;
    threadState.nativeId = 1000;
    threadState.threadName.append(longThreadName);

    tsb.StartBatch();
    tsb.StartSample(1, &threadState, ThreadSpanContext());
    tsb.RecordFrame(7001, frame1);
    tsb.RecordFrame(7002, frame2);
    tsb.RecordFrame(7001, frame1);
    tsb.EndSample();
    tsb.EndBatch();
    tsb.WriteFinalStats(100);
    ASSERT_EQ(1278, tsb.buffer->size()); // not manually calculated but does depend on thread name limiting and not repeating frame strings
    ASSERT_EQ(2, tsb.codes.size());
}

TEST(ThreadSamplerTest, BufferOverrunBehavior)
{
    auto buf = std::vector<unsigned char>();
    WSTRING longThreadName = WStr("");
    for (int i = 0; i < 400; i++)
    {
        longThreadName.append(WStr("blah blah "));
    }
    WSTRING frame1 = WStr("SomeFairlyLongClassName::SomeMildlyLongMethodName");
    WSTRING frame2 = WStr("SomeFairlyLongClassName::ADifferentMethodName");
    ThreadSamplesBuffer tsb(&buf);

    ThreadState threadState;
    threadState.nativeId = 1000;
    threadState.threadName.append(longThreadName);
   
    // Now span a bunch of data and ensure we don't overflow (too much)
    for (int i = 0; i < 100000; i++)
    {
        tsb.StartBatch();
        tsb.StartSample(1, &threadState, ThreadSpanContext());
        tsb.RecordFrame(7001, frame1);
        tsb.RecordFrame(7002, frame2);
        tsb.RecordFrame(7001, frame1);
        tsb.EndSample();
        tsb.EndBatch();
        tsb.WriteFinalStats(100);
    }
    // 200k buffer plus one more thread entry before it stops adding more
    ASSERT_EQ(buf.size(), 205432);
}

TEST(ThreadSamplerTest, StaticBufferManagement)
{
    auto bufA = new std::vector<unsigned char>();
    bufA->resize(1);
    std::fill(bufA->begin(), bufA->end(), 'A');
    auto bufB = new std::vector<unsigned char>();
    bufB->resize(2);
    std::fill(bufB->begin(), bufB->end(), 'B');
    auto bufC = new std::vector<unsigned char>();
    bufC->resize(4);
    std::fill(bufC->begin(), bufC->end(), 'C');
    unsigned char readBuf[4];
    ASSERT_EQ(true, ThreadSampling_ShouldProduceThreadSample());
    ASSERT_EQ(0, ThreadSampling_ConsumeOneThreadSample(4, readBuf));

    ThreadSampling_RecordProducedThreadSample(bufA);
    ThreadSampling_RecordProducedThreadSample(bufB);
    ASSERT_EQ(false, ThreadSampling_ShouldProduceThreadSample());

    ThreadSampling_RecordProducedThreadSample(bufC); // no-op (but deletes the buf)
    ASSERT_EQ(false, ThreadSampling_ShouldProduceThreadSample());

    ASSERT_EQ(1, ThreadSampling_ConsumeOneThreadSample(4, readBuf));
    ASSERT_EQ('A', readBuf[0]);
    ASSERT_EQ(2, ThreadSampling_ConsumeOneThreadSample(4, readBuf));
    ASSERT_EQ('B', readBuf[0]);
    ASSERT_EQ(0, ThreadSampling_ConsumeOneThreadSample(4, readBuf));

    auto bufD = new std::vector<unsigned char>();
    bufD->resize(4);
    std::fill(bufD->begin(), bufD->end(), 'D');
    ThreadSampling_RecordProducedThreadSample(bufD);
    ASSERT_EQ(4, ThreadSampling_ConsumeOneThreadSample(4, readBuf));
    ASSERT_EQ('D', readBuf[0]);

    // Finally, publish something too big for readBuf and ensure nothing explodes
    auto bufE = new std::vector<unsigned char>();
    bufE->resize(5);
    std::fill(bufE->begin(), bufE->end(), 'E');
    ThreadSampling_RecordProducedThreadSample(bufE);
    ASSERT_EQ(4, ThreadSampling_ConsumeOneThreadSample(4, readBuf));
    ASSERT_EQ('E', readBuf[0]);
}

TEST(ThreadSamplerTest, LRUCache)
{
    int max = 10000;
    NameCache cache(max);
    for (int i = 1; i <= max; i++)
    {
        ASSERT_EQ(NULL, cache.get(i));
        WSTRING* val = new WSTRING(L"Function ");
        val->append(std::to_wstring(i));
        cache.put(i, val);
        ASSERT_EQ(val, cache.get(i));
    }
    // Now cache is full; add another and item 1 gets kicked out
    WSTRING* funcMaxPlus1 = new WSTRING(L"Function max+1");
    ASSERT_EQ(NULL, cache.get(max+1));
    cache.put(max + 1, funcMaxPlus1);
    ASSERT_EQ(NULL, cache.get(1));
    ASSERT_EQ(funcMaxPlus1, cache.get(max+1));

    // Put 1 back, 2 falls off and everything else is there
    WSTRING* func1 = new WSTRING(L"Function 1");
    cache.put(1, func1);
    ASSERT_EQ(NULL, cache.get(2));
    ASSERT_EQ(func1, cache.get(1));
    ASSERT_EQ(funcMaxPlus1, cache.get(max+1));
    for (int i = 3; i <= max; i++) {
        ASSERT_EQ(true, cache.get(i) != NULL);
    }
}