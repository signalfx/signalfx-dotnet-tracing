using System.Diagnostics.CodeAnalysis;

namespace SignalFx.OverheadTest.Results;

// MB is preferred.
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal record CounterResults(
    float AverageTotalCpuPercentage,
    float AverageWorkingSetMB,
    float AverageTimeInGcPercentage,
    float MinHeapUsedMB,
    float MaxHeapUsedMB,
    float TotalAllocatedMB);
