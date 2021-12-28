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
            var reporter = new TestReporter();
            var sender = new SignalFxMetricSender(reporter, new[] { "tag1:v1" });

            sender.SendGaugeMetric("test_metric", 1.11, tags: new[] { "tag2:v2" });

            Assert(reporter.SentMessages, MetricType.GAUGE);
        }

        [Fact]
        public void Counter_metric_types_are_sent()
        {
            var reporter = new TestReporter();
            var sender = new SignalFxMetricSender(reporter, new[] { "tag1:v1" });

            sender.SendCounterMetric("test_metric", 1.11, tags: new[] { "tag2:v2" });

            Assert(reporter.SentMessages, MetricType.COUNTER);
        }

        private static void Assert(IList<DataPointUploadMessage> dataPointUploadMessages, MetricType expectedMetricType)
        {
            dataPointUploadMessages.Should().HaveCount(1);

            var sentMessage = dataPointUploadMessages[0];

            sentMessage.datapoints.Should().HaveCount(1);
            var dataPoint = sentMessage.datapoints[0];
            dataPoint.metric.Should().Be("test_metric");
            dataPoint.value.doubleValue.Should().Be(1.11);
            dataPoint.metricType.Should().Be(expectedMetricType);
            var dataPointDimensions = dataPoint.dimensions;

            dataPointDimensions.Should().HaveCount(2);

            dataPointDimensions[0].key.Should().Be("tag2");
            dataPointDimensions[0].value.Should().Be("v2");

            dataPointDimensions[1].key.Should().Be("tag1");
            dataPointDimensions[1].value.Should().Be("v1");
        }

        private class TestReporter : ISignalFxReporter
        {
            public IList<DataPointUploadMessage> SentMessages { get; } = new List<DataPointUploadMessage>();

            public void Send(DataPointUploadMessage msg)
            {
                SentMessages.Add(msg);
            }
        }
    }
}
