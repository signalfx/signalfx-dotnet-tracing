using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.OutputConsumers;
using DotNet.Testcontainers.Networks;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Containers.Local;

internal class LocalCollector : CollectorBase
{
    private const string CollectorConfigSource = "Docker/otel-config.yaml";

    public LocalCollector(IDockerNetwork network, string iterationResults) : base(iterationResults)
    {
        Container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(ImageName)
            .WithName(Address)
            .WithNetwork(network)
            .WithMount(CollectorConfigSource, CollectorConfigDestination)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(Stream, Stream))
            .Build();
    }

    protected sealed override string Address => $"{Constants.Prefix}-local-collector";
    protected override TestcontainersContainer Container { get; }
}
