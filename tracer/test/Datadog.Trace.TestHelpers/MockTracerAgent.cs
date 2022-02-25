// <copyright file="MockTracerAgent.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.HttpOverStreams;
using Datadog.Trace.Util;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;
using MessagePack; // use nuget MessagePack to deserialize

namespace Datadog.Trace.TestHelpers
{
    public class MockTracerAgent : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly HttpListener _metricsListener;
        private readonly Task _tracesListenerTask;
        private readonly Task _metricsListenerTask;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public MockTracerAgent(WindowsPipesConfig config)
        {
            throw new NotImplementedException("Windows named pipes are not yet implemented in the MockTracerAgent");
        }

        public MockTracerAgent(int port = 8126, int retries = 5, bool useSfxMetrics = false, bool doNotBindPorts = false, int? requestedStatsDPort = null)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            if (doNotBindPorts)
            {
                // This is for any tests that want to use a specific port but never actually bind
                Port = port;
                return;
            }

            var listeners = new List<string>();

            if (useSfxMetrics)
            {
                var metricsPort = 8226;
                var metricsRetries = 5;

                if (requestedStatsDPort != null)
                {
                    // This port is explicit, allow failure if not available
                    var metricsListener = new HttpListener();
                    metricsListener.Prefixes.Add($"http://127.0.0.1:{requestedStatsDPort}/");
                    metricsListener.Prefixes.Add($"http://localhost:{requestedStatsDPort}/");

                    MetricsPort = requestedStatsDPort.Value;
                    _metricsListener = metricsListener;

                    _metricsListenerTask = Task.Run(HandleMetricsHttpRequests);
                }
                else
                {
                    while (true)
                    {
                        var metricsListener = new HttpListener();
                        metricsListener.Prefixes.Add($"http://127.0.0.1:{metricsPort}/");
                        metricsListener.Prefixes.Add($"http://localhost:{metricsPort}/");

                        try
                        {
                            metricsListener.Start();

                            // successfully listening
                            MetricsPort = metricsPort;
                            _metricsListener = metricsListener;

                            _metricsListenerTask = Task.Run(HandleMetricsHttpRequests);

                            break;
                        }
                        catch (HttpListenerException) when (metricsRetries > 0)
                        {
                            // only catch the exception if there are retries left
                            metricsPort = TcpPortProvider.GetOpenPort();
                            metricsRetries--;
                        }

                        // always close listener if exception is thrown,
                        // whether it was caught or not
                        metricsListener.Close();
                    }
                }

                listeners.Add($"Stats at port {MetricsPort}");
            }

