// <copyright file="HttpClientRequestTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#if NETCOREAPP3_1_OR_GREATER
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.Agent.Transports;
using Datadog.Trace.Propagation;
using Xunit;

namespace Datadog.Trace.Tests
{
    public class HttpClientRequestTests
    {
        [Fact]
        public async Task SetHeaders()
        {
            var handler = new CustomHandler();

            var factory = new HttpClientRequestFactory(new Uri("http://localhost/"), AgentHttpHeaderNames.DefaultHeaders, handler);
            var request = factory.Create(factory.GetEndpoint(string.Empty));

            request.AddHeader("Hello", "World");

            await request.PostAsync(ArraySegment<byte>.Empty, MimeTypes.MsgPack);

            var message = handler.Message;

            Assert.NotNull(message);
            Assert.Equal(".NET", message.Headers.GetValues(AgentHttpHeaderNames.Language).First());
            Assert.Equal(TracerConstants.AssemblyVersion, message.Headers.GetValues(AgentHttpHeaderNames.TracerVersion).First());
            Assert.Equal("false", message.Headers.GetValues(CommonHttpHeaderNames.TracingEnabled).First());
            Assert.Equal("World", message.Headers.GetValues("Hello").First());
        }

        [Fact]
        public async Task SerializeSpans()
        {
            var handler = new CustomHandler();

            var factory = new HttpClientRequestFactory(new Uri("http://localhost/"), AgentHttpHeaderNames.DefaultHeaders, handler);
            var request = factory.Create(factory.GetEndpoint(string.Empty));

            await request.PostAsync(ArraySegment<byte>.Empty, MimeTypes.MsgPack);

            var message = handler.Message;

            Assert.IsAssignableFrom<ByteArrayContent>(message.Content);
        }

        private class CustomHandler : HttpClientHandler
        {
            public HttpRequestMessage Message { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Message = request;
                return Task.FromResult(new HttpResponseMessage());
            }
        }
    }
}

#endif
