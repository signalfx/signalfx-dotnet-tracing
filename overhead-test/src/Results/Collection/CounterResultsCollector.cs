using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalFx.OverheadTest.Containers;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Results.Collection;

internal class CounterResultsCollector : IDisposable
{
    private const string CpuUsage = "CPU Usage (%)";
    private const string WorkingSet = "Working Set (MB)";
    private const string PercentTimeInGc = "% Time in GC since last GC (%)";
    private const string HeapSize = "GC Heap Size (MB)";
    private const string Events = "Events";
    private const string AllocationRate = $"Allocation Rate (B / {EshopApp.CounterUpdateInterval} sec)";

    private readonly StreamReader _streamReader;

    public CounterResultsCollector(DirectoryInfo filePath)
    {
        if (filePath == null) throw new ArgumentNullException(nameof(filePath));
        var counterResultsFile = File.Open(Path.Combine(filePath.FullName, EshopApp.CounterResultsFile), FileMode.Open);

        _streamReader = new StreamReader(counterResultsFile);
    }

    public async Task<CounterResults> CollectAsync()
    {
        using var jsonTextReader = new JsonTextReader(_streamReader);
        var counters = await JObject.LoadAsync(jsonTextReader);

        var events = counters[Events];

        var averageCpuUsage = ComputeAverage(events, CpuUsage);

        var averageWorkingSet = ComputeAverage(events, WorkingSet);

        var averageTimeInGc = ComputeAverage(events, PercentTimeInGc);

        var heapSizeStats = ExtractValues(events, HeapSize);

        var minHeapUsed = heapSizeStats.Min();
        var maxHeapUsed = heapSizeStats.Max();

        var allocationRateStats = ExtractValues(events, AllocationRate);

        var totalAllocated = allocationRateStats.Sum() / (1024 * 1024);

        return new CounterResults(
            averageCpuUsage,
            averageWorkingSet,
            averageTimeInGc,
            minHeapUsed,
            maxHeapUsed,
            totalAllocated);
    }

    private static double ComputeAverage(JToken events, string metricName)
    {
        var metricStats = ExtractValues(events, metricName);
        return metricStats.Average();
    }

    private static IList<double> ExtractValues(JToken events, string metricName)
    {
        return events
            .Where(token => token["name"].Value<string>() == metricName)
            .Select(token => token["value"]!.Value<double>())
            .ToList();
    }

    public void Dispose()
    {
        _streamReader.Dispose();
    }
}
