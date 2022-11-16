using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using SignalFx.OverheadTest.Configs;
using SignalFx.OverheadTest.Results;

namespace SignalFx.OverheadTest.Containers;

internal class EshopApp : IAsyncDisposable
{
    public const string ContainerName = $"{OverheadTest.Prefix}-eshop-app";
    public const string CounterResultsFile = "counters.json";
    public const string CounterUpdateInterval = "1";

    private const int AppPort = 80;
    private const string ContainerResultsPath = $"/app/{CounterResultsFile}";

    private readonly TestcontainersContainer _container;
    private readonly Stream _logStream;
    private readonly Stream _resultStream;


    public EshopApp(IDockerNetwork network, CollectorBase collector, SqlServerBase sqlServer,
        NamingConvention namingConvention, AgentConfig config)
    {
        if (network == null) throw new ArgumentNullException(nameof(network));
        if (collector == null) throw new ArgumentNullException(nameof(collector));
        if (sqlServer == null) throw new ArgumentNullException(nameof(sqlServer));
        if (namingConvention == null) throw new ArgumentNullException(nameof(namingConvention));
        if (config == null) throw new ArgumentNullException(nameof(config));

        _logStream = File.Create(Path.Combine(namingConvention.ContainerLogs, "eshop-app.txt"));
        _resultStream = File.Create(Path.Combine(namingConvention.AgentResults, CounterResultsFile));

        var instrumentationLogs = Path.Combine(namingConvention.AgentResults, "instrumentation-logs");
        Directory.CreateDirectory(instrumentationLogs);

        var testcontainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(config.DockerImageName)
            .WithName(ContainerName)
            .WithNetwork(network)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("SIGNALFX_ENDPOINT_URL", collector.TraceReceiverUrl)
            .WithEnvironment("SIGNALFX_PROFILER_LOGS_ENDPOINT", collector.LogsReceiverUrl)
            .WithEnvironment("SIGNALFX_METRICS_ENDPOINT_URL", collector.MetricsReceiverUrl)
            .WithEnvironment("SIGNALFX_PROFILER_EXCLUDE_PROCESSES", "dotnet-counters")
            .WithEnvironment("SIGNALFX_TRACE_BUFFER_SIZE", "3000")
            .WithEnvironment("ConnectionStrings__CatalogConnection", sqlServer.CatalogConnection)
            .WithEnvironment("ConnectionStrings__IdentityConnection", sqlServer.IdentityConnection)
            .WithEnvironment("Logging__LogLevel__Microsoft", "Warning")
            .WithEnvironment("Logging__LogLevel__Default", "Warning")
            .WithEnvironment("Logging__LogLevel__System", "Warning")
            .WithEnvironment("Logging__LogLevel__Microsoft.Hosting.Lifetime", "Information")
            .WithBindMount(instrumentationLogs, "/var/log/signalfx")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(AppPort))
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(_logStream, _logStream));
        foreach (var envVar in config.AdditionalEnvVars)
        {
            testcontainersBuilder = testcontainersBuilder.WithEnvironment(envVar.Name, envVar.Value);
        }
        _container = testcontainersBuilder.Build();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.CleanUpAsync();
        await _logStream.DisposeAsync();
        await _resultStream.DisposeAsync();
    }

    internal Task StartAsync() => _container.StartAsync();


    internal async Task StartCountersAsync()
    {
        var command = new[]
        {
            "./dotnet-counters",
            "collect",
            "--process-id", "1",
            "--refresh-interval", CounterUpdateInterval,
            "--format", "json",
            "--output", ContainerResultsPath
        };
        using var client = new DockerClientConfiguration().CreateClient();

        var execCreateResponse = await client.Exec.ExecCreateContainerAsync(_container.Id, new ContainerExecCreateParameters {Cmd = command});
        await client.Exec.StartContainerExecAsync(execCreateResponse.ID);
    }

    internal async Task<IList<string>> ListProcessesAsync()
    {
        using var client = new DockerClientConfiguration().CreateClient();
        var containerProcessesResponse = await client.Containers.ListProcessesAsync(_container.Id, new ContainerListProcessesParameters());
        // each process is a list of strings, with last string being cmd
        return containerProcessesResponse.Processes.Select(list => list[^1]).ToList();
    }

    internal async Task StopAsync()
    {
        await _container.StopAsync();

        // for now, save container's file content locally
        var fileContent = await _container.ReadFileAsync(ContainerResultsPath);
        await _resultStream.WriteAsync(fileContent);
    }
}
