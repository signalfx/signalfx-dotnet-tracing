using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SignalFx.OverheadTest.Configs;

namespace SignalFx.OverheadTest.Results.Collection;

public class ResultsCollector
{
    private readonly DirectoryInfo _iterationResults;

    public ResultsCollector(DirectoryInfo iterationResults)
    {
        _iterationResults = iterationResults ?? throw new ArgumentNullException(nameof(iterationResults));
    }

    internal async Task<IList<AgentPerfResults>> CollectResultsAsync(IList<AgentConfig> testConfigurations)
    {
        var results = new List<AgentPerfResults>();
        var directories = _iterationResults.EnumerateDirectories().ToList();

        foreach (var configuration in testConfigurations)
        {
            // find results for agent configuration
            var agentResults = directories.Single(directoryInfo => directoryInfo.Name == configuration.Name);

            using var k6ResultsCollector = new K6ResultsCollector(agentResults);
            var k6Results = await k6ResultsCollector.CollectAsync();

            using var counterResultsCollector = new CounterResultsCollector(agentResults);
            var counterResults = await counterResultsCollector.CollectAsync();

            results.Add(new AgentPerfResults(configuration, k6Results, counterResults));
        }

        return results;
    }
}
