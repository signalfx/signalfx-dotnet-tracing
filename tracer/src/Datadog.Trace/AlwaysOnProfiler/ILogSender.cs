using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal interface ILogSender
{
    void Send(LogsData logsData);
}
