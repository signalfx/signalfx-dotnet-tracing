#if !NET452
using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.TestHelpers;
using SignalFx.Tracing;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class RabbitMQTests : TestHelper
    {
        private const string ExpectedServiceName = "Samples.RabbitMQ";

        public RabbitMQTests(ITestOutputHelper output)
            : base("RabbitMQ", output)
        {
        }

        public static IEnumerable<object[]> GetRabbitMQVersions()
        {
            foreach (object[] item in PackageVersions.RabbitMQ)
            {
                yield return item;
            }
        }

        [Theory]
        [MemberData(nameof(GetRabbitMQVersions))]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces(string packageVersion)
        {
            var expectedSpanCount = 24;

            int basicPublishCount = 0;
            int basicGetCount = 0;
            int basicDeliverCount = 0;
            int exchangeDeclareCount = 0;
            int queueDeclareCount = 0;
            int queueBindCount = 0;
            var distributedParentSpans = new Dictionary<ulong, int>();

            int agentPort = TcpPortProvider.GetOpenPort();
            using (var agent = new MockZipkinCollector(agentPort))
            using (var processResult = RunSampleAndWaitForExit(agent.Port, arguments: $"{TestPrefix}", packageVersion: packageVersion))
            {
                Assert.True(processResult.ExitCode >= 0, $"Process exited with code {processResult.ExitCode} and exception: {processResult.StandardError}");

                var spans = agent.WaitForSpans(expectedSpanCount); // Do not filter on operation name because they will vary depending on instrumented method
                Assert.True(spans.Count >= expectedSpanCount, $"Expecting at least {expectedSpanCount} spans, only received {spans.Count}");

                var rabbitmqSpans = spans.Where(span => span.Tags.ContainsKey(Tags.InstrumentationName));
                var manualSpans = spans.Where(span => !span.Tags.ContainsKey(Tags.InstrumentationName));

                foreach (var span in rabbitmqSpans)
                {
                    var command = span.Tags["amqp.command"];

                    if (command.StartsWith("basic.", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(command, "basic.publish", StringComparison.OrdinalIgnoreCase))
                        {
                            basicPublishCount++;
                            Assert.Equal(SpanKinds.Producer, span.Tags[Tags.SpanKind]);
                            Assert.True(span.Tags.TryGetValue(Tags.RabbitMQ.DeliveryMode, out string mode));
                        }
                        else if (string.Equals(command, "basic.get", StringComparison.OrdinalIgnoreCase))
                        {
                            basicGetCount++;
                            Assert.Equal(SpanKinds.Consumer, span.Tags[Tags.SpanKind]);
                            Assert.True(span.Tags.TryGetValue(Tags.Messaging.MessagePayloadSizeBytes, out string messageSize));
                            Assert.NotNull(span.ParentId);
                        }
                        else if (string.Equals(command, "basic.deliver", StringComparison.OrdinalIgnoreCase))
                        {
                            basicDeliverCount++;
                            Assert.Equal(SpanKinds.Consumer, span.Tags[Tags.SpanKind]);
                            Assert.True(span.Tags.TryGetValue(Tags.Messaging.MessagePayloadSizeBytes, out string messageSize));
                            Assert.NotNull(span.ParentId);
                        }
                        else
                        {
                            throw new Xunit.Sdk.XunitException($"amqp.command {command} not recognized.");
                        }
                    }
                    else
                    {
                        Assert.Equal(SpanKinds.Client, span.Tags[Tags.SpanKind]);

                        if (string.Equals(command, "exchange.declare", StringComparison.OrdinalIgnoreCase))
                        {
                            exchangeDeclareCount++;
                        }
                        else if (string.Equals(command, "queue.declare", StringComparison.OrdinalIgnoreCase))
                        {
                            queueDeclareCount++;
                        }
                        else if (string.Equals(command, "queue.bind", StringComparison.OrdinalIgnoreCase))
                        {
                            queueBindCount++;
                        }
                        else
                        {
                            throw new Xunit.Sdk.XunitException($"amqp.command {command} not recognized.");
                        }
                    }
                }

                foreach (var span in manualSpans)
                {
                    Assert.Equal("Samples.RabbitMQ", span.Service);
                }
            }

            Assert.Equal(5, basicPublishCount);
            Assert.Equal(2, basicGetCount);
            Assert.Equal(3, basicDeliverCount);

            Assert.Equal(1, exchangeDeclareCount);
            Assert.Equal(1, queueBindCount);
            Assert.Equal(4, queueDeclareCount);
        }
    }
}
#endif
