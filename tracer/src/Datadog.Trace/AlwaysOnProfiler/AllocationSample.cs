namespace Datadog.Trace.AlwaysOnProfiler;

internal class AllocationSample
{
    public AllocationSample(long allocationSizeBytes, string allocationTypeName, ThreadSample threadSample)
    {
        AllocationSizeBytes = allocationSizeBytes;
        TypeName = allocationTypeName;
        ThreadSample = threadSample;
    }

    public long AllocationSizeBytes { get; }

    public string TypeName { get; }

    public ThreadSample ThreadSample { get; }
}
