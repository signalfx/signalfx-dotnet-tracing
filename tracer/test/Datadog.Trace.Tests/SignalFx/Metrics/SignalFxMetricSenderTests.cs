using System.Collections.Generic;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.SignalFx.Metrics
{
    public class SignalFxMetricSenderTests
    {
        [Fact]
        public void Gauge_metric_types_are_sent()
        {
            var reporter = new TestWriter();
            var sender = new SignalFxMetricSender(reporter, new[] { "tag1:v1" });

            sender.SendGaugeMetric("test_metric", 1.11, tags: new[] { "tag2:v2" });

            var dataPointUploadMessages = reporter.SentMessages;
            dataPointUploadMessages.Should().HaveCount(1);
            var dataPoint = dataPointUploadMessages[0];

            AssertCommon(dataPoint);

            dataPoint.value.doubleValue.Should().Be(1.11);
            dataPoint.metricType.Should().Be(MetricType.GAUGE);
        }

        [Fact]
        public void Counter_metric_types_are_sent()
        {
            var reporter = new TestWriter();
            var sender = new SignalFxMetricSender(reporter, new[] { "tag1:v1" });

            sender.SendCounterMetric("test_metric", 1, tags: new[] { "tag2:v2" });

            var dataPointUploadMessages = reporter.SentMessages;
            dataPointUploadMessages.Should().HaveCount(1);
            var dataPoint = dataPointUploadMessages[0];

            AssertCommon(dataPoint);

            dataPoint.value.intValue.Should().Be(1);
            dataPoint.metricType.Should().Be(MetricType.COUNTER);
        }

        [Fact]
        public void Cumulative_counter_metric_types_are_sent()
        {
            var reporter = new TestWriter();
            var sender = new SignalFxMetricSender(reporter, new[] { "tag1:v1" });

            sender.SendCumulativeCounterMetric("test_metric", 1, tags: new[] { "tag2:v2" });

            var dataPointUploadMessages = reporter.SentMessages;
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
