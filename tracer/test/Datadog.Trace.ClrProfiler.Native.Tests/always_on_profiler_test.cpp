#include "pch.h"

#include <codecvt>
#include <locale>
#include <string>
#include <vector>

#include "../../src/Datadog.Trace.ClrProfiler.Native/always_on_profiler.h"

using namespace always_on_profiler;

TEST(AlwaysOnProfilerTest, ThreadStateTracking)
{
    AlwaysOnProfiler ts; // Do NOT call StartSampling on this, which will create a background thread, etc.
    ts.ThreadNameChanged(1, 6, const_cast<WCHAR*>(L"Sample"));
    ts.ThreadCreated(1);
    ts.ThreadNameChanged(2, 7, const_cast<WCHAR*>(L"Thread1"));
    ts.ThreadNameChanged(2, 6, const_cast<WCHAR*>(L"thread"));
    ts.ThreadCreated(2);
    EXPECT_EQ(L"Sample", ts.managed_tid_to_state_[1]->thread_name_);
    EXPECT_EQ(L"thread", ts.managed_tid_to_state_[2]->thread_name_);
    ts.ThreadDestroyed(1);
    ts.ThreadDestroyed(2);
    EXPECT_EQ(0, ts.managed_tid_to_state_.size());
}

TEST(AlwaysOnProfilerTest, BasicBufferBehavior)
{
    auto buf = std::vector<unsigned char>();
    shared::WSTRING longThreadName;
    for (int i = 0; i < 400; i++) {
        longThreadName.append(WStr("blah blah "));
    }
    const shared::WSTRING frame1 = WStr("SomeFairlyLongClassName::SomeMildlyLongMethodName");
    const shared::WSTRING frame2 = WStr("SomeFairlyLongClassName::ADifferentMethodName");
    ThreadSamplesBuffer tsb(&buf);
    ThreadState threadState;
    threadState.thread_name_.append(longThreadName);

    tsb.StartBatch();
    tsb.StartSample(1, &threadState, thread_span_context());
    tsb.RecordFrame(7001, frame1);
    tsb.RecordFrame(7002, frame2);
    tsb.RecordFrame(7001, frame1);
    tsb.EndSample();
    tsb.EndBatch();
    tsb.WriteFinalStats(SamplingStatistics());
    ASSERT_EQ(1290, tsb.buffer_->size()); // not manually calculated but does depend on thread name limiting and not repeating frame strings
    ASSERT_EQ(2, tsb.codes_.size());
}
TEST(AlwaysOnProfilerTest, AllocationSampleBuffer)
{
    auto buf = std::vector<unsigned char>();
    shared::WSTRING longThreadName;
    for (int i = 0; i < 400; i++)
    {
        longThreadName.append(WStr("blah blah "));
    }
    const shared::WSTRING frame1 = WStr("SomeFairlyLongClassName::SomeMildlyLongMethodName");
    const shared::WSTRING frame2 = WStr("SomeFairlyLongClassName::ADifferentMethodName");
    const shared::WSTRING typeName = WStr("ThisIsMyTypeName");
    ThreadSamplesBuffer tsb(&buf);
    ThreadState threadState;
    threadState.thread_name_.append(longThreadName);

    tsb.AllocationSample(32, typeName.c_str(), typeName.length(), 1, &threadState, thread_span_context());
    tsb.RecordFrame(7001, frame1);
    tsb.RecordFrame(7002, frame2);
    tsb.RecordFrame(7001, frame1);
    tsb.EndSample();

    ASSERT_EQ(1309, tsb.buffer_->size()); // not manually calculated
}

TEST(AlwaysOnProfilerTest, BufferOverrunBehavior)
{
    auto buf = std::vector<unsigned char>();
    shared::WSTRING long_thread_name;
    for (int i = 0; i < 400; i++)
    {
        long_thread_name.append(WStr("blah blah "));
    }
    shared::WSTRING frame1 = WStr("SomeFairlyLongClassName::SomeMildlyLongMethodName");
    shared::WSTRING frame2 = WStr("SomeFairlyLongClassName::ADifferentMethodName");
    ThreadSamplesBuffer tsb(&buf);

    ThreadState threadState;
    threadState.thread_name_.append(long_thread_name);
   
    // Now span a bunch of data and ensure we don't overflow (too much)
    for (int i = 0; i < 100000; i++)
    {
        tsb.StartBatch();
        tsb.StartSample(1, &threadState, thread_span_context());
        tsb.RecordFrame(7001, frame1);
        tsb.RecordFrame(7002, frame2);
        tsb.RecordFrame(7001, frame1);
        tsb.EndSample();
        tsb.EndBatch();
        tsb.WriteFinalStats(SamplingStatistics());
    }
    // 200k buffer plus one more thread entry before it stops adding more
    ASSERT_TRUE(buf.size() < 210000 && buf.size() >= 200000);
}

