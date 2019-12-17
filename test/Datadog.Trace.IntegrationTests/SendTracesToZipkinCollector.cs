// Modified by SignalFx
using System;
using System.Linq;
using System.Net;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.TestHelpers.HttpMessageHandlers;
using Xunit;

namespace Datadog.Trace.IntegrationTests
{
    public class SendTracesToZipkinCollector
    {
        private readonly Tracer _tracer;
        private readonly RecordHttpHandler _httpRecorder;

        private readonly int collectorPort = 9080;

        public SendTracesToZipkinCollector()
        {
            var settings = new TracerSettings();
            _httpRecorder = new RecordHttpHandler();
            var api = new ZipkinApi(settings.EndpointUrl, _httpRecorder, statsd: null);
            var agentWriter = new AgentWriter(api, statsd: null);
            _tracer = new Tracer(settings, agentWriter, sampler: null, scopeManager: null, statsd: null);
        }

        [Fact]
        public async void MinimalSpan()
        {
            using (var agent = new MockZipkinCollector(collectorPort))
            {
                var scope = _tracer.StartActive("Operation");
                scope.Dispose();

                await _httpRecorder.WaitForCompletion(1);
                Assert.Single(_httpRecorder.Requests);
                Assert.Single(_httpRecorder.Responses);
                Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

                var trace = _httpRecorder.ZipkinTraces.Single();
                ZipkinHelpers.AssertSpanEqual(scope.Span, trace);
            }
        }

        [Fact]
        public async void CustomServiceName()
        {
            using (var agent = new MockZipkinCollector(collectorPort))
            {
                const string ServiceName = "MyService";

                var scope = _tracer.StartActive("Operation", serviceName: ServiceName);
                scope.Span.ResourceName = "This is a resource";
                scope.Dispose();

                await _httpRecorder.WaitForCompletion(1);
                Assert.Single(_httpRecorder.Requests);
                Assert.Single(_httpRecorder.Responses);
                Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

                var trace = _httpRecorder.ZipkinTraces.Single();
                ZipkinHelpers.AssertSpanEqual(scope.Span, trace);
            }
        }

        [Fact]
        public async void Utf8Everywhere()
        {
            using (var agent = new MockZipkinCollector(collectorPort))
            {
                var scope = _tracer.StartActive("Aᛗᚪᚾᚾᚪ", serviceName: "На берегу пустынных волн");
                scope.Span.ResourceName = "η γλώσσα μου έδωσαν ελληνική";
                scope.Span.SetTag("யாமறிந்த", "ნუთუ კვლა");
                scope.Dispose();

                await _httpRecorder.WaitForCompletion(1);
                Assert.Single(_httpRecorder.Requests);
                Assert.Single(_httpRecorder.Responses);
                Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

                var trace = _httpRecorder.ZipkinTraces.Single();
                ZipkinHelpers.AssertSpanEqual(scope.Span, trace);
            }
        }

        [Fact]
        public async void SubmitsOutOfOrderSpans()
        {
            using (var agent = new MockZipkinCollector(collectorPort))
            {
                var scope1 = _tracer.StartActive("op1");
                var scope2 = _tracer.StartActive("op2");
                scope1.Close();
                scope2.Close();

                await _httpRecorder.WaitForCompletion(1);
                Assert.Single(_httpRecorder.Requests);
                Assert.Single(_httpRecorder.Responses);
                Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

                var trace = _httpRecorder.ZipkinTraces;
                ZipkinHelpers.AssertSpanEqual(scope1.Span, trace[0][0]);
                ZipkinHelpers.AssertSpanEqual(scope2.Span, trace[0][1]);
            }
        }

        [Fact]
        public async void WithError()
        {
            using (var agent = new MockZipkinCollector(collectorPort))
            {
                var scope = _tracer.StartActive("Operation");
                scope.Span.Error = true;
                scope.Dispose();

                await _httpRecorder.WaitForCompletion(1);
                Assert.Single(_httpRecorder.Requests);
                Assert.Single(_httpRecorder.Responses);
                Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

                var trace = _httpRecorder.ZipkinTraces.Single();
                ZipkinHelpers.AssertSpanEqual(scope.Span, trace);
            }
        }
    }
}
