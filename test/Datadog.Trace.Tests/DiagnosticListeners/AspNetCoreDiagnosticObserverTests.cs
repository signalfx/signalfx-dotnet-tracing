// Modified by SignalFx
#if NETCOREAPP3_0 || NETCOREAPP3_1
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Moq;
using SignalFx.Tracing;
using SignalFx.Tracing.Agent;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.DiagnosticListeners;
using SignalFx.Tracing.Sampling;
using Xunit;

namespace Datadog.Trace.Tests.DiagnosticListeners
{
    public class AspNetCoreDiagnosticObserverTests
    {
        [Fact]
        public void BeforeActionEventDataIncompleteRequest()
        {
            const string initialSpanName = "testToForceCodePath";
            var tracer = GetTracer();
            using var scope = tracer.StartActive(initialSpanName);

            IObserver<KeyValuePair<string, object>> observer = new AspNetCoreDiagnosticObserver(tracer, new AspNetCoreDiagnosticOptions());

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.SetupGet(r => r.RouteValues).Returns(new RouteValueDictionary { { "action", "Get" } });
            httpRequest.SetupGet(r => r.Method).Returns("GET");

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(httpRequest.Object);

            const string finalSpanName = "api/values/{id}";
            var actionDescriptor = new ActionDescriptor()
            {
                AttributeRouteInfo = new AttributeRouteInfo { Template = finalSpanName },
                RouteValues = new Dictionary<string, string> { { "action", "Get" } },
            };

            var eventData = new BeforeActionEventData(
                actionDescriptor,
                httpContext.Object,
                new RouteData());

            var span = scope.Span;
            Assert.NotNull(span);
            Assert.Equal(initialSpanName, span.OperationName);
            observer.OnNext(new KeyValuePair<string, object>("Microsoft.AspNetCore.Mvc.BeforeAction", eventData));

            httpContext.VerifyAll();
            Assert.Equal(finalSpanName, span.OperationName);
            Assert.True(span.Tags.TryGetValue("http.method", out var httpMethod));
            Assert.Equal("GET", httpMethod);
        }

        private static Tracer GetTracer()
        {
            var settings = new TracerSettings();
            var writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ISampler>();

            return new Tracer(settings, writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
        }

        private static HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Headers.Add("hello", "hello");
            httpContext.Request.Headers.Add("world", "world");

            httpContext.Request.Host = new HostString("localhost");
            httpContext.Request.Scheme = "http";
            httpContext.Request.Path = "/home/1/action";
            httpContext.Request.Method = "GET";

            return httpContext;
        }
    }
}

#endif