TEST(AlwaysOnProfilerTest, StaticBufferManagement)
{
    const auto buf_a = new std::vector<unsigned char>();
    buf_a->resize(1);
    std::fill(buf_a->begin(), buf_a->end(), 'A');
    const auto buf_b = new std::vector<unsigned char>();
    buf_b->resize(2);
    std::fill(buf_b->begin(), buf_b->end(), 'B');
    const auto buf_c = new std::vector<unsigned char>();
    buf_c->resize(4);
    std::fill(buf_c->begin(), buf_c->end(), 'C');
    unsigned char read_buf[4];
    ASSERT_EQ(true, ThreadSamplingShouldProduceThreadSample());
    ASSERT_EQ(0, ThreadSamplingConsumeOneThreadSample(4, read_buf));

    ThreadSamplingRecordProducedThreadSample(buf_a);
    ThreadSamplingRecordProducedThreadSample(buf_b);
    ASSERT_EQ(false, ThreadSamplingShouldProduceThreadSample());

    ThreadSamplingRecordProducedThreadSample(buf_c); // no-op (but deletes the buf)
    ASSERT_EQ(false, ThreadSamplingShouldProduceThreadSample());

    ASSERT_EQ(1, ThreadSamplingConsumeOneThreadSample(4, read_buf));
    ASSERT_EQ('A', read_buf[0]);
    ASSERT_EQ(2, ThreadSamplingConsumeOneThreadSample(4, read_buf));
    ASSERT_EQ('B', read_buf[0]);
    ASSERT_EQ(0, ThreadSamplingConsumeOneThreadSample(4, read_buf));

    const auto buf_d = new std::vector<unsigned char>();
    buf_d->resize(4);
    std::fill(buf_d->begin(), buf_d->end(), 'D');
    ThreadSamplingRecordProducedThreadSample(buf_d);
    ASSERT_EQ(4, ThreadSamplingConsumeOneThreadSample(4, read_buf));
    ASSERT_EQ('D', read_buf[0]);

    // Finally, publish something too big for readBuf and ensure nothing explodes
    const auto buf_e = new std::vector<unsigned char>();
    buf_e->resize(5);
    std::fill(buf_e->begin(), buf_e->end(), 'E');
    ThreadSamplingRecordProducedThreadSample(buf_e);
    ASSERT_EQ(4, ThreadSamplingConsumeOneThreadSample(4, read_buf));
    ASSERT_EQ('E', read_buf[0]);
}

TEST(AlwaysOnProfilerTest, AllocationBufferBehavior)
{
    unsigned char read_buf[4];
    unsigned char write_buf[] = {5, 6, 7, 8};
    // Invalid inputs don't blow up
    ASSERT_EQ(0, SignalFxReadAllocationSamples(0, read_buf));
    ASSERT_EQ(0, SignalFxReadAllocationSamples(4, NULL));
    AllocationSamplingAppendToBuffer(0, NULL);
    // No data->0 result
    ASSERT_EQ(0, SignalFxReadAllocationSamples(4, read_buf));
    AllocationSamplingAppendToBuffer(1, write_buf);
    ASSERT_EQ(1, SignalFxReadAllocationSamples(4, read_buf));
    ASSERT_EQ(0, SignalFxReadAllocationSamples(4, read_buf));
    AllocationSamplingAppendToBuffer(1, write_buf);
    AllocationSamplingAppendToBuffer(2, write_buf);
    ASSERT_EQ(3, SignalFxReadAllocationSamples(4, read_buf));
    ASSERT_EQ(0, SignalFxReadAllocationSamples(4, read_buf));

    // Now test overrun
    int unitSize = 6 * 1024;
    unsigned char* buf = (unsigned char*) calloc(100 * unitSize, 1);
    ASSERT_TRUE(buf != NULL);
    for (int i = 0; i < 100; i++)
    {
        AllocationSamplingAppendToBuffer(unitSize, buf);
    }
    int32_t amountRead = SignalFxReadAllocationSamples(100 * unitSize, buf);
    ASSERT_EQ(198 * 1024, amountRead);
}


