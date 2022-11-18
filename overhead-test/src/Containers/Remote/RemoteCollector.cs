using System;
using System.IO;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Containers.Remote;

internal class RemoteCollector : CollectorBase
{
    private const string HostCollectorConfigPath = "/home/splunk/otel-config.yaml";
    private readonly DockerEndpoint _dockerEndpoint;

    public RemoteCollector(DockerEndpoint dockerEndpoint, DirectoryInfo iterationResults) : base(iterationResults)
    {
        _dockerEndpoint = dockerEndpoint ?? throw new ArgumentNullException(nameof(dockerEndpoint));
        Container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(ImageName)
            .WithName($"{OverheadTest.Prefix}-remote-collector")
            .WithDockerEndpoint(_dockerEndpoint.Url)
            .WithExposedPort(LogReceiverPort)
            .WithExposedPort(TraceReceiverPort)
            .WithExposedPort(MetricReceiverPort)
            .WithPortBinding(LogReceiverPort, LogReceiverPort)
            .WithPortBinding(TraceReceiverPort, TraceReceiverPort)
            .WithPortBinding(MetricReceiverPort, MetricReceiverPort)
            .WithBindMount(HostCollectorConfigPath, ContainerCollectorConfigPath)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(Stream, Stream))
            .Build();
    }

    protected override string Address => _dockerEndpoint.Hostname;

    protected override TestcontainersContainer Container { get; }
}
