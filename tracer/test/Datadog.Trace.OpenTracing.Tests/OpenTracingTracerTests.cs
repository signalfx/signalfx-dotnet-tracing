// <copyright file="OpenTracingTracerTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Propagation;
using Datadog.Trace.Sampling;
using Datadog.Trace.TestHelpers;
using Moq;
using OpenTracing.Propagation;
using OpenTracing.Util;
using Xunit;

namespace Datadog.Trace.OpenTracing.Tests
{
    public class OpenTracingTracerTests
    {
        private readonly OpenTracingTracer _tracer;
        private readonly Tracer _signalFxTracer;

        public OpenTracingTracerTests()
        {
            var settings = new TracerSettings
            {
                // force to be a Datadog for unit tests purposes, default is OpenTelemetry
                Convention = ConventionType.Datadog
            };
            var writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            _signalFxTracer = new Tracer(settings, writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);

            _tracer = new OpenTracingTracer(_signalFxTracer);
        }

        [Fact]
        public void BuildSpan_NoParameter_DefaultParameters()
        {
            var builder = _tracer.BuildSpan("Op1");
            var span = (OpenTracingSpan)builder.Start();

            Assert.Contains(span.DDSpan.ServiceName, TestRunners.ValidNames);
            Assert.Equal("Op1", span.DDSpan.OperationName);
        }

        [Fact]
        public void BuildSpan_OneChild_ChildParentProperlySet()
        {
            var root = _tracer
                         .BuildSpan("Root")
                         .StartActive(finishSpanOnDispose: true);
            var child = _tracer
                          .BuildSpan("Child")
                          .StartActive(finishSpanOnDispose: true);

            Span rootDatadogSpan = (Span)((OpenTracingSpan)root.Span).Span;
            Span childDatadogSpan = (Span)((OpenTracingSpan)child.Span).Span;

            Assert.Equal(rootDatadogSpan.Context.TraceContext, childDatadogSpan.Context.TraceContext);
            Assert.Equal(rootDatadogSpan.Context.SpanId, childDatadogSpan.Context.ParentId);
        }

        [Fact]
        public void BuildSpan_2ChildrenOfRoot_ChildrenParentProperlySet()
        {
            var root = _tracer
                         .BuildSpan("Root")
                         .StartActive(finishSpanOnDispose: true);

            var child1 = _tracer
                           .BuildSpan("Child1")
                           .StartActive(finishSpanOnDispose: true);

            child1.Dispose();

            var child2 = _tracer
                           .BuildSpan("Child2")
                           .StartActive(finishSpanOnDispose: true);

            Span rootDatadogSpan = (Span)((OpenTracingSpan)root.Span).Span;
            Span child1DatadogSpan = (Span)((OpenTracingSpan)child1.Span).Span;
            Span child2DatadogSpan = (Span)((OpenTracingSpan)child2.Span).Span;

            Assert.Same(rootDatadogSpan.Context.TraceContext, child1DatadogSpan.Context.TraceContext);
            Assert.Equal(rootDatadogSpan.Context.SpanId, child1DatadogSpan.Context.ParentId);
            Assert.Same(rootDatadogSpan.Context.TraceContext, child2DatadogSpan.Context.TraceContext);
            Assert.Equal(rootDatadogSpan.Context.SpanId, child2DatadogSpan.Context.ParentId);
        }

        [Fact]
        public void BuildSpan_2LevelChildren_ChildrenParentProperlySet()
        {
            var root = _tracer
                         .BuildSpan("Root")
                         .StartActive(finishSpanOnDispose: true);
            var child1 = _tracer
                           .BuildSpan("Child1")
                           .StartActive(finishSpanOnDispose: true);
            var child2 = _tracer
                           .BuildSpan("Child2")
                           .StartActive(finishSpanOnDispose: true);

            Span rootDatadogSpan = (Span)((OpenTracingSpan)root.Span).Span;
            Span child1DatadogSpan = (Span)((OpenTracingSpan)child1.Span).Span;
            Span child2DatadogSpan = (Span)((OpenTracingSpan)child2.Span).Span;

            Assert.Same(rootDatadogSpan.Context.TraceContext, child1DatadogSpan.Context.TraceContext);
            Assert.Equal(rootDatadogSpan.Context.SpanId, child1DatadogSpan.Context.ParentId);
            Assert.Same(rootDatadogSpan.Context.TraceContext, child2DatadogSpan.Context.TraceContext);
            Assert.Equal(child1DatadogSpan.Context.SpanId, child2DatadogSpan.Context.ParentId);
        }

