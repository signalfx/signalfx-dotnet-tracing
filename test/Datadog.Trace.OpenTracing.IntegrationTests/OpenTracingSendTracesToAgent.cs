using System;
using System.Linq;
using System.Net;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.TestHelpers.HttpMessageHandlers;
using SignalFx.Tracing;
using SignalFx.Tracing.Agent;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.OpenTracing;
using Xunit;

namespace Datadog.Trace.OpenTracing.IntegrationTests
{
    public class OpenTracingSendTracesToAgent
    {
        private readonly OpenTracingTracer _tracer;
        private readonly RecordHttpHandler _httpRecorder;

        public OpenTracingSendTracesToAgent()
        {
            var settings = new TracerSettings();

            _httpRecorder = new RecordHttpHandler();
            var api = new ZipkinApi(settings, _httpRecorder);
            var agentWriter = new AgentWriter(api, statsd: null, synchronousSend: false);

            var tracer = new Tracer(settings, agentWriter, sampler: null, scopeManager: null, statsd: null);
            _tracer = new OpenTracingTracer(tracer);
        }

        [Fact]
        public async void MinimalSpan()
        {
            using var mockZipkinCollector = new MockZipkinCollector();

            var span = (OpenTracingSpan)_tracer.BuildSpan("Operation")
                                               .Start();
            span.Finish();

            // Check that the HTTP calls went as expected
            await _httpRecorder.WaitForCompletion(1);
            Assert.Single(_httpRecorder.Requests);
            Assert.Single(_httpRecorder.Responses);
            Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

            var trace = _httpRecorder.ZipkinTraces.Single();
            ZipkinHelpers.AssertSpanEqual(span.Span, trace.Single());
        }

        [Fact]
        public async void CustomServiceName()
        {
            using var mockZipkinCollector = new MockZipkinCollector();

            const string ServiceName = "MyService";

            var span = (OpenTracingSpan)_tracer.BuildSpan("Operation-From-OpenTracingSendTracesToAgent")
                                               .WithTag(Tags.ResourceName, "This is a resource")
                                               .WithTag(CustomTags.ServiceName, ServiceName)
                                               .Start();
            span.Finish();

            // Check that the HTTP calls went as expected
            await _httpRecorder.WaitForCompletion(1);
            Assert.Single(_httpRecorder.Requests);
            Assert.Single(_httpRecorder.Responses);
            Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

            var trace = _httpRecorder.ZipkinTraces.Single();
            ZipkinHelpers.AssertSpanEqual(span.Span, trace.Single());
        }

        [Fact]
        public async void Utf8Everywhere()
        {
            using var mockZipkinCollector = new MockZipkinCollector();

            var span = (OpenTracingSpan)_tracer.BuildSpan("Aᛗᚪᚾᚾᚪ")
                                               .WithTag(Tags.ResourceName, "η γλώσσα μου έδωσαν ελληνική")
                                               .WithTag(CustomTags.ServiceName, "На берегу пустынных волн")
                                               .WithTag("யாமறிந்த", "ნუთუ კვლა")
                                               .Start();
            span.Finish();

            // Check that the HTTP calls went as expected
            await _httpRecorder.WaitForCompletion(1);
            Assert.Single(_httpRecorder.Requests);
            Assert.Single(_httpRecorder.Responses);
            Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

            var trace = _httpRecorder.ZipkinTraces.Single();
            ZipkinHelpers.AssertSpanEqual(span.Span, trace.Single());
        }

        [Fact]
        public void WithDefaultFactory()
        {
            // This test does not check anything it validates that this codepath runs without exceptions
            _tracer.BuildSpan("Operation")
                   .Start()
                   .Finish();
        }
    }
}
