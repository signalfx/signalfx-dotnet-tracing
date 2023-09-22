namespace Datadog.Trace.AlwaysOnProfiler;

internal interface IOtlpSender
{
    void Send(object data);
}
