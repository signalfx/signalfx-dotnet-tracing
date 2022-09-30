namespace Datadog.Trace.AlwaysOnProfiler;

internal class AllocationDetails
{
    public AllocationDetails(long allocationSizeBytes, string typeName)
    {
        AllocationSizeBytes = allocationSizeBytes;
        TypeName = typeName;
    }

    public long AllocationSizeBytes { get; }

    public string TypeName { get; }
}