        [Fact]
        public async Task BuildSpan_AsyncChildrenCreation_ChildrenParentProperlySet()
        {
            var tcs = new TaskCompletionSource<bool>();

            var root = _tracer
                         .BuildSpan("Root")
                         .StartActive(finishSpanOnDispose: true);

            Func<OpenTracingTracer, Task<OpenTracingSpan>> createSpanAsync = async (t) =>
            {
                await tcs.Task;
                return (OpenTracingSpan)_tracer.BuildSpan("AsyncChild").Start();
            };
            var tasks = Enumerable.Range(0, 10).Select(x => createSpanAsync(_tracer)).ToArray();

            var syncChild = (OpenTracingSpan)_tracer.BuildSpan("SyncChild").Start();
            var syncChildSpanContext = ((Span)syncChild.Span).Context;
            tcs.SetResult(true);

            Span rootDatadogSpan = (Span)((OpenTracingSpan)root.Span).Span;

            Assert.Equal(rootDatadogSpan.Context.TraceContext, syncChildSpanContext.TraceContext);
            Assert.Equal(rootDatadogSpan.Context.SpanId, syncChildSpanContext.ParentId);

            foreach (var task in tasks)
            {
                var span = await task;
                var spanContext = ((Span)syncChild.Span).Context;
                Assert.Equal(rootDatadogSpan.Context.TraceContext, spanContext.TraceContext);
                Assert.Equal(rootDatadogSpan.Context.SpanId, spanContext.ParentId);
            }
        }

        [Fact]
        public void Inject_HttpHeadersFormat_CorrectHeaders()
        {
            var span = (OpenTracingSpan)_tracer.BuildSpan("Span").Start();
            var headers = new MockTextMap();

            _tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, headers);

            Assert.Equal(span.DDSpan.Context.TraceId.ToString(), headers.Get(B3HttpHeaderNames.B3TraceId));
            Assert.Equal(span.DDSpan.Context.SpanId, ulong.Parse(headers.Get(B3HttpHeaderNames.B3SpanId), NumberStyles.HexNumber));
        }

        [Fact]
        public void Inject_TextMapFormat_CorrectHeaders()
        {
            var span = (OpenTracingSpan)_tracer.BuildSpan("Span").Start();
            var headers = new MockTextMap();

            _tracer.Inject(span.Context, BuiltinFormats.TextMap, headers);

            Assert.Equal(span.DDSpan.Context.TraceId.ToString(), headers.Get(B3HttpHeaderNames.B3TraceId));
            Assert.Equal(span.DDSpan.Context.SpanId, ulong.Parse(headers.Get(B3HttpHeaderNames.B3SpanId), NumberStyles.HexNumber));
        }

        [Fact]
        public void Inject_UnknownFormat_Throws()
        {
            var span = (OpenTracingSpan)_tracer.BuildSpan("Span").Start();
            var headers = new MockTextMap();
            var mockFormat = new Mock<IFormat<ITextMap>>();

            Assert.Throws<NotSupportedException>(() => _tracer.Inject(span.Context, mockFormat.Object, headers));
        }

        [Fact]
        public void Extract_HttpHeadersFormat_HeadersProperlySet_SpanContext()
        {
            const ulong spanId = 1000000000000000;
            var traceId = TraceId.CreateRandom();
            var headers = new MockTextMap();
            headers.Set(B3HttpHeaderNames.B3SpanId, spanId.ToString());
            headers.Set(B3HttpHeaderNames.B3TraceId, traceId.ToString());

            var otSpanContext = (OpenTracingSpanContext)_tracer.Extract(BuiltinFormats.HttpHeaders, headers);

            Assert.Equal(spanId.ToString(), otSpanContext.Context.SpanId.ToString("x16"));
            Assert.Equal(traceId, otSpanContext.Context.TraceId);
        }

