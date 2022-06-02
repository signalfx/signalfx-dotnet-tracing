using System;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.OutputConsumers;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Containers.Remote;

internal class RemoteCollector : CollectorBase
{
    private const string CollectorConfigSource = "/home/splunk/otel-config.yaml";
    private readonly DockerEndpoint _dockerEndpoint;

    public RemoteCollector(DockerEndpoint dockerEndpoint, string iterationResults) : base(iterationResults)
    {
        _dockerEndpoint = dockerEndpoint ?? throw new ArgumentNullException(nameof(dockerEndpoint));
        Container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(ImageName)
            .WithName($"{Constants.Prefix}-remote-collector")
            .WithDockerEndpoint(_dockerEndpoint.Url)
            .WithExposedPort(LogReceiverPort)
            .WithExposedPort(TraceReceiverPort)
            .WithPortBinding(LogReceiverPort, LogReceiverPort)
            .WithPortBinding(TraceReceiverPort, TraceReceiverPort)
            .WithMount(CollectorConfigSource, CollectorConfigDestination)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(Stream, Stream))
            .Build();
    }

    protected override string Address => _dockerEndpoint.Hostname;

    protected override TestcontainersContainer Container { get; }
}
