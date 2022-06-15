using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalFx.OverheadTest.Results.Persistence;

/// <summary>
/// Persists results in csv file format.
/// </summary>
internal class CsvPersister : IAsyncDisposable
{
    private readonly IList<AgentPerfResults> _results;
    private const string TimestampHeader = "timestamp";
    private const string ResultsFilename = "results.csv";
    private const char FieldSeparator = ',';
    private const char FieldNameInfix = ':';

    private static readonly Dictionary<string, Func<AgentPerfResults, string>> Fields = new()
    {
        ["iterationAvg"] = perf => StringFrom(perf.K6Results.IterationDurationAvg),
        ["iterationP95"] = perf => StringFrom(perf.K6Results.IterationDurationP95),
        ["requestAvg"] = perf => StringFrom(perf.K6Results.RequestDurationAvg),
        ["requestP95"] = perf => StringFrom(perf.K6Results.RequestDurationP95),
        ["averageCpuUsage"] = perf => StringFrom(perf.CounterResults.AverageCpuUsage),
        ["averageWorkingSet"] = perf => StringFrom(perf.CounterResults.AverageWorkingSetMB),
        ["averageTimeSpentInGc"] = perf => StringFrom(perf.CounterResults.AverageTimeInGcPercentage),
        ["totalAllocatedMB"] = perf => StringFrom(perf.CounterResults.TotalAllocatedMB),
        ["minHeapUsed"] = perf => StringFrom(perf.CounterResults.MinHeapUsedMB),
        ["maxHeapUsed"] = perf => StringFrom(perf.CounterResults.MaxHeapUsedMB),
        ["maxThreadPoolThreadCount"] = perf => perf.CounterResults.MaxThreadPoolThreadCount.ToString()
    };

    private static string StringFrom(double val)
    {
        return val.ToString(CultureInfo.InvariantCulture);
    }

    private readonly StreamWriter _writer;

    public CsvPersister(DirectoryInfo iterationResultsDirectory, IList<AgentPerfResults> results)
    {
        if (iterationResultsDirectory == null) throw new ArgumentNullException(nameof(iterationResultsDirectory));
        _results = results ?? throw new ArgumentNullException(nameof(results));

        var filePath = Path.Combine(iterationResultsDirectory.FullName, ResultsFilename);
        var shouldWriteHeader = !File.Exists(filePath);

        var fileStream = File.Open(filePath, FileMode.Append);
        _writer = new StreamWriter(fileStream);
        if (shouldWriteHeader)
        {
            _writer.Write(Header(_results));
        }
    }

    public async Task PersistResultsAsync()
    {
        var sb = new StringBuilder();
        sb.Append((int) DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        foreach (var extractor in Fields.Values)
        {
            foreach (var result in _results)
            {
                sb.Append(FieldSeparator).Append(extractor(result));
            }
        }

        sb.Append('\n');
        await _writer.WriteAsync(sb.ToString());
    }

    private static string Header(IList<AgentPerfResults> results)
    {
        var sb = new StringBuilder(TimestampHeader);
        foreach (var fieldKey in Fields.Keys)
        {
            foreach (var agent in results.Select(pr => pr.AgentConfig))
            {
                sb.Append(FieldSeparator).Append(agent.Name).Append(FieldNameInfix).Append(fieldKey);
            }
        }

        sb.Append('\n');
        return sb.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.DisposeAsync();
    }
}