            // try up to 5 consecutive ports before giving up
            while (true)
            {
                // seems like we can't reuse a listener if it fails to start,
                // so create a new listener each time we retry
                var listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                listener.Prefixes.Add($"http://localhost:{port}/");

                var containerHostname = EnvironmentHelpers.GetEnvironmentVariable("CONTAINER_HOSTNAME");
                if (containerHostname != null)
                {
                    listener.Prefixes.Add($"{containerHostname}:{port}/");
                }

                try
                {
                    listener.Start();

                    // successfully listening
                    Port = port;
                    _listener = listener;

                    listeners.Add($"Traces at port {Port}");
                    _tracesListenerTask = Task.Run(HandleHttpRequests);

                    return;
                }
                catch (HttpListenerException) when (retries > 0)
                {
                    // only catch the exception if there are retries left
                    port = TcpPortProvider.GetOpenPort();
                    retries--;
                }
                finally
                {
                    ListenerInfo = string.Join(", ", listeners);
                }

                // always close listener if exception is thrown,
                // whether it was caught or not
                listener.Close();
            }
        }

        public event EventHandler<EventArgs<HttpListenerContext>> RequestReceived;

        public event EventHandler<EventArgs<IList<IList<MockSpan>>>> RequestDeserialized;

        public event EventHandler<EventArgs<string>> MetricsReceived;

        public string ListenerInfo { get; }

        /// <summary>
        /// Gets the TCP port that this Agent is listening on.
        /// Can be different from <see cref="MockTracerAgent(int, int, bool)"/>'s <c>initialPort</c>
        /// parameter if listening on that port fails.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the port that this agent is listening for SignalFx metrics on.
        /// </summary>
        public int MetricsPort { get; }

        public string TracesUdsPath { get; }

        public string StatsUdsPath { get; }

        public string TracesWindowsPipeName { get; }

        public string StatsWindowsPipeName { get; }

        public string Version { get; set; }

        /// <summary>
        /// Gets the filters used to filter out spans we don't want to look at for a test.
        /// </summary>
        public List<Func<MockSpan, bool>> SpanFilters { get; } = new();

        public ConcurrentBag<Exception> Exceptions { get; private set; } = new ConcurrentBag<Exception>();

        public IImmutableList<MockSpan> Spans { get; private set; } = ImmutableList<MockSpan>.Empty;

        public IImmutableList<DataPoint> Metrics { get; private set; } = ImmutableList<DataPoint>.Empty;

        public IImmutableList<NameValueCollection> RequestHeaders { get; private set; } = ImmutableList<NameValueCollection>.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to skip deserialization of traces.
        /// </summary>
        public bool ShouldDeserializeTraces { get; set; } = true;

        /// <summary>
        /// Wait for the given number of spans to appear.
        /// </summary>
        /// <param name="count">The expected number of spans.</param>
        /// <param name="timeoutInMilliseconds">The timeout</param>
        /// <param name="operationName">The integration we're testing</param>
        /// <param name="minDateTime">Minimum time to check for spans from</param>
        /// <param name="returnAllOperations">When true, returns every span regardless of operation name</param>
        /// <returns>The list of spans.</returns>
        public IImmutableList<MockSpan> WaitForSpans(
            int count,
            int timeoutInMilliseconds = 20000,
            string operationName = null,
            DateTimeOffset? minDateTime = null,
            bool returnAllOperations = false)
        {
            var deadline = DateTime.Now.AddMilliseconds(timeoutInMilliseconds);
            var minimumOffset = (minDateTime ?? DateTimeOffset.MinValue).ToUnixTimeNanoseconds();

            IImmutableList<MockSpan> relevantSpans = ImmutableList<MockSpan>.Empty;

            while (DateTime.Now < deadline)
            {
                relevantSpans =
                    Spans
                       .Where(s => SpanFilters.All(shouldReturn => shouldReturn(s)) && s.Start > minimumOffset)
                       .ToImmutableList();

                // Upstream uses operation name to identify the "scope", ie. source code used to populate the span
                // as a shortcut to select spans in tests.
                if (relevantSpans.Count(s => operationName == null || s.Name == operationName || s.LogicScope == operationName) >= count)
                {
                    break;
                }

                Thread.Sleep(500);
            }

            foreach (var headers in RequestHeaders)
            {
                // This is the place to check against headers we expect
                AssertHeader(
                    headers,
                    "X-Datadog-Trace-Count",
                    header =>
                    {
                        if (int.TryParse(header, out var traceCount))
                        {
                            return traceCount >= 0;
                        }

                        return false;
                    });
            }

            if (!returnAllOperations)
            {
                // Upstream uses operation name to identify the "scope", ie. source code used to populate the span
                // as a shortcut to select spans in tests.
                relevantSpans =
                    relevantSpans
                       .Where(s => operationName == null || s.Name == operationName || s.LogicScope == operationName)
                       .ToImmutableList();
            }

            foreach (var span in relevantSpans)
            {
                // Upstream uses "http.request.headers.host" tag.
                if (span.Tags.TryGetValue(Tags.HttpRequestHeadersHost, out var value))
                {
                    span.Tags["http.request.headers.host"] = value;
                    span.Tags.Remove(Tags.HttpRequestHeadersHost);
                }
            }

            return relevantSpans;
        }

        public void Dispose()
        {
            // TODO splunk: shutdown gracefully
            _listener?.Close();
            _metricsListener?.Close();
            _cancellationTokenSource.Cancel();
        }

        protected void IgnoreException(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Exceptions.Add(ex);
            }
        }

        protected virtual void OnRequestReceived(HttpListenerContext context)
        {
            RequestReceived?.Invoke(this, new EventArgs<HttpListenerContext>(context));
        }

        protected virtual void OnRequestDeserialized(IList<IList<MockSpan>> traces)
        {
            RequestDeserialized?.Invoke(this, new EventArgs<IList<IList<MockSpan>>>(traces));
        }

        protected virtual void OnMetricsReceived(string stats)
        {
            MetricsReceived?.Invoke(this, new EventArgs<string>(stats));
        }

        private void AssertHeader(
            NameValueCollection headers,
            string headerKey,
            Func<string, bool> assertion)
        {
            var header = headers.Get(headerKey);

            if (string.IsNullOrEmpty(header))
            {
                throw new Exception($"Every submission to the agent should have a {headerKey} header.");
            }

            if (!assertion(header))
            {
                throw new Exception($"Failed assertion for {headerKey} on {header}");
            }
        }

