namespace Datadog.Trace.AlwaysOnProfiler;

internal class AllocationSample : ThreadSample
{
    public long AllocationSizeBytes { get; set; }

    public string TypeName { get; set; }
}
