using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Networks;
using DotNet.Testcontainers.Networks.Builders;
using SignalFx.OverheadTest.Containers;
using SignalFx.OverheadTest.Containers.Local;
using SignalFx.OverheadTest.Containers.Remote;
using SignalFx.OverheadTest.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SignalFx.OverheadTest;

public class OverheadTest : IAsyncLifetime
{
    private const string OverheadTestNetwork = $"{Constants.Prefix}-test-network";
    private readonly CollectorBase _collector;

    private readonly string _iterationResults;

    private readonly IDockerNetwork _network;
    private readonly ITestOutputHelper _testOutputHelper;

    public OverheadTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        _iterationResults = Path.Combine(Directory.GetCurrentDirectory(), "results",
            DateTime.UtcNow.ToString("yyyyMMddHHmmss"));

        // Creates directory for results specific to this run
        Directory.CreateDirectory(_iterationResults);

        _network = BuildDefaultNetwork();
        _collector = CreateCollector(_iterationResults);
    }

    private static string ExternalsHost { get; } = Environment.GetEnvironmentVariable("EXTERNALS_HOST");

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();
        await _collector.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _collector.DisposeAsync();
        await _network.DeleteAsync();
    }

    private CollectorBase CreateCollector(string iterationResults)
    {
        return ShouldRunLocally()
            ? new LocalCollector(_network, iterationResults)
            : new RemoteCollector(new DockerEndpoint(ExternalsHost), iterationResults);
    }

    [Fact]
    public async Task Run()
    {
        _testOutputHelper.WriteLine($"----------------Starting execution: {(ShouldRunLocally() ? "local" : "remote")}");
        _testOutputHelper.WriteLine($"Directory for iteration results created at: {_iterationResults}");
        var dotnetCounters = new DotnetCounters(_testOutputHelper);

        foreach (var config in AgentConfig.GetDefaultConfigurations())
        {
            // TestOutputHelper writes messages to test output, logged at the end of the test when run with dotnet test
            _testOutputHelper.WriteLine($"----------------Running configuration: {config.Name}");

            var resultsConvention = new ResultsNamingConvention(config, _iterationResults);

            Directory.CreateDirectory(resultsConvention.ContainerLogs);

            _testOutputHelper.WriteLine($"Directory for configuration created at: {resultsConvention.AgentResults}");

            await using var sqlServer = CreateSqlServer(resultsConvention);
            await sqlServer.StartAsync();

            await using var eshopApp = new EshopApp(_network, _collector, sqlServer, resultsConvention, config);
            await eshopApp.StartAsync();

            _testOutputHelper.WriteLine("----------------Starting warmup.");

            await using var loadDriver = new LoadDriver(_network, resultsConvention);

            await using var warmupDriverContainer = loadDriver.BuildWarmup();
            await warmupDriverContainer.StartAsync();

            var warmupExitCode = await warmupDriverContainer.GetExitCode();

            Assert.Equal(0, warmupExitCode);

            _testOutputHelper.WriteLine($"Warmup finished with exit code: {warmupExitCode}");

            // start dotnet-counters inside app container
            dotnetCounters.StartCollecting(EshopApp.ContainerName);

            _testOutputHelper.WriteLine("----------------Starting driving a load on the app.");
            await loadDriver.StartAsync();

            var exitCode = await loadDriver.GetExitCodeAsync();

            _testOutputHelper.WriteLine($"Load driver exited with code: {exitCode}");

            await eshopApp.StopAsync();

            Assert.Equal(0, exitCode);
        }
    }

    private SqlServerBase CreateSqlServer(ResultsNamingConvention namingConvention)
    {
        return ShouldRunLocally()
            ? new LocalSqlServer(_network, namingConvention)
            : new RemoteSqlServer(new DockerEndpoint(ExternalsHost), namingConvention);
    }

    private static bool ShouldRunLocally()
    {
        return string.IsNullOrEmpty(ExternalsHost);
    }

    private static IDockerNetwork BuildDefaultNetwork()
    {
        return new TestcontainersNetworkBuilder()
            .WithName(OverheadTestNetwork)
            .Build();
    }
}
