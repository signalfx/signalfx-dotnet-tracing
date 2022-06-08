using System;
using System.IO;

namespace SignalFx.OverheadTest.Utils;

internal class ResultsNamingConvention
{
    private readonly AgentConfig _config;
    private readonly string _iterationResults;

    public ResultsNamingConvention(AgentConfig config, string iterationResults)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _iterationResults = iterationResults ?? throw new ArgumentNullException(nameof(iterationResults));
    }


    internal string AgentResults =>
        Path.Combine(_iterationResults, _config.Name);

    public string ContainerLogs => Path.Combine(AgentResults, "containers");
}
