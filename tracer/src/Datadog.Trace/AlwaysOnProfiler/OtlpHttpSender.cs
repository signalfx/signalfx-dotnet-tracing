using System;
using System.Net;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagation;

namespace Datadog.Trace.AlwaysOnProfiler;

/// <summary>
/// Sends logs in binary-encoded protobuf format over HTTP.
/// </summary>
internal class OtlpHttpSender : IOtlpSender
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(OtlpHttpSender));

    private readonly Uri _logsEndpointUrl;

    public OtlpHttpSender(Uri logsEndpointUrl)
    {
        _logsEndpointUrl = logsEndpointUrl ?? throw new ArgumentNullException(nameof(logsEndpointUrl));
    }

    public void Send(object data)
    {
        HttpWebRequest httpWebRequest;

        try
        {
            httpWebRequest = WebRequest.CreateHttp(_logsEndpointUrl);
            httpWebRequest.ContentType = "application/x-protobuf";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add(CommonHttpHeaderNames.TracingEnabled, "false");

            using var stream = httpWebRequest.GetRequestStream();
            Vendors.ProtoBuf.Serializer.Serialize(stream, data);
            stream.Flush();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception preparing request to send thread samples to {0}", _logsEndpointUrl);
            return;
        }

        try
        {
            using var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            if (httpWebResponse.StatusCode >= HttpStatusCode.OK && httpWebResponse.StatusCode < HttpStatusCode.MultipleChoices)
            {
                return;
            }

            Log.Warning("HTTP error sending thread samples to {0}: {1}", _logsEndpointUrl, httpWebResponse.StatusCode);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception sending thread samples to {0}", _logsEndpointUrl);
        }
    }
}
