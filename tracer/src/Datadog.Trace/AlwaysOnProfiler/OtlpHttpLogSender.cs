using System;
using System.Net;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagation;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

/// <summary>
/// Sends logs in binary-encoded protobuf format over HTTP.
/// </summary>
internal class OtlpHttpLogSender : ILogSender
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(OtlpHttpLogSender));

    private readonly ImmutableExporterSettings _exporterSettings;

    public OtlpHttpLogSender(ImmutableExporterSettings exporterSettings)
    {
        _exporterSettings = exporterSettings ?? throw new ArgumentNullException(nameof(exporterSettings));
    }

    public void Send(LogsData logsData)
    {
        HttpWebRequest httpWebRequest;

        try
        {
            httpWebRequest = WebRequest.CreateHttp(_exporterSettings.LogsEndpointUrl);
            httpWebRequest.ContentType = "application/x-protobuf";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add(CommonHttpHeaderNames.TracingEnabled, "false");

            using var stream = httpWebRequest.GetRequestStream();
            Vendors.ProtoBuf.Serializer.Serialize(stream, logsData);
            stream.Flush();
        }
        catch (Exception ex)
        {
            Log.Error("Exception preparing request to send thread samples to {0}: {1}", _exporterSettings.LogsEndpointUrl, ex);
            return;
        }

        try
        {
            using var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            if (httpWebResponse.StatusCode >= HttpStatusCode.OK && httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices)
            {
                return;
            }

            Log.Warning("HTTP error sending thread samples to {0}: {1}", _exporterSettings.LogsEndpointUrl, httpWebResponse.StatusCode);
        }
        catch (Exception ex)
        {
            Log.Error("Exception sending thread samples to {0}: {1}", _exporterSettings.LogsEndpointUrl, ex.Message);
        }
    }
}
