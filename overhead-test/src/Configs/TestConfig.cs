using System.Collections.Generic;

namespace SignalFx.OverheadTest.Configs;

internal class TestConfig
{
    public static readonly TestConfig Default = DefaultConfig();

    private static TestConfig DefaultConfig()
    {
        return new TestConfig(
            new[]
            {
                AgentConfig.Baseline,
                AgentConfig.Instrumented,
                AgentConfig.CpuProfiled,
                AgentConfig.MemoryProfiled,
                AgentConfig.CpuAndMemoryProfiled,
            },
            8500,
            30,
            900);
    }

    private TestConfig(IList<AgentConfig> agents, int iterations, int concurrentConnections, int maxRequestRate)
    {
        Agents = agents;
        Iterations = iterations;
        ConcurrentConnections = concurrentConnections;
        MaxRequestRate = maxRequestRate;
    }

    public IList<AgentConfig> Agents { get; }
    public int Iterations { get; }
    public int ConcurrentConnections { get; }
    public int MaxRequestRate { get; }
}
