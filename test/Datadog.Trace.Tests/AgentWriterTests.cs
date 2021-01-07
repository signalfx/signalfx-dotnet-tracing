// Modified by SignalFx
using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using SignalFx.Tracing;
using SignalFx.Tracing.Agent;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class AgentWriterTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WriteTrace_2Traces_SendToApi(bool synchronousSend)
        {
            var tracer = new Mock<ISignalFxTracer>();
            tracer.Setup(x => x.DefaultServiceName).Returns("Default");

            var api = new Mock<IApi>();
            var agentWriter = new AgentWriter(api.Object, statsd: null, synchronousSend);

            var parentSpanContext = new Mock<ISpanContext>();
            var traceContext = new Mock<ITraceContext>();
            var spanContext = new SpanContext(parentSpanContext.Object, traceContext.Object, serviceName: null);

            // TODO:bertrand it is too complicated to setup such a simple test
            var trace = new[] { new Span(spanContext, start: null) };
            agentWriter.WriteTrace(trace);
            if (!synchronousSend)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            api.Verify(x => x.SendTracesAsync(It.Is<Span[][]>(y => y.Single().Equals(trace))), Times.Once);

            trace = new[] { new Span(spanContext, start: null) };
            agentWriter.WriteTrace(trace);
            if (!synchronousSend)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }

            api.Verify(x => x.SendTracesAsync(It.Is<Span[][]>(y => y.Single().Equals(trace))), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task FlushTwice(bool synchronousSend)
        {
            var api = new Mock<IApi>();
            var w = new AgentWriter(api.Object, statsd: null, synchronousSend);
            await w.FlushAndCloseAsync();
            await w.FlushAndCloseAsync();
        }
    }
}
