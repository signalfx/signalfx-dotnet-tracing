using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Networks;
using SignalFx.OverheadTest.Configs;
using SignalFx.OverheadTest.Containers;
using SignalFx.OverheadTest.Containers.Local;
using SignalFx.OverheadTest.Containers.Remote;
using SignalFx.OverheadTest.Results;
using SignalFx.OverheadTest.Results.Collection;
using SignalFx.OverheadTest.Results.Persistence;
using SignalFx.OverheadTest.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SignalFx.OverheadTest;

public class OverheadTest : IAsyncLifetime
{
    public const string Prefix = "overhead";

    private const string OverheadTestNetwork = $"{Prefix}-test-network";
    private readonly CollectorBase _collector;

    private readonly DirectoryInfo _iterationResults;

    private readonly IDockerNetwork _network;
    private readonly ITestOutputHelper _testOutputHelper;

    public OverheadTest(ITestOutputHelper testOutputHelper)
    {
        TestcontainersSettings.ResourceReaperEnabled = false;
        _testOutputHelper = testOutputHelper;

        var iterationResults = Path.Combine(Directory.GetCurrentDirectory(), "results",
            DateTime.UtcNow.ToString("yyyyMMddHHmmss"));

        // Creates directory for results specific to this run
        _iterationResults = Directory.CreateDirectory(iterationResults);

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

    private CollectorBase CreateCollector(DirectoryInfo iterationResults)
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

        var testConfig = TestConfig.Default;
        foreach (var agentConfig in testConfig.Agents)
        {
            // TestOutputHelper writes messages to test output, logged at the end of the test when run with dotnet test
            _testOutputHelper.WriteLine($"----------------Running configuration: {agentConfig.Name}");

            var resultsConvention = new NamingConvention(agentConfig, _iterationResults);

            Directory.CreateDirectory(resultsConvention.ContainerLogs);

            _testOutputHelper.WriteLine($"Directory for configuration created at: {resultsConvention.AgentResults}");

            await using var sqlServer = CreateSqlServer(resultsConvention);
            await sqlServer.StartAsync();

            await using var eshopApp = new EshopApp(_network, _collector, sqlServer, resultsConvention, agentConfig);
            await eshopApp.StartAsync();

            _testOutputHelper.WriteLine("----------------Starting warmup.");

            await using var loadDriver = new LoadDriver(_network, resultsConvention, testConfig);

            await using var warmupDriverContainer = loadDriver.BuildWarmup();
            await warmupDriverContainer.StartAsync();

            var warmupExitCode = await warmupDriverContainer.GetExitCode();

            Assert.Equal(0, warmupExitCode);

            _testOutputHelper.WriteLine($"Warmup finished with exit code: {warmupExitCode}");

            // start dotnet-counters inside the app container
            await eshopApp.StartCountersAsync();

            var processList = await eshopApp.ListProcessesAsync();

            // there should be 2 processes running inside the container: the app and dotnet-counters
            Assert.Equal(2, processList.Count);
            Assert.Contains(processList, s => s.Contains("dotnet Web.dll"));
            Assert.Contains(processList, s => s.Contains("./dotnet-counters"));

            _testOutputHelper.WriteLine("----------------Starting driving a load on the app.");
            await loadDriver.StartAsync();

            var exitCode = await loadDriver.StopAsync();

            _testOutputHelper.WriteLine($"Load driver exited with code: {exitCode}");
            Assert.Equal(0, exitCode);

            await eshopApp.StopAsync();
        }

        _testOutputHelper.WriteLine("----------------Starting perf results collection.");
        var results = await new ResultsCollector(_iterationResults).CollectResultsAsync(testConfig.Agents);

        _testOutputHelper.WriteLine("----------------Persisting test results.");
        // creates or appends to a single file, shared between different test runs
        await using var csvPersister = new CsvPersister(_iterationResults.Parent, results);
        await csvPersister.PersistResultsAsync();

        _testOutputHelper.WriteLine("----------------Persisting test configuration.");
        await using var configPersister = new ConfigPersister(_iterationResults.Parent);
        await configPersister.PersistConfigurationAsync(testConfig);
    }

    private SqlServerBase CreateSqlServer(NamingConvention namingConvention)
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
