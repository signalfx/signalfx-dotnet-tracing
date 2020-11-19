// Modified by SignalFx
using System;
using System.Collections.Immutable;
using System.Threading;
using Datadog.Trace.Configuration;
using Datadog.Trace.DogStatsd;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.Vendors.StatsdClient;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Tests
{
    public class DogStatsDTests
    {
        private readonly ITestOutputHelper _output;

        public DogStatsDTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Do_not_send_metrics_when_disabled()
        {
            var statsd = new Mock<IStatsd>();
            var spans = SendSpan(tracerMetricsEnabled: false, statsd);

            Assert.True(spans.Count == 1, "Expected one span");

            // no methods should be called on the IStatsd
            statsd.VerifyNoOtherCalls();
        }

        [Fact(Skip="Test is very unstable and we don't use the metics")]
        public void Send_metrics_when_enabled()
        {
            var statsd = new Mock<IStatsd>();
            var spans = SendSpan(tracerMetricsEnabled: true, statsd);

            Assert.True(spans.Count == 1, "Expected one span");

            // for a single trace, these methods are called once with a value of "1"
            statsd.Verify(
                s => s.Add<Statsd.Counting, int>(TracerMetricNames.Queue.EnqueuedTraces, 1, 1, null),
                Times.Once());

            statsd.Verify(
                s => s.Add<Statsd.Counting, int>(TracerMetricNames.Queue.EnqueuedSpans, 1, 1, null),
                Times.Once());

            statsd.Verify(
                s => s.Add<Statsd.Counting, int>(TracerMetricNames.Queue.DequeuedTraces, 1, 1, null),
                Times.Once());

            statsd.Verify(
                s => s.Add<Statsd.Counting, int>(TracerMetricNames.Queue.DequeuedSpans, 1, 1, null),
                Times.Once());

            statsd.Verify(
                s => s.Add<Statsd.Counting, int>(TracerMetricNames.Api.Requests, 1, 1, null),
                Times.Once());

            statsd.Verify(
                s => s.Add<Statsd.Counting, int>(TracerMetricNames.Api.Responses, 1, 1, "status:200"),
                Times.Once());

            // these methods can be called multiple times with a "0" value (no more traces left)
            /*
            statsd.Verify(
                s => s.Add<Statsd.Gauge, int>(TracerMetricNames.Queue.DequeuedTraces, 0, 1, null),
                Times.AtLeastOnce);

            statsd.Verify(
                s => s.Add<Statsd.Gauge, int>(TracerMetricNames.Queue.DequeuedSpans, 0, 1, null),
                Times.AtLeastOnce());
            */

            // these method can be called multiple times with a "1000" value (the max buffer size, constant)
            statsd.Verify(
                s => s.Add<Statsd.Gauge, int>(TracerMetricNames.Queue.MaxTraces, 1000, 1, null),
                Times.AtLeastOnce());

            // these method can be called multiple times (send buffered commands)
            statsd.Verify(
                s => s.Send(),
                Times.AtLeastOnce());

            // these method can be called multiple times (send heartbeat)
            statsd.Verify(
                s => s.Add<Statsd.Gauge, int>(TracerMetricNames.Health.Heartbeat, It.IsAny<int>(), 1, null),
                Times.AtLeastOnce());

            // no other methods should be called on the IStatsd
            statsd.VerifyNoOtherCalls();
        }

        private static IImmutableList<IMockSpan> SendSpan(bool tracerMetricsEnabled, Mock<IStatsd> statsd)
        {
            IImmutableList<IMockSpan> spans;
            var agentPort = TcpPortProvider.GetOpenPort();

            using (var agent = new MockTracerAgent(agentPort))
            {
                var settings = new TracerSettings
                               {
                                   AgentUri = new Uri($"http://localhost:{agentPort}"),
                                   TracerMetricsEnabled = tracerMetricsEnabled,
                                   ApiType = "dd"
                               };

                var tracer = new Tracer(settings, agentWriter: null, sampler: null, scopeManager: null, statsd.Object);

                using (var scope = tracer.StartActive("root"))
                {
                    scope.Span.ResourceName = "resource";
                    Thread.Sleep(5);
                }

                spans = agent.WaitForSpans(1);
            }

            return spans;
        }
    }
}