        [Fact]
        public void Extract_TextMapFormat_HeadersProperlySet_SpanContext()
        {
            const ulong spanId = 1000000000000000;
            var traceId = TraceId.CreateRandom();
            var headers = new MockTextMap();
            headers.Set(B3HttpHeaderNames.B3SpanId, spanId.ToString());
            headers.Set(B3HttpHeaderNames.B3TraceId, traceId.ToString());

            var otSpanContext = (OpenTracingSpanContext)_tracer.Extract(BuiltinFormats.TextMap, headers);

            Assert.Equal(spanId.ToString(), otSpanContext.Context.SpanId.ToString("x16"));
            Assert.Equal(traceId, otSpanContext.Context.TraceId);
        }

        [Fact]
        public void Extract_UnknownFormat_Throws()
        {
            const ulong parentId = 10;
            var traceId = TraceId.CreateFromInt(42);
            var headers = new MockTextMap();
            headers.Set(DDHttpHeaderNames.ParentId, parentId.ToString());
            headers.Set(DDHttpHeaderNames.TraceId, traceId.ToString());
            var mockFormat = new Mock<IFormat<ITextMap>>();

            Assert.Throws<NotSupportedException>(() => _tracer.Extract(mockFormat.Object, headers));
        }

        [Fact]
        public void StartActive_NoServiceName_DefaultServiceName()
        {
            var scope = _tracer.BuildSpan("Operation")
                               .StartActive();

            var otSpan = (OpenTracingSpan)scope.Span;
            var ddSpan = otSpan.Span;

            Assert.Contains(ddSpan.ServiceName, TestRunners.ValidNames);
        }

        [Fact]
        public void SetDefaultServiceName()
        {
            var tracer = OpenTracingTracerFactory.CreateTracer(defaultServiceName: "DefaultServiceName");

            var scope = tracer.BuildSpan("Operation")
                              .StartActive();

            var otSpan = (OpenTracingSpan)scope.Span;
            var ddSpan = otSpan.Span;

            Assert.Equal("DefaultServiceName", ddSpan.ServiceName);
        }

        [Fact]
        public void SetServiceName_WithTag()
        {
            var scope = _tracer.BuildSpan("Operation")
                               .WithTag(DatadogTags.ServiceName, "MyAwesomeService")
                               .StartActive();

            var otSpan = (OpenTracingSpan)scope.Span;
            var ddSpan = otSpan.Span;

            Assert.Equal("MyAwesomeService", ddSpan.ServiceName);
        }

        [Fact]
        public void SetServiceName_SetTag()
        {
            var scope = _tracer.BuildSpan("Operation")
                               .StartActive();

            scope.Span.SetTag(DatadogTags.ServiceName, "MyAwesomeService");
            var otSpan = (OpenTracingSpan)scope.Span;
            var ddSpan = otSpan.Span;

            Assert.Equal("MyAwesomeService", ddSpan.ServiceName);
        }

        [Fact]
        public void OverrideDefaultServiceName_WithTag()
        {
            var tracer = OpenTracingTracerFactory.CreateTracer(defaultServiceName: "DefaultServiceName");

            var scope = tracer.BuildSpan("Operation")
                              .WithTag(DatadogTags.ServiceName, "MyAwesomeService")
                              .StartActive();

            var otSpan = (OpenTracingSpan)scope.Span;
            var ddSpan = otSpan.Span;

            Assert.Equal("MyAwesomeService", ddSpan.ServiceName);
        }

        [Fact]
        public void OverrideDefaultServiceName_SetTag()
        {
            var tracer = OpenTracingTracerFactory.CreateTracer(defaultServiceName: "DefaultServiceName");

            var scope = tracer.BuildSpan("Operation")
                              .StartActive();

            scope.Span.SetTag(DatadogTags.ServiceName, "MyAwesomeService");
            var otSpan = (OpenTracingSpan)scope.Span;
            var ddSpan = otSpan.Span;

            Assert.Equal("MyAwesomeService", ddSpan.ServiceName);
        }

