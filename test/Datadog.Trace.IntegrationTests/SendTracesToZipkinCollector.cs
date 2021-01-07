// Modified by SignalFx
using System;
using System.Linq;
using System.Net;
using Datadog.Trace.TestHelpers;
using Datadog.Trace.TestHelpers.HttpMessageHandlers;
using SignalFx.Tracing;
using SignalFx.Tracing.Agent;
using SignalFx.Tracing.Configuration;
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
            var api = new ZipkinApi(settings, _httpRecorder);
            var agentWriter = new AgentWriter(api, statsd: null, synchronousSend: false);
            _tracer = new Tracer(settings, agentWriter, sampler: null, scopeManager: null, statsd: null);
        }

        [Fact]
        public async void MinimalSpan()
        {
            using (var agent = new MockZipkinCollector(collectorPort))
            {
                var scope = _tracer.StartActive("Operation");
                scope.Span.SetTag(Tags.SpanKind, SpanKinds.Client);
                scope.Dispose();

                await _httpRecorder.WaitForCompletion(1);
                Assert.Single(_httpRecorder.Requests);
                Assert.Single(_httpRecorder.Responses);
                Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

                var trace = _httpRecorder.ZipkinTraces.Single();
                ZipkinHelpers.AssertSpanEqual(scope.Span, trace);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void CustomServiceName(bool serviceNamePerSpanEnabled)
        {
            var savedServiceNamePerSpanSetting = _tracer.Settings.ServiceNamePerSpanEnabled;
            try
            {
                _tracer.Settings.ServiceNamePerSpanEnabled = serviceNamePerSpanEnabled;

                using (var agent = new MockZipkinCollector(collectorPort))
                {
                    const string serviceName = "MyService";

                    var scope = _tracer.StartActive("Operation-From-SendTracesToZipkinCollector", serviceName: serviceName);
                    scope.Span.ResourceName = "This is a resource";
                    scope.Dispose();

                    agent.WaitForSpans(1);
                    await _httpRecorder.WaitForCompletion(1);
                    Assert.Single(_httpRecorder.Requests);
                    Assert.Single(_httpRecorder.Responses);
                    Assert.All(_httpRecorder.Responses, (x) => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

                    var trace = _httpRecorder.ZipkinTraces.Single();

                    var expectedServiceName = serviceNamePerSpanEnabled
                                                  ? serviceName
                                                  : _tracer.DefaultServiceName;

                    ZipkinHelpers.AssertSpanEqual(scope.Span, trace, expectedServiceName);
                }
            }
            finally
            {
                _tracer.Settings.ServiceNamePerSpanEnabled = savedServiceNamePerSpanSetting;
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
                scope.Span.Log("யாமறிந்த", "ნუთუ კვლა");
                scope.Span.Log("யாமறிந்த", "ნუთუ კვლა");
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

                // Check root span for mandatory tags.
                Assert.Contains(scope1.Span.Tags, kvp => kvp.Key == Tags.Language && kvp.Value == TracerConstants.Language);
                Assert.Contains(scope1.Span.Tags, kvp => kvp.Key == Tags.Version && kvp.Value == TracerConstants.AssemblyVersion);

                // Child spans should not have root spans tags.
                Assert.Null(scope2.Span.Tags);
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
