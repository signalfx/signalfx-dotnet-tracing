using SignalFx.OverheadTest.Configs;

namespace SignalFx.OverheadTest.Results;

internal record AgentPerfResults(AgentConfig AgentConfig, K6Results K6Results, CounterResults CounterResults);
