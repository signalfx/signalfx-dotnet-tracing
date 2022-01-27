// Modified by Splunk Inc.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using OpenTelemetry.TestHelpers.Proto.Collector.Logs.V1;
using OpenTelemetry.TestHelpers.Proto.Logs.V1;

namespace OpenTelemetry.TestHelpers;

public class MockOtelLogsCollector : IDisposable
{
    private readonly HttpListener _listener;

    public MockOtelLogsCollector(int port = 4318)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Prefixes.Add($"http://localhost:{port}/");

        listener.Start();

        // successfully listening
        Port = port;
        _listener = listener;

        var listenerThread = new Thread(HandleHttpRequests);
        listenerThread.Start();
    }

    /// <summary>
    /// Gets the TCP port that this Agent is listening on.
    /// </summary>
    public int Port { get; }

    public ConcurrentQueue<LogsData> LogsData { get; } = new();

    public void Dispose()
    {
        _listener.Stop();
    }

    private void HandleHttpRequests()
    {
        while (_listener.IsListening)
        {
            try
            {
                var ctx = _listener.GetContext();

                var logsData = ProtoBuf.Serializer.Deserialize<LogsData>(ctx.Request.InputStream);

                LogsData.Enqueue(logsData);

                ctx.Response.ContentType = "application/x-protobuf";

                var exportLogsServiceResponse = new ExportLogsServiceResponse();

                ProtoBuf.Serializer.Serialize(ctx.Response.OutputStream, exportLogsServiceResponse);
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
}
