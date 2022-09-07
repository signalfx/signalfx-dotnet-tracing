using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using SignalFx.OverheadTest.Configs;
using SignalFx.OverheadTest.Results;

namespace SignalFx.OverheadTest.Containers;

internal class LoadDriver : IAsyncDisposable
{
    public const string K6ResultsFile = "k6-test-summary.json";

    private const string ContainerResultsPath = $"/home/k6/{K6ResultsFile}";
    private const string ContainerScriptPath = "/home/k6/basic.js";
    private const string LoadDriveImageName = "grafana/k6";

    private readonly TestcontainersContainer _container;
    private readonly IDockerNetwork _network;
    private readonly Stream _logStream;
    private readonly Stream _resultStream;
    private readonly string _hostScriptPath;


    public LoadDriver(IDockerNetwork network, NamingConvention namingConvention, TestConfig testConfig)
    {
        _network = network ?? throw new ArgumentNullException(nameof(network));
        if (namingConvention == null) throw new ArgumentNullException(nameof(namingConvention));
        if (testConfig == null) throw new ArgumentNullException(nameof(testConfig));

        _logStream = File.Create(Path.Combine(namingConvention.ContainerLogs, "k6.txt"));
        _resultStream = File.Create(Path.Combine(namingConvention.AgentResults, K6ResultsFile));

        _hostScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "K6", "basic.js");
        _container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(LoadDriveImageName)
            .WithName($"{OverheadTest.Prefix}-k6-load")
            .WithNetwork(_network)
            .WithCommand("run",
                "-u", testConfig.ConcurrentConnections.ToString(),
                "-e", $"ESHOP_HOSTNAME={EshopApp.ContainerName}",
                "-i", testConfig.Iterations.ToString(),
                "--rps", testConfig.MaxRequestRate.ToString(),
                ContainerScriptPath,
                "--summary-export", ContainerResultsPath)
            .WithBindMount(_hostScriptPath, ContainerScriptPath)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(_logStream, _logStream))
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        // An attempt to cleanup not started container
        // results in exception
        if (_container.State != TestcontainersState.Undefined)
        {
            await _container.CleanUpAsync();
        }

        await _logStream.DisposeAsync();
        await _resultStream.DisposeAsync();
    }

    internal Task StartAsync() => _container.StartAsync();


    internal async Task<long> StopAsync()
    {
        var exitCode = await _container.GetExitCode();

        // for now, save container's file content locally
        var fileContent = await _container.ReadFileAsync(ContainerResultsPath);
        await _resultStream.WriteAsync(fileContent);

        return exitCode;
    }

    internal TestcontainersContainer BuildWarmup()
    {
        return new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(LoadDriveImageName)
            .WithName($"{OverheadTest.Prefix}-k6-warmup")
            .WithNetwork(_network)
            .WithCommand("run", "-u", "10", "-e", $"ESHOP_HOSTNAME={EshopApp.ContainerName}", "-i", "500",
                ContainerScriptPath)
            .WithBindMount(_hostScriptPath, ContainerScriptPath)
            .WithAutoRemove(true)
            .Build();
    }
}
