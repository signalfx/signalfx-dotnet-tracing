using System;
using System.Collections.Generic;

namespace SignalFx.OverheadTest;

internal class AgentConfig
{
    private static readonly AgentConfig Baseline = new("baseline-app", "eshop-app-dc", "0");
    private static readonly AgentConfig Instrumented = new("instrumented-app", "eshop-app-instrumented-dc", "0");
    private static readonly AgentConfig Profiled = new("profiled-app", "eshop-app-instrumented-dc", "1");

    private AgentConfig(string name, string dockerImageName, string profilerEnabled)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DockerImageName = dockerImageName ?? throw new ArgumentNullException(nameof(dockerImageName));
        ProfilerEnabled = profilerEnabled ?? throw new ArgumentNullException(nameof(profilerEnabled));
    }

    public string Name { get; }
    public string DockerImageName { get; }
    public string ProfilerEnabled { get; }

    public static IEnumerable<AgentConfig> GetDefaultConfigurations()
    {
        return new[] {Baseline, Instrumented, Profiled};
    }
}
