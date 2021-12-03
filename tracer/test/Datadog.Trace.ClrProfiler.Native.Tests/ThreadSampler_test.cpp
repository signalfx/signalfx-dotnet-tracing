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
    unsigned char buf[100 * 1024];
    WSTRING longThreadName = WStr("");
    for (int i = 0; i < 400; i++) {
        longThreadName.append(WStr("blah blah "));
    }
    WSTRING frame1 = WStr("SomeFairlyLongClassName::SomeMildlyLongMethodName");
    WSTRING frame2 = WStr("SomeFairlyLongClassName::ADifferentMethodName");
    ThreadSamplesBuffer tsb(buf);
    ThreadState threadState;
    threadState.nativeId = 1000;
    threadState.threadName.append(longThreadName);

    tsb.StartBatch();
    tsb.StartSample(1, &threadState);
    tsb.RecordFrame(7001, frame1);
    tsb.RecordFrame(7002, frame2);
    tsb.RecordFrame(7001, frame1);
    tsb.EndSample();
    tsb.EndBatch();
    tsb.WriteFinalStats(100);
    ASSERT_EQ(1260, tsb.pos); // not manually calculated but does depend on thread name limiting and not repeating frame strings
    ASSERT_EQ(2, tsb.codes.size());
}

TEST(ThreadSamplerTest, BufferOverrunBehavior)
{
    unsigned char buf[200 * 1024];
    memset(buf, 'x',  200 * 1024);
    memset(buf, 'V',  100 * 1024);
    WSTRING longThreadName = WStr("");
    for (int i = 0; i < 400; i++)
    {
        longThreadName.append(WStr("blah blah "));
    }
    WSTRING frame1 = WStr("SomeFairlyLongClassName::SomeMildlyLongMethodName");
    WSTRING frame2 = WStr("SomeFairlyLongClassName::ADifferentMethodName");
    ThreadSamplesBuffer tsb(buf);

    ThreadState threadState;
    threadState.nativeId = 1000;
    threadState.threadName.append(longThreadName);

    tsb.pos = 100 * 1024 - 1;
    // Now exerise some methods and ensure that no changes to the buffer or pos happen

    tsb.StartBatch();
    tsb.StartSample(1, &threadState);
    tsb.RecordFrame(7001, frame1);
    tsb.RecordFrame(7002, frame2);
    tsb.RecordFrame(7001, frame1);
    tsb.EndSample();
    tsb.EndBatch();
    tsb.WriteFinalStats(100);
    ASSERT_EQ(100 * 1024 - 1, tsb.pos);
    for (int i = 0; i < 100 * 1024; i++) {
        ASSERT_EQ('V', buf[i]);
    }
    for (int i = 100 * 1024; i < 200 * 1024; i++) {
        ASSERT_EQ('x', buf[i]);
    }
}

TEST(ThreadSamplerTest, StaticBufferManagement)
{
    unsigned char* bufA = new unsigned char[1];
    unsigned char* bufB = new unsigned char[2];
    unsigned char* bufC = new unsigned char[3];
    unsigned char readBuf[4];
    bufA[0] = 'A';
    bufB[0] = 'B';
    bufC[0] = 'C';
    ASSERT_EQ(true, ThreadSampling_ShouldProduceThreadSample());
    ASSERT_EQ(0, ThreadSampling_ConsumeOneThreadSample(4, readBuf));

    ThreadSampling_RecordProducedThreadSample(1, bufA);
    ThreadSampling_RecordProducedThreadSample(2, bufB);
    ASSERT_EQ(false, ThreadSampling_ShouldProduceThreadSample());

    ThreadSampling_RecordProducedThreadSample(3, bufC); // no-op (but deletes the buf)
    ASSERT_EQ(false, ThreadSampling_ShouldProduceThreadSample());

    ASSERT_EQ(1, ThreadSampling_ConsumeOneThreadSample(4, readBuf));
    ASSERT_EQ('A', readBuf[0]);
    ASSERT_EQ(2, ThreadSampling_ConsumeOneThreadSample(4, readBuf));
    ASSERT_EQ('B', readBuf[0]);
    ASSERT_EQ(0, ThreadSampling_ConsumeOneThreadSample(4, readBuf));

    unsigned char* bufD = new unsigned char[4];
    bufD[0] = 'D';
    ThreadSampling_RecordProducedThreadSample(4, bufD);
    ASSERT_EQ(4, ThreadSampling_ConsumeOneThreadSample(4, readBuf));
    ASSERT_EQ('D', readBuf[0]);
}