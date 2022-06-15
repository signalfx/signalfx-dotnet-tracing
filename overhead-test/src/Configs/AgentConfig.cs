using System;

namespace SignalFx.OverheadTest.Configs;

internal class AgentConfig
{
    public static readonly AgentConfig Baseline = new("none", BaseImageName, Disabled);
    public static readonly AgentConfig Instrumented = new("signalfx-dotnet", InstrumentedImageName, Disabled);
    public static readonly AgentConfig Profiled = new("signalfx-dotnet-with-profiling", InstrumentedImageName, Enabled);

    private const string BaseImageName = "eshop-app-dc";
    private const string InstrumentedImageName = "eshop-app-instrumented-dc";
    private const string Disabled = "0";
    private const string Enabled = "1";

    private AgentConfig(string name, string dockerImageName, string profilerEnabled)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DockerImageName = dockerImageName ?? throw new ArgumentNullException(nameof(dockerImageName));
        ProfilerEnabled = profilerEnabled ?? throw new ArgumentNullException(nameof(profilerEnabled));
    }

    public string Name { get; }
    public string DockerImageName { get; }
    public string ProfilerEnabled { get; }
}
