// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.AlwaysOnProfiler.Builder;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.Pprof.Proto.Profile.V1;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler
{
    public class PprofTests
    {
        private readonly Pprof _pprof = new();

        [Fact]
        public void GetLocationId()
        {
            var pprof = new Pprof();
            var locationId1 = pprof.GetLocationId("unknown", "A()", 0);
            var locationId2 = pprof.GetLocationId("unknown", "A()", 0);
            var locationId3 = pprof.GetLocationId("unknown", "B()", 0);

            var profile = pprof.ProfileBuilder.Build();

            locationId1.Should().Be(1);
            locationId2.Should().Be(1);
            locationId3.Should().Be(2);
            profile.StringTables.Should().HaveCount(4);
            profile.StringTables.Should().ContainInOrder(new List<string> { string.Empty, "unknown", "A()", "B()" });
        }

        [Fact]
        public void AddLabel()
        {
            var sampleBuilder = new SampleBuilder();

            _pprof.AddLabel(sampleBuilder, "long1", 100L);
            _pprof.AddLabel(sampleBuilder, "bool", true);
            _pprof.AddLabel(sampleBuilder, "long2", 32132L);
            _pprof.AddLabel(sampleBuilder, "string", "value");

            var sample = sampleBuilder.Build();
            var profile = _pprof.ProfileBuilder.Build();

            sample.Labels.Should().HaveCount(4);
            sample.Labels.Should().Contain(pair => pair.Key == 1 && pair.Num == 100L);
            sample.Labels.Should().Contain(pair => pair.Key == 2 && pair.Str == 3);
            sample.Labels.Should().Contain(pair => pair.Key == 4 && pair.Num == 32132L);
            sample.Labels.Should().Contain(pair => pair.Key == 5 && pair.Str == 6);
            profile.StringTables.Should().HaveCount(7);
            profile.StringTables.Should().ContainInOrder(new List<string> { string.Empty, "long1", "bool", "True", "long2", "string", "value" });
        }

        [Fact]
        public void GetStringId()
        {
            var id1 = _pprof.GetStringId("New string");
            var id2 = _pprof.GetStringId("Second string");
            var id3 = _pprof.GetStringId("New string");

            var profile = _pprof.ProfileBuilder.Build();

            id1.Should().Be(1);
            id2.Should().Be(2);
            id3.Should().Be(id1);
            profile.StringTables.Should().HaveCount(3);
            profile.StringTables.Should().ContainInOrder(new List<string> { string.Empty, "New string", "Second string" });
        }

        [Fact]
        public void Test2()
        {
            var threadSample = new ThreadSample
            {
                TraceIdHigh = 0,
                TraceIdLow = 0,
                Timestamp = new ThreadSample.Time(1),
                SpanId = 0,
                StackTrace = ".NET ThreadPool Worker #8 prio=0 os_prio=0 cpu=0 elapsed=0 tid=0xffffffff nid=0x26d0" +
                             "\n" +
                             "\n     at Interop.Kernel32.GetQueuedCompletionStatus(System.IntPtr, System.Int32&, System.UIntPtr&, System.IntPtr&, System.Int32)" +
                             "\n     at System.Threading.LowLevelLifoSemaphore.WaitForSignal(System.Int32)" +
                             "\n     at System.Threading.LowLevelLifoSemaphore.Wait(System.Int32, System.Boolean)" +
                             "\n     at System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()" +
                             "\n     at System.Threading.Thread.StartCallback()" +
                             "\n     at Unknown_Native_Function(unknown)",
                ThreadName = ".NET ThreadPool Worker #8"
            };

            threadSample.Frames.Add("Interop.Kernel32.GetQueuedCompletionStatus(System.IntPtr, System.Int32&, System.UIntPtr&, System.IntPtr&, System.Int32)");
            threadSample.Frames.Add("System.Threading.LowLevelLifoSemaphore.WaitForSignal(System.Int32)");
            threadSample.Frames.Add("System.Threading.LowLevelLifoSemaphore.Wait(System.Int32, System.Boolean)");
            threadSample.Frames.Add("System.Threading.PortableThreadPool.WorkerThread.WorkerThreadStart()");
            threadSample.Frames.Add("System.Threading.Thread.StartCallback()");
            threadSample.Frames.Add("Unknown_Native_Function(unknown)");

            var pprof = new Pprof();
            var sampleBuilder = new SampleBuilder();

            pprof.AddLabel(sampleBuilder, "source.event.time", threadSample.Timestamp.Milliseconds);

            if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
            {
                pprof.AddLabel(sampleBuilder, "span_id", threadSample.SpanId);
                pprof.AddLabel(sampleBuilder, "trace_id", $"{threadSample.TraceIdHigh:x16}{threadSample.TraceIdLow:x16}");
            }

            // if (stackTrace.isTruncated() || stackTrace.getFrames().size() > stackDepth)
            // {
            pprof.AddLabel(sampleBuilder, "thread.stack.truncated", true);
            // }

            foreach (var methodName in threadSample.Frames)
            {
                sampleBuilder.AddLocationId(pprof.GetLocationId("unknown", methodName, 0));
            }

            pprof.AddLabel(sampleBuilder, "thread.id", threadSample.ManagedId);
            pprof.AddLabel(sampleBuilder, "thread.name", threadSample.ThreadName);
            pprof.AddLabel(sampleBuilder, "thread.os.id", threadSample.NativeId);
            pprof.AddLabel(sampleBuilder, "thread.state", "RUNNABLE");

            pprof.ProfileBuilder.AddSample(sampleBuilder.Build());

            var profile = pprof.ProfileBuilder.Build();
            // var base64 = PprofThreadSampleExporter.Serialize(profile);

            var base64 = "H4sIAAAAAAAACpSRz07bQBDGib1eHP60C4cqysniUBxUrarNvVLDnwo1itImEUe0JFOw4uxG6zERT8AdXqTv0CfgWXiAdmIbUkQvtWT99ht/Mxrvt/tpg9c8nwV8d61ZD2uNu4dfj/cfmyz0hN/cDjcbv5+eGhW3xHaTh28aP28ZqbdC7PGwtufRy0OP6BF9ok9kREYMiAGRE/kB+UUQMaIneEFfrBdkIiwYiHpBLjYiptbUTmZzNwYJ12BQYjID9Q6vHOiJzFCPpxJdbsYaYaLY0OWg1nMzNXZh1OLUIDg7l1/BGUjbSn4B/JZDDpNDO5ungIk1A9SYZ/HgJkOYSeroo/sQrWRbvX+Wo/LzqvAP3VYt1anksFgzMZeyaxdd+oG0m/ywA5jp+ZV1IM90gifWDZJLo9P45YzT/5gR/936vEzH2hS0aamjV6P61qG+SKGs9Mkoz6ybgisLLwRdkMO4pfZfTanMheFQp+kFpUHGaFTe/3lPY3IN5ycUz/Km4yqXlqpX+SUTtVkdjV7mKnvHw2i1VFTuobYqk82WLU+K0kdQ4fdRr/e50z3+AwAA//8DACmj4NjLAgAA";
            var output = Convert.FromBase64String(base64);

            using var memoryStream = new MemoryStream(output);
            using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);

            var p = Serializer.Deserialize<Profile>(gzipStream);

            var a = p.StringTables;
        }
    }
}
