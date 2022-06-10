using System.IO;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace SignalFx.OverheadTest.Containers.Local;

internal class LocalCollector : CollectorBase
{
    private const string CollectorConfigPath = "Docker/otel-config.yaml";

    public LocalCollector(IDockerNetwork network, DirectoryInfo iterationResults) : base(iterationResults)
    {
        var hostCollectorConfigPath = Path.Combine(Directory.GetCurrentDirectory(), CollectorConfigPath);
        Container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(ImageName)
            .WithName(Address)
            .WithNetwork(network)
            .WithBindMount(hostCollectorConfigPath, ContainerCollectorConfigPath)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(Stream, Stream))
            .Build();
    }

    protected sealed override string Address => $"{OverheadTest.Prefix}-local-collector";
    protected override TestcontainersContainer Container { get; }
}
