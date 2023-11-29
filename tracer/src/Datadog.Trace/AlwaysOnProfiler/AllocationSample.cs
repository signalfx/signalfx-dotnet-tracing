namespace Datadog.Trace.AlwaysOnProfiler;

internal class AllocationSample : ThreadSample
{
    public long AllocationSizeBytes { get; set; }

    public string TypeName { get; set; } // OTLP_PROFILES: TODO: Add TypeName as an attribute to each stacktrace.
}
