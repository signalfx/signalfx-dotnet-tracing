using System;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using SignalFx.OverheadTest.Results;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Containers.Remote;

internal class RemoteSqlServer : SqlServerBase
{
    private readonly DockerEndpoint _endpoint;

    public RemoteSqlServer(DockerEndpoint endpoint, NamingConvention namingConvention) : base(
        namingConvention)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(ImageName)
            .WithName($"{OverheadTest.Prefix}-remote-sqlserver")
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