#if NETCOREAPP
        private byte[] GetResponseBytes()
        {
            var responseBody = Encoding.UTF8.GetBytes("{}");
            var contentLength64 = responseBody.LongLength;

            var response = $"HTTP/1.1 200 OK";
            response += DatadogHttpValues.CrLf;
            response += $" Date: {DateTime.UtcNow.ToString("ddd, dd MMM yyyy H:mm::ss K")}";
            response += DatadogHttpValues.CrLf;
            response += $"Connection: Keep-Alive";
            response += DatadogHttpValues.CrLf;
            response += $"Server: dd-mock-agent";

            if (Version != null)
            {
                response += DatadogHttpValues.CrLf;
                response += $"Datadog-Agent-Version: {Version}";
            }

            response += DatadogHttpValues.CrLf;
            response += $"Content-Type: application/json";
            response += DatadogHttpValues.CrLf;
            response += $"Content-Length: {contentLength64}";
            response += DatadogHttpValues.CrLf;
            response += DatadogHttpValues.CrLf;
            response += Encoding.ASCII.GetString(responseBody);

            var responseBytes = Encoding.UTF8.GetBytes(response);
            return responseBytes;
        }

        private void HandlePotentialTraces(MockHttpParser.MockHttpRequest request)
        {
            if (ShouldDeserializeTraces && request.ContentLength >= 1)
            {
                byte[] body = null;
                IList<IList<MockSpan>> spans = null;

                try
                {
                    var i = 0;
                    body = new byte[request.ContentLength];

                    while (request.Body.Stream.CanRead && i < request.ContentLength)
                    {
                        var nextByte = request.Body.Stream.ReadByte();

                        if (nextByte == -1)
                        {
                            break;
                        }

                        body[i] = (byte)nextByte;
                        i++;
                    }

                    if (i < request.ContentLength)
                    {
                        throw new Exception($"Less bytes were sent than we counted. {i} read versus {request.ContentLength} expected.");
                    }

                    spans = MessagePackSerializer.Deserialize<IList<IList<MockSpan>>>(body);
                    OnRequestDeserialized(spans);

                    lock (this)
                    {
                        // we only need to lock when replacing the span collection,
                        // not when reading it because it is immutable
                        Spans = Spans.AddRange(spans.SelectMany(trace => trace));

                        var headerCollection = new NameValueCollection();
                        foreach (var header in request.Headers)
                        {
                            headerCollection.Add(header.Name, header.Value);
                        }

                        RequestHeaders = RequestHeaders.Add(headerCollection);
                    }
                }
                catch (Exception ex)
                {
                    var message = ex.Message.ToLowerInvariant();

                    if (message.Contains("beyond the end of the stream"))
                    {
                        // Accept call is likely interrupted by a dispose
                        // Swallow the exception and let the test finish
                    }

                    throw;
                }
            }
        }

#endif

        private void HandleHttpRequests()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    OnRequestReceived(ctx);
                    if (ShouldDeserializeTraces)
                    {
                        var spans = MessagePackSerializer.Deserialize<IList<IList<MockSpan>>>(ctx.Request.InputStream);
                        OnRequestDeserialized(spans);

                        lock (this)
                        {
                            // we only need to lock when replacing the span collection,
                            // not when reading it because it is immutable
                            Spans = Spans.AddRange(spans.SelectMany(trace => trace));
                            RequestHeaders = RequestHeaders.Add(new NameValueCollection(ctx.Request.Headers));
                        }
                    }

                    if (Version != null)
                    {
                        ctx.Response.AddHeader("Datadog-Agent-Version", Version);
                    }

                    // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
                    // (Setting content-length avoids that)

                    ctx.Response.ContentType = "application/json";
                    var buffer = Encoding.UTF8.GetBytes("{}");
                    ctx.Response.ContentLength64 = buffer.LongLength;
                    ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    ctx.Response.Close();
                }
                catch (HttpListenerException)
                {
                    // listener was stopped,
                    // ignore to let the loop end and the method return
                }
                catch (ObjectDisposedException)
                {
                    // the response has been already disposed.
                }
                catch (InvalidOperationException)
                {
                    // this can occur when setting Response.ContentLength64, with the framework claiming that the response has already been submitted
                    // for now ignore, and we'll see if this introduces downstream issues
                }
                catch (Exception) when (!_listener.IsListening)
                {
                    // we don't care about any exception when listener is stopped
                }
            }
        }

        private void HandleMetricsHttpRequests()
        {
            while (_metricsListener.IsListening)
            {
                try
                {
                    var ctx = _metricsListener.GetContext();
                    var uploadMessage = Vendors.ProtoBuf.Serializer.Deserialize<DataPointUploadMessage>(ctx.Request.InputStream);

                    lock (this)
                    {
                        Metrics = Metrics.AddRange(uploadMessage.datapoints);
                    }

                    ctx.Response.StatusCode = 200;
                    ctx.Response.Close();
                }
                catch (HttpListenerException)
                {
                    // listener was stopped,
                    // ignore to let the loop end and the method return
                }
                catch (ObjectDisposedException)
                {
                    // the response has been already disposed.
                }
                catch (InvalidOperationException)
                {
                    // this can occur when setting Response.ContentLength64, with the framework claiming that the response has already been submitted
                    // for now ignore, and we'll see if this introduces downstream issues
                }
                catch (Exception) when (!_metricsListener.IsListening)
                {
                    // we don't care about any exception when listener is stopped
                }
            }
        }
    }
}
