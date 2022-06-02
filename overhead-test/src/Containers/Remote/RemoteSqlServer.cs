using System;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.OutputConsumers;
using DotNet.Testcontainers.Containers.WaitStrategies;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Containers.Remote;

internal class RemoteSqlServer : SqlServerBase
{
    private readonly DockerEndpoint _endpoint;

    public RemoteSqlServer(DockerEndpoint endpoint, ResultsNamingConvention resultsNamingConvention) : base(
        resultsNamingConvention)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(ImageName)
            .WithName($"{Constants.Prefix}-remote-sqlserver")
            .WithDockerEndpoint(_endpoint.Url)
            .WithPortBinding(Port, Port)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SA_PASSWORD", TestPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(Port))
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(Stream, Stream))
            .Build();
    }

    protected override string Address => _endpoint.Hostname;
    protected override TestcontainersContainer Container { get; }
}
