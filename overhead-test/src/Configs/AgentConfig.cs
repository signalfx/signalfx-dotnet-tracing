using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalFx.OverheadTest.Configs;

internal class AgentConfig
{
    public static readonly AgentConfig Baseline = new(
        "none",
        "no agent at all",
        BaseImageName,
        Enumerable.Empty<KeyValuePair<string, string>>());

    public static readonly AgentConfig Instrumented = new(
        "signalfx-dotnet",
        "signalfx dotnet tracing",
        InstrumentedImageName,
        new KeyValuePair<string, string>[]
        {
            new("SIGNALFX_PROFILER_ENABLED", "0")
        });

    public static readonly AgentConfig Profiled = new(
        "signalfx-dotnet-with-profiling-10s",
        "signalfx dotnet with tracing and default profiling frequency (every 10s)",
        InstrumentedImageName,
        new KeyValuePair<string, string>[]
        {
            new("SIGNALFX_PROFILER_ENABLED", "1")
        });

    public static readonly AgentConfig ProfiledHighFrequency = new(
        "signalfx-dotnet-with-profiling-1s",
        "signalfx dotnet with tracing and max profiling frequency (every 1s)",
        InstrumentedImageName,
        new KeyValuePair<string, string>[]
        {
            new("SIGNALFX_PROFILER_ENABLED", "1"),
            new("SIGNALFX_PROFILER_CALL_STACK_INTERVAL", "1000")
        });

    private const string BaseImageName = "eshop-app-dc";
    private const string InstrumentedImageName = "eshop-app-instrumented-dc";

    private AgentConfig(string name, string description, string dockerImageName, IEnumerable<KeyValuePair<string, string>> additionalEnvVars)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        DockerImageName = dockerImageName ?? throw new ArgumentNullException(nameof(dockerImageName));
        AdditionalEnvVars = additionalEnvVars ?? throw new ArgumentNullException(nameof(additionalEnvVars));
    }

    public string Name { get; }
    public string Description { get; }
    public string DockerImageName { get; }
    public IEnumerable<KeyValuePair<string, string>> AdditionalEnvVars { get; }
}
