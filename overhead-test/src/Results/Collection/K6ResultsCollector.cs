using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalFx.OverheadTest.Containers;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Results.Collection;

internal class K6ResultsCollector : IDisposable
{
    private const string IterationDuration = "iteration_duration";
    private const string HttpRequestDuration = "http_req_duration";

    private readonly StreamReader _streamReader;

    public K6ResultsCollector(DirectoryInfo filePath)
    {
        if (filePath == null) throw new ArgumentNullException(nameof(filePath));
        var jsonResultsFile = File.Open(Path.Combine(filePath.FullName, LoadDriver.K6ResultsFile), FileMode.Open);

        _streamReader = new StreamReader(jsonResultsFile);
    }

    public async Task<K6Results> CollectAsync()
    {
        using var jsonTextReader = new JsonTextReader(_streamReader);
        var summary = await JObject.LoadAsync(jsonTextReader);

        var metrics = summary["metrics"];

        var iterationDuration = metrics![IterationDuration];

        var iterationAvg = iterationDuration!["avg"]!.Value<float>();
        var iterationP95 = iterationDuration["p(95)"]!.Value<float>();

        var httpRequestDuration = metrics[HttpRequestDuration];

        var requestAvg = httpRequestDuration!["avg"]!.Value<float>();
        var requestP95 = httpRequestDuration["p(95)"]!.Value<float>();

        return new K6Results(iterationAvg, iterationP95, requestAvg, requestP95);
    }

    public void Dispose()
    {
        _streamReader.Dispose();
    }
}
