using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.SignalFx.Metrics;

public class BufferingWorkerTests
{
    [Fact]
    public void If_buffer_size_is_exceeded_then_its_flushed_and_cleared()
    {
        var metricExporter = new TestMetricExporter();
        var message = new DataPointUploadMessage();

        var bufferSize = 1;
        var worker = new BufferingWorker(metricExporter, bufferSize, () => message);

        worker.OnNewValue(new DataPoint());

        metricExporter.SentMessages.Count.Should().Be(1);
        message.datapoints.Count.Should().Be(0);
    }

    [Fact]
    public void Single_upload_message_is_reused_between_invocations()
    {
        var metricExporter = new TestMetricExporter();
        var message = new DataPointUploadMessage();
        var bufferSize = 1;

        var worker = new BufferingWorker(metricExporter, bufferSize, () => message);

        var firstDatapoint = new DataPoint
        {
            metric = "metric1",
            source = "source1",
            value = new Datum { doubleValue = 1.1 },
            dimensions = { new Dimension { key = "key1", value = "val1" } }
        };
        worker.OnNewValue(firstDatapoint);

        var secondDatapoint = new DataPoint
        {
            metric = "metric2",
            source = "source2",
            value = new Datum { doubleValue = 2.2 },
            dimensions = { new Dimension { key = "key2", value = "val2" } }
        };
        worker.OnNewValue(secondDatapoint);

        metricExporter.SentMessages.Should().HaveCount(2);
        metricExporter.SentMessages.Should().OnlyContain(m => m == message);

        metricExporter.Snapshots[0].Should().HaveCount(1).And.Contain(firstDatapoint);
        metricExporter.Snapshots[1].Should().HaveCount(1).And.Contain(secondDatapoint);
    }

    [Fact]
    public void Flush_is_triggered_only_when_buffer_is_full()
    {
        var metricExporter = new TestMetricExporter();
        var message = new DataPointUploadMessage();
        var bufferSize = 3;

        var worker = new BufferingWorker(metricExporter, bufferSize, () => message);

        var firstDatapoint = new DataPoint();
        worker.OnNewValue(firstDatapoint);
        message.datapoints.Count.Should().Be(1);
        metricExporter.SentMessages.Should().BeEmpty();

        var secondDatapoint = new DataPoint();
        worker.OnNewValue(secondDatapoint);
        message.datapoints.Count.Should().Be(2);
        metricExporter.SentMessages.Should().BeEmpty();

        var thirdDatapoint = new DataPoint();
        worker.OnNewValue(thirdDatapoint);

        message.datapoints.Count.Should().Be(0);

        // verify all 3 data points were sent
        metricExporter.SentMessages.Should().HaveCount(1);
        metricExporter.Snapshots[0].Should().BeEquivalentTo(new[] { firstDatapoint, secondDatapoint, thirdDatapoint });
    }

    [Fact]
    public void Forced_flush_sends_current_buffer_contents()
    {
        var metricExporter = new TestMetricExporter();
        var message = new DataPointUploadMessage();

        var bufferSize = 5;
        var worker = new BufferingWorker(metricExporter, bufferSize, () => message);

        worker.OnNewValue(new DataPoint());
        metricExporter.SentMessages.Should().HaveCount(0);
        message.datapoints.Count.Should().Be(1);

        worker.Flush();
        metricExporter.SentMessages.Should().HaveCount(1);
        message.datapoints.Count.Should().Be(0);
    }

    private class TestMetricExporter : ISignalFxMetricExporter
    {
        public IList<DataPointUploadMessage> SentMessages { get; } = new List<DataPointUploadMessage>();

        public List<List<DataPoint>> Snapshots { get; } = new List<List<DataPoint>>();

        public void Send(DataPointUploadMessage msg)
        {
            SentMessages.Add(msg);
            Snapshots.Add(msg.datapoints.ToList());
        }
    }
}
