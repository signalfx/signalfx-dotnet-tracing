using System.Diagnostics.CodeAnalysis;

namespace SignalFx.OverheadTest.Results;

// MB is preferred.
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal record CounterResults(
    double AverageCpuUsage,
    double AverageWorkingSetMB,
    double AverageTimeInGcPercentage,
    double MinHeapUsedMB,
    double MaxHeapUsedMB,
    double TotalAllocatedMB,
    int MaxThreadPoolThreadCount);
