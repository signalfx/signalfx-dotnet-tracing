namespace Datadog.Trace.AlwaysOnProfiler;

internal interface INativeBufferExporter
{
    void Export(byte[] buffer);
}
