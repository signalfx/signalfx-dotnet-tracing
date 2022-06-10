using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SignalFx.OverheadTest.Configs;

namespace SignalFx.OverheadTest.Results.Persistence;

/// <summary>
/// Persists test configuration in json format.
/// </summary>
internal class ConfigPersister : IAsyncDisposable
{
    private readonly StreamWriter _streamWriter;

    public ConfigPersister(DirectoryInfo path)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        var stream = File.Create(Path.Combine(path.FullName, "config.json"));
        _streamWriter = new StreamWriter(stream);
    }

    public async Task PersistConfigurationAsync(TestConfig config)
    {
        var serialized = JsonConvert.SerializeObject(new
        {
            // use java-overhead-tests format/names for now 
            name = $"Release_{config.ConcurrentConnections}vu_{config.Iterations}iter",
            description = "Multiple agent configurations compared.",
            agents = config.Agents.Select(agent => agent.Name),
            maxRequestRate = config.MaxRequestRate,
            concurrentConnections = config.ConcurrentConnections,
            totalIterations = config.Iterations
        });

        await _streamWriter.WriteAsync(serialized);
    }

    public async ValueTask DisposeAsync()
    {
        await _streamWriter.DisposeAsync();
    }
}
