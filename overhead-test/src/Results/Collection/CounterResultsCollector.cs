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
    private const string ThreadPoolThreadCount = "ThreadPool Thread Count";

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

        var heapSizeStats = ExtractValues<double>(events, HeapSize);

        var allocationRateStats = ExtractValues<long>(events, AllocationRate);

        var totalAllocated = (double)allocationRateStats.Sum() / (1024 * 1024);

        var maxThreadPoolThreadCount = ExtractValues<int>(events, ThreadPoolThreadCount).Max();

        return new CounterResults(
            averageCpuUsage,
            averageWorkingSet,
            averageTimeInGc,
            heapSizeStats.Min(),
            heapSizeStats.Max(),
            totalAllocated,
            maxThreadPoolThreadCount);
    }

    private static double ComputeAverage(JToken events, string metricName)
    {
        var metricStats = ExtractValues<double>(events, metricName);
        return metricStats.Average();
    }

    private static IList<T> ExtractValues<T>(JToken events, string metricName)
    {
        return events
            .Where(token => token["name"].Value<string>() == metricName)
            .Select(token => token["value"]!.Value<T>())
            .ToList();
    }

    public void Dispose()
    {
        _streamReader.Dispose();
    }
}
