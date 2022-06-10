using System;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using SignalFx.OverheadTest.Results;

namespace SignalFx.OverheadTest.Containers.Local;

internal class LocalSqlServer : SqlServerBase
{
    public LocalSqlServer(IDockerNetwork network, NamingConvention namingConvention) : base(
        namingConvention)
    {
        Container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(ImageName)
            .WithName(Address)
            .WithNetwork(network ?? throw new ArgumentNullException(nameof(network)))
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SA_PASSWORD", TestPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(Port))
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(Stream, Stream))
            .Build();
    }

    protected sealed override string Address => $"{OverheadTest.Prefix}-local-sqlserver";

    protected override TestcontainersContainer Container { get; }
}
