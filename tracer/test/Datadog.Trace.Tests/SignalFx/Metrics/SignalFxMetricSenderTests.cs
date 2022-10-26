using System.Collections.Generic;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.SignalFx.Metrics
{
    public class SignalFxMetricSenderTests
    {
        [Fact]
        public void Gauge_double_metrics_are_sent()
        {
            var testWriter = new TestWriter();
            var sender = new SignalFxMetricSender(new[] { "tag1:v1" }, Mock.Of<ISignalFxMetricExporter>(), 100, (_, _) => testWriter);

            sender.SendDouble("test_metric", 1.11, MetricType.GAUGE, new[] { "tag2:v2" });

            var dataPointUploadMessages = testWriter.SentMessages;
            dataPointUploadMessages.Should().HaveCount(1);
            var dataPoint = dataPointUploadMessages[0];

            AssertCommon(dataPoint);

            dataPoint.value.doubleValue.Should().Be(1.11);
            dataPoint.metricType.Should().Be(MetricType.GAUGE);
        }

        [Fact]
        public void Gauge_long_metrics_are_sent()
        {
            var testWriter = new TestWriter();
            var sender = new SignalFxMetricSender(new[] { "tag1:v1" }, Mock.Of<ISignalFxMetricExporter>(), 100, (_, _) => testWriter);

            sender.SendLong("test_metric", 1, MetricType.GAUGE, new[] { "tag2:v2" });

            var dataPointUploadMessages = testWriter.SentMessages;
            dataPointUploadMessages.Should().HaveCount(1);
            var dataPoint = dataPointUploadMessages[0];

            AssertCommon(dataPoint);

            dataPoint.value.intValue.Should().Be(1);
            dataPoint.metricType.Should().Be(MetricType.GAUGE);
        }

        [Fact]
        public void Counter_double_metrics_are_sent()
        {
            var testWriter = new TestWriter();
            var sender = new SignalFxMetricSender(new[] { "tag1:v1" }, Mock.Of<ISignalFxMetricExporter>(), 100, (_, _) => testWriter);

            sender.SendDouble("test_metric", 1.11, MetricType.COUNTER, new[] { "tag2:v2" });

            var dataPointUploadMessages = testWriter.SentMessages;
            dataPointUploadMessages.Should().HaveCount(1);
            var dataPoint = dataPointUploadMessages[0];

            AssertCommon(dataPoint);

            dataPoint.value.doubleValue.Should().Be(1.11);
            dataPoint.metricType.Should().Be(MetricType.COUNTER);
        }

        [Fact]
        public void Counter_long_metrics_are_sent()
        {
            var testWriter = new TestWriter();
            var sender = new SignalFxMetricSender(new[] { "tag1:v1" }, Mock.Of<ISignalFxMetricExporter>(), 100, (_, _) => testWriter);

            sender.SendLong("test_metric", 1, MetricType.COUNTER, new[] { "tag2:v2" });

            var dataPointUploadMessages = testWriter.SentMessages;
            dataPointUploadMessages.Should().HaveCount(1);
            var dataPoint = dataPointUploadMessages[0];

            AssertCommon(dataPoint);

            dataPoint.value.intValue.Should().Be(1);
            dataPoint.metricType.Should().Be(MetricType.COUNTER);
        }

        [Fact]
        public void Cumulative_counter_metrics_are_sent()
        {
            var testWriter = new TestWriter();
            var sender = new SignalFxMetricSender(new[] { "tag1:v1" }, Mock.Of<ISignalFxMetricExporter>(), 100, (_, _) => testWriter);

            sender.SendLong("test_metric", 1, MetricType.CUMULATIVE_COUNTER, new[] { "tag2:v2" });

            var dataPointUploadMessages = testWriter.SentMessages;
            dataPointUploadMessages.Should().HaveCount(1);
            var dataPoint = dataPointUploadMessages[0];

            AssertCommon(dataPoint);

            dataPoint.value.intValue.Should().Be(1);
            dataPoint.metricType.Should().Be(MetricType.CUMULATIVE_COUNTER);
        }

        private static void AssertCommon(DataPoint dataPoint)
        {
            dataPoint.metric.Should().Be("test_metric");

            var dataPointDimensions = dataPoint.dimensions;

            dataPointDimensions.Should().HaveCount(2);

            dataPointDimensions[0].key.Should().Be("tag1");
            dataPointDimensions[0].value.Should().Be("v1");

            dataPointDimensions[1].key.Should().Be("tag2");
            dataPointDimensions[1].value.Should().Be("v2");
        }

        private class TestWriter : ISignalFxMetricWriter
        {
            public IList<DataPoint> SentMessages { get; } = new List<DataPoint>();

            public bool TryWrite(DataPoint msg)
            {
                SentMessages.Add(msg);
                return true;
            }

            public void Dispose()
            {
            }
        }
    }
}
