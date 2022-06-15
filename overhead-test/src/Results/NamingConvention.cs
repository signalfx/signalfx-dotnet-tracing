using System;
using System.IO;
using SignalFx.OverheadTest.Configs;

namespace SignalFx.OverheadTest.Results;

internal class NamingConvention
{
    private readonly AgentConfig _config;
    private readonly DirectoryInfo _iterationResults;

    public NamingConvention(AgentConfig config, DirectoryInfo iterationResults)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _iterationResults = iterationResults ?? throw new ArgumentNullException(nameof(iterationResults));
    }


    internal string AgentResults =>
        Path.Combine(_iterationResults.FullName, _config.Name);

    public string ContainerLogs => Path.Combine(AgentResults, "containers");
}
