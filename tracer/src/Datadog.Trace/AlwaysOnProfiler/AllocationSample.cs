namespace Datadog.Trace.AlwaysOnProfiler;

internal class AllocationSample
{
    public AllocationSample(AllocationDetails allocationDetails, ThreadSample threadSample)
    {
        ThreadSample = threadSample;
        AllocationDetails = allocationDetails;
    }

    public AllocationDetails AllocationDetails { get; }

    public ThreadSample ThreadSample { get; }
}