        [Fact]
        public void DoesNotInheritParentServiceName_WithTag()
        {
            var parentScope = _tracer.BuildSpan("ParentOperation")
                                     .WithTag(DatadogTags.ServiceName, "MyAwesomeService")
                                     .StartActive();

            var childScope = _tracer.BuildSpan("ChildOperation")
                                    .AsChildOf(parentScope.Span)
                                    .StartActive();

            Assert.Equal("MyAwesomeService", ((OpenTracingSpan)parentScope.Span).Span.ServiceName);
            Assert.NotEqual("MyAwesomeService", ((OpenTracingSpan)childScope.Span).Span.ServiceName);
            Assert.Equal(_tracer.DefaultServiceName, ((OpenTracingSpan)childScope.Span).Span.ServiceName);
        }

        [Fact]
        public void DoesNotInheritParentServiceName_SetTag()
        {
            var parentScope = _tracer.BuildSpan("ParentOperation")
                                     .StartActive();

            parentScope.Span.SetTag(DatadogTags.ServiceName, "MyAwesomeService");

            var childScope = _tracer.BuildSpan("ChildOperation")
                                    .AsChildOf(parentScope.Span)
                                    .StartActive();

            Assert.Equal("MyAwesomeService", ((OpenTracingSpan)parentScope.Span).Span.ServiceName);
            Assert.NotEqual("MyAwesomeService", ((OpenTracingSpan)childScope.Span).Span.ServiceName);
            Assert.Equal(_tracer.DefaultServiceName, ((OpenTracingSpan)childScope.Span).Span.ServiceName);
        }

        [Fact]
        public void Parent_OverrideDefaultServiceName_WithTag()
        {
            const string defaultServiceName = "DefaultServiceName";
            var tracer = OpenTracingTracerFactory.CreateTracer(defaultServiceName: defaultServiceName);

            var parentScope = tracer.BuildSpan("ParentOperation")
                                    .WithTag(DatadogTags.ServiceName, "MyAwesomeService")
                                    .StartActive();

            var childScope = tracer.BuildSpan("ChildOperation")
                                   .AsChildOf(parentScope.Span)
                                   .StartActive();

            Assert.Equal("MyAwesomeService", ((OpenTracingSpan)parentScope.Span).Span.ServiceName);
            Assert.NotEqual("MyAwesomeService", ((OpenTracingSpan)childScope.Span).Span.ServiceName);
            Assert.Equal(defaultServiceName, ((OpenTracingSpan)childScope.Span).Span.ServiceName);
        }

        [Fact]
        public void Parent_OverrideDefaultServiceName_SetTag()
        {
            const string defaultServiceName = "DefaultServiceName";
            var tracer = OpenTracingTracerFactory.CreateTracer(defaultServiceName: defaultServiceName);

            var parentScope = tracer.BuildSpan("ParentOperation")
                                    .StartActive();

            parentScope.Span.SetTag(DatadogTags.ServiceName, "MyAwesomeService");

            var childScope = tracer.BuildSpan("ChildOperation")
                                   .AsChildOf(parentScope.Span)
                                   .StartActive();

            Assert.Equal("MyAwesomeService", ((OpenTracingSpan)parentScope.Span).Span.ServiceName);
            Assert.NotEqual("MyAwesomeService", ((OpenTracingSpan)childScope.Span).Span.ServiceName);
            Assert.Equal(defaultServiceName, ((OpenTracingSpan)childScope.Span).Span.ServiceName);
        }

        [Fact]
        public void RegisteredAsGlobalTracer_ByDefault()
        {
            var globalTracerRep = GlobalTracer.Instance.ToString();
            Assert.Contains("Datadog.Trace.OpenTracing.OpenTracingTracer", globalTracerRep);
        }

        [Fact]
        public void RegisteredAsGlobalTracer_OnlyOnce()
        {
            Assert.False(OpenTracingTracerFactory.RegisterGlobalTracerIfAbsent(_signalFxTracer));
        }
    }
}
