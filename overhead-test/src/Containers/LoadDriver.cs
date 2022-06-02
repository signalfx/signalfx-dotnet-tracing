using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.OutputConsumers;
using DotNet.Testcontainers.Networks;
using SignalFx.OverheadTest.Utils;

namespace SignalFx.OverheadTest.Containers;

internal class LoadDriver : IAsyncDisposable
{
    private readonly TestcontainersContainer _container;
    private readonly IDockerNetwork _network;
    private readonly Stream _stream;

    public LoadDriver(IDockerNetwork network, ResultsNamingConvention namingConvention)
    {
        _network = network ?? throw new ArgumentNullException(nameof(network));
        if (namingConvention == null) throw new ArgumentNullException(nameof(namingConvention));

        _stream = File.Create(Path.Combine(namingConvention.ContainerLogs, "k6.txt"));
        _container = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("k6-eshop")
            .WithName($"{Constants.Prefix}-k6-load")
            .WithNetwork(_network)
            .WithCommand("run", "-u", "30", "-e", $"ESHOP_HOSTNAME={EshopApp.ContainerName}", "-i", "3000",
                "/app/basic.js", "--summary-export", "/results/k6-test-summary.json")
            .WithMount("K6/basic.js", "/app/basic.js")
            .WithMount(namingConvention.AgentResults, "/results")
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(_stream, _stream))
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        await _stream.DisposeAsync();
    }

    internal Task StartAsync() => _container.StartAsync();

    internal Task<long> GetExitCodeAsync() => _container.GetExitCode();

    internal TestcontainersContainer BuildWarmup()
    {
        return new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("grafana/k6")
            .WithName($"{Constants.Prefix}-k6-warmup")
            .WithNetwork(_network)
            .WithCommand("run", "-u", "10", "-e", $"ESHOP_HOSTNAME={EshopApp.ContainerName}", "-i", "500",
                "/app/basic.js")
            .WithMount("K6/basic.js", "/app/basic.js")
            .Build();
    }
}
