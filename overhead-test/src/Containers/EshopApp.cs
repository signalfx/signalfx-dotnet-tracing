using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.OutputConsumers;
using DotNet.Testcontainers.Containers.WaitStrategies;
using DotNet.Testcontainers.Networks;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Containers;

internal class EshopApp : IAsyncDisposable
{
    internal const string ContainerName = $"{Constants.Prefix}-eshop-app";
    private const int AppPort = 80;
    private readonly TestcontainersContainer _container;
    private readonly Stream _stream;

    public EshopApp(IDockerNetwork network, CollectorBase collector, SqlServerBase sqlServer,
        ResultsNamingConvention resultsNamingConvention, AgentConfig config)
    {
        if (network == null) throw new ArgumentNullException(nameof(network));
        if (collector == null) throw new ArgumentNullException(nameof(collector));
        if (sqlServer == null) throw new ArgumentNullException(nameof(sqlServer));
        if (resultsNamingConvention == null) throw new ArgumentNullException(nameof(resultsNamingConvention));
        if (config == null) throw new ArgumentNullException(nameof(config));

        _stream = File.Create(Path.Combine(resultsNamingConvention.ContainerLogs, "eshop-app.txt"));

        _container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(config.DockerImageName)
            .WithName(ContainerName)
            .WithNetwork(network)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("SIGNALFX_ENDPOINT_URL", collector.TraceReceiverUrl)
            .WithEnvironment("SIGNALFX_PROFILER_ENABLED", config.ProfilerEnabled)
            .WithEnvironment("SIGNALFX_PROFILER_LOGS_ENDPOINT", collector.LogsReceiverUrl)
            .WithEnvironment("ConnectionStrings__CatalogConnection", sqlServer.CatalogConnection)
            .WithEnvironment("ConnectionStrings__IdentityConnection", sqlServer.IdentityConnection)
            .WithEnvironment("Logging__LogLevel__Microsoft", "Warning")
            .WithEnvironment("Logging__LogLevel__Default", "Warning")
            .WithEnvironment("Logging__LogLevel__System", "Warning")
            .WithEnvironment("Logging__LogLevel__Microsoft.Hosting.Lifetime", "Information")
            .WithMount(resultsNamingConvention.AgentResults, "/results")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(AppPort))
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(_stream, _stream))
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        await _stream.DisposeAsync();
    }

    internal Task StartAsync() => _container.StartAsync();

    internal Task StopAsync() => _container.StopAsync();
}
