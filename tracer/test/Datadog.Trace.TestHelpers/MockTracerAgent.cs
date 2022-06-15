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
using Datadog.Trace.Telemetry;
using Datadog.Trace.TestHelpers.Stats;
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

        public MockTracerAgent(int port = 8126, int retries = 5, bool useSfxMetrics = false, bool doNotBindPorts = false, int? requestedStatsDPort = null, bool useTelemetry = false)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            TelemetryEnabled = useTelemetry;
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

        public event EventHandler<EventArgs<MockClientStatsPayload>> StatsDeserialized;

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

        public bool TelemetryEnabled { get; }

        /// <summary>
        /// Gets the filters used to filter out spans we don't want to look at for a test.
        /// </summary>
        public List<Func<MockSpan, bool>> SpanFilters { get; } = new();

        public ConcurrentBag<Exception> Exceptions { get; private set; } = new ConcurrentBag<Exception>();

        public IImmutableList<MockSpan> Spans { get; private set; } = ImmutableList<MockSpan>.Empty;

        public IImmutableList<DataPoint> Metrics { get; private set; } = ImmutableList<DataPoint>.Empty;

        public IImmutableList<MockClientStatsPayload> Stats { get; private set; } = ImmutableList<MockClientStatsPayload>.Empty;

        public IImmutableList<NameValueCollection> RequestHeaders { get; private set; } = ImmutableList<NameValueCollection>.Empty;

        public IImmutableList<NameValueCollection> RequestHeaders { get; private set; } = ImmutableList<NameValueCollection>.Empty;

        /// <summary>
        /// Gets the <see cref="Datadog.Trace.Telemetry.TelemetryData"/> requests received by the telemetry endpoint
        /// </summary>
        public ConcurrentStack<object> Telemetry { get; } = new();

        public IImmutableList<NameValueCollection> TelemetryRequestHeaders { get; private set; } = ImmutableList<NameValueCollection>.Empty;

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
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutInMilliseconds);
            var minimumOffset = (minDateTime ?? DateTimeOffset.MinValue).ToUnixTimeNanoseconds();

            IImmutableList<MockSpan> relevantSpans = ImmutableList<MockSpan>.Empty;

            while (DateTime.UtcNow < deadline)
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

                // Ensure only one Content-Type is specified and that it is msgpack
                AssertHeader(
                    headers,
                    "Content-Type",
                    header =>
                    {
                        if (!header.Equals("application/msgpack"))
                        {
                            return false;
                        }

                        return true;
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

        /// <summary>
        /// Wait for the telemetry condition to be satisfied.
        /// Note that the first telemetry that satisfies the condition is returned
        /// To retrieve all telemetry received, use <see cref="Telemetry"/>
        /// </summary>
        /// <param name="hasExpectedValues">A predicate for the current telemetry.
        /// The object passed to the func will be a <see cref="TelemetryData"/> instance</param>
        /// <param name="timeoutInMilliseconds">The timeout</param>
        /// <param name="sleepTime">The time between checks</param>
        /// <returns>The telemetry that satisfied <paramref name="hasExpectedValues"/></returns>
        public object WaitForLatestTelemetry(
            Func<object, bool> hasExpectedValues,
            int timeoutInMilliseconds = 5000,
            int sleepTime = 200)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutInMilliseconds);

            object latest = default;
            while (DateTime.UtcNow < deadline)
            {
                if (Telemetry.TryPeek(out latest) && hasExpectedValues(latest))
                {
                    break;
                }

                Thread.Sleep(sleepTime);
            }

            return latest;
        }

        public IImmutableList<MockClientStatsPayload> WaitForStats(
            int count,
            int timeoutInMilliseconds = 20000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutInMilliseconds);

            IImmutableList<MockClientStatsPayload> stats = ImmutableList<MockClientStatsPayload>.Empty;

            while (DateTime.UtcNow < deadline)
            {
                stats = Stats;

                if (stats.Count >= count)
                {
                    break;
                }

                Thread.Sleep(500);
            }

            return stats;
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

        protected virtual void OnStatsDeserialized(MockClientStatsPayload stats)
        {
            StatsDeserialized?.Invoke(this, new EventArgs<MockClientStatsPayload>(stats));
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

        private void HandleHttpRequests()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    OnRequestReceived(ctx);

                    if (Version != null)
                    {
                        ctx.Response.AddHeader("Datadog-Agent-Version", Version);
                    }

                    if (TelemetryEnabled && (ctx.Request.Url?.AbsolutePath.StartsWith("/" + TelemetryConstants.AgentTelemetryEndpoint) ?? false))
                    {
                        // telemetry request
                        var telemetry = MockTelemetryAgent<TelemetryData>.DeserializeResponse(ctx.Request.InputStream);
                        Telemetry.Push(telemetry);

                        lock (this)
                        {
                            TelemetryRequestHeaders = TelemetryRequestHeaders.Add(new NameValueCollection(ctx.Request.Headers));
                        }

                        ctx.Response.StatusCode = 200;
                    }
                    else
                    {
                        if (ShouldDeserializeTraces)
                        {
                            if (ctx.Request.Url.AbsolutePath == "/v0.6/stats")
                            {
                                var statsPayload = MessagePackSerializer.Deserialize<MockClientStatsPayload>(ctx.Request.InputStream);
                                OnStatsDeserialized(statsPayload);

                                lock (this)
                                {
                                    Stats = Stats.Add(statsPayload);
                                }
                            }

                            // assume trace request
                            else
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
                        }

                        ctx.Response.ContentType = "application/json";
                        var buffer = Encoding.UTF8.GetBytes("{}");
                        ctx.Response.ContentLength64 = buffer.LongLength;
                        ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    }

                    // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
                    // (Setting content-length avoids that)

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
