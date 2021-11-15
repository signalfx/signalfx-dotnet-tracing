// <copyright file="SpanContextTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class SpanContextTests
    {
        [Fact]
        public void OverrideTraceIdWithoutParent()
        {
            var expectedTraceId = TraceId.CreateFromInt(41);
            const ulong expectedSpanId = 42;

            var spanContext = new SpanContext(parent: null, traceContext: null, serviceName: "service", traceId: expectedTraceId, spanId: expectedSpanId);

            spanContext.SpanId.Should().Be(expectedSpanId);
            spanContext.TraceId.Should().Be(expectedTraceId);
        }

        [Fact]
        public void OverrideTraceIdWithParent()
        {
            var parentTraceId = TraceId.CreateFromInt(41);
            const ulong parentSpanId = 42;

            var childTraceId = TraceId.CreateFromInt(43);
            const ulong childSpanId = 44;

            var parent = new SpanContext(parentTraceId, parentSpanId);

            var spanContext = new SpanContext(parent: parent, traceContext: null, serviceName: "service", traceId: childTraceId, spanId: childSpanId);

            spanContext.SpanId.Should().Be(childSpanId);
            spanContext.TraceId.Should().Be(parentTraceId, "trace id shouldn't be overriden if a parent trace exists. Doing so would break the HttpWebRequest.GetRequestStream/GetResponse integration.");
        }
    }
}