TEST(AlwaysOnProfilerTest, LRUCache)
{
    constexpr int max = 10000;
    NameCache<FunctionID, std::pair<shared::WSTRING*, FunctionIdentifier>> cache(max, std::pair<shared::WSTRING*, FunctionIdentifier>(nullptr, {}));
    for (int i = 1; i <= max; i++)
    {
        ASSERT_EQ(NULL, cache.Get(i).first);
        auto val = new shared::WSTRING(L"Function ");
        val->append(std::to_wstring(i));
        cache.Put(i, std::pair<shared::WSTRING*, FunctionIdentifier>(val, {}));
        ASSERT_EQ(val, cache.Get(i).first);
    }

    // Now cache is full; add another and item 1 gets kicked out
    auto* func_max_plus1 = new shared::WSTRING(L"Function max+1");
    ASSERT_EQ(NULL, cache.Get(max + 1).first);
    cache.Put(max + 1, std::pair<shared::WSTRING*, FunctionIdentifier>(func_max_plus1, {}));
    ASSERT_EQ(NULL, cache.Get(1).first);
    ASSERT_EQ(func_max_plus1, cache.Get(max + 1).first);
    
    // Put 1 back, 2 falls off and everything else is there
    const auto func1 = new shared::WSTRING(L"Function 1");
    cache.Put(1, std::pair<shared::WSTRING*, FunctionIdentifier>(func1, {}));
    ASSERT_EQ(NULL, cache.Get(2).first);
    ASSERT_EQ(func1, cache.Get(1).first);
    ASSERT_EQ(func_max_plus1, cache.Get(max + 1).first);
    for (int i = 3; i <= max; i++) {
        ASSERT_EQ(true, cache.Get(i).first != NULL);
    }

    // test clear cache
    cache.Clear();
    for (int i = 1; i <= max; i++)
    {
        ASSERT_EQ(NULL, cache.Get(i).first);
    }
}

TEST(AlwaysOnProfilerTest, AllocationSubSampler)
{
    AllocationSubSampler samp(10, 1);
    // Cycle 1: 20, of which the first 10 should be chosen
    for (int i = 0; i < 10; i++)
    {
        ASSERT_EQ(true, samp.ShouldSample());
    }
    for (int i = 0; i < 10; i++)
    {
        ASSERT_EQ(false, samp.ShouldSample());
    }
    // Cycle 2: 100, of which 10 should be chosen
    samp.AdvanceCycle(
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch()));
    std::vector<bool> c2;
    int c2Total = 0;
    for (int i = 0; i < 100; i++)
    {
        bool sample = samp.ShouldSample();
        c2.push_back(sample);
        if (sample)
        {
            c2Total++;
        }
    }
    // but not, statistically speaking, the first 8 in a row
    ASSERT_FALSE(c2[0] && c2[1] && c2[2] && c2[3] && c2[4] && c2[5] && c2[6] && c2[7]);
    ASSERT_EQ(10, c2Total);

    // Cycle 3: 10, of which statistically 1 is chosen, relaxed to <= 4
    samp.AdvanceCycle(
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch()));
    int c3Total = 0;
    for (int i = 0; i < 10; i++)
    {
        if (samp.ShouldSample())
        {
            c3Total++;
        }
    }
    ASSERT_TRUE(c3Total <= 4);

    // Now start over and test the actual time advancement logic
    AllocationSubSampler timedSamp(2, 1);
    ASSERT_TRUE(timedSamp.ShouldSample());
    ASSERT_TRUE(timedSamp.ShouldSample());
    ASSERT_FALSE(timedSamp.ShouldSample());
    ASSERT_FALSE(timedSamp.ShouldSample());
    // advance cycle
    std::this_thread::sleep_for(std::chrono::milliseconds(1500));
    timedSamp.ShouldSample(); // 50/50 chance on this, no assert
    // advance cycle again - with lastCycle == 1, the next should be guaranteed reset of state
    std::this_thread::sleep_for(std::chrono::milliseconds(1500));
    ASSERT_TRUE(timedSamp.ShouldSample());
    ASSERT_TRUE(timedSamp.ShouldSample());
    ASSERT_FALSE(timedSamp.ShouldSample());
    ASSERT_FALSE(timedSamp.ShouldSample());
}
