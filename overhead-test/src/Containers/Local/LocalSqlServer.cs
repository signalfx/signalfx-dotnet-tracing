using System;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.OutputConsumers;
using DotNet.Testcontainers.Containers.WaitStrategies;
using DotNet.Testcontainers.Networks;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Containers.Local;

internal class LocalSqlServer : SqlServerBase
{
    public LocalSqlServer(IDockerNetwork network, ResultsNamingConvention resultsNamingConvention) : base(
        resultsNamingConvention)
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

    protected sealed override string Address => $"{Constants.Prefix}-local-sqlserver";

    protected override TestcontainersContainer Container { get; }
}
