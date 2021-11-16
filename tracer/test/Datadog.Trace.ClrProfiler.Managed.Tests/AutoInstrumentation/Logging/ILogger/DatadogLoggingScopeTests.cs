// <copyright file="DatadogLoggingScopeTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using Datadog.Trace.Agent;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.Logging.ILogger;
using Datadog.Trace.Configuration;
using Datadog.Trace.DogStatsd;
using FluentAssertions;
using Moq;
using Xunit;

namespace Datadog.Trace.ClrProfiler.Managed.Tests.AutoInstrumentation.Logging.ILogger
{
    public class DatadogLoggingScopeTests
    {
        [Fact]
        public void OutputsJsonFormattedStringWhenNoActiveTrace()
        {
            var settings = new TracerSettings
            {
                ServiceName = "TestService",
                ServiceVersion = "1.2.3",
                Environment = "test"
            };

            var tracer = new Tracer(settings, null, new Mock<IAgentWriter>().Object, null, null, new NoOpStatsd());

            var scope = new DatadogLoggingScope(tracer);

            var actual = scope.ToString();

            actual.Should().Be(@"service.name:""TestService"", deployment.environment:""test"", service.version:""1.2.3""");
        }

        [Fact]
        public void OutputsJsonFormattedStringWhenActiveTrace()
        {
            var settings = new TracerSettings
            {
                ServiceName = "TestService",
                ServiceVersion = "1.2.3",
                Environment = "test"
            };

            var tracer = new Tracer(settings, null, new Mock<IAgentWriter>().Object, null, null, new NoOpStatsd());
            using var spanScope = tracer.StartActive("test");
            var scope = new DatadogLoggingScope(tracer);

            var actual = scope.ToString();

            var expected = @$"service.name:""TestService"", deployment.environment:""test"", service.version:""1.2.3"", trace_id:""{spanScope.Span.TraceId}"", span_id:""{spanScope.Span.SpanId}""";
            actual.Should().Be(expected);
        }
    }
}
