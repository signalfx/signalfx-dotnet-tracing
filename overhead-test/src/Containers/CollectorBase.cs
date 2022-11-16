using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;

namespace SignalFx.OverheadTest.Containers;

internal abstract class CollectorBase : IAsyncDisposable
{
    protected const string ImageName = "otel/opentelemetry-collector-contrib:0.58.0";
    protected const string ContainerCollectorConfigPath = "/etc/otelcol-contrib/config.yaml";

    protected const int TraceReceiverPort = 9411;
    protected const int LogReceiverPort = 4318;
    protected const int MetricReceiverPort = 9943;
    protected readonly Stream Stream;

    protected CollectorBase(DirectoryInfo resultsDirectory)
    {
        if (resultsDirectory == null) throw new ArgumentNullException(nameof(resultsDirectory));
        Stream = File.Create(Path.Combine(resultsDirectory.FullName, "collector.txt"));
    }

    protected abstract TestcontainersContainer Container { get; }
    protected abstract string Address { get; }

    internal string TraceReceiverUrl => $"http://{Address}:{TraceReceiverPort}/api/v2/spans";
    internal string LogsReceiverUrl => $"http://{Address}:{LogReceiverPort}/v1/logs";
    internal string MetricsReceiverUrl => $"http://{Address}:{MetricReceiverPort}/v2/datapoint";

    public async ValueTask DisposeAsync()
    {
        await Container.CleanUpAsync();
        await Stream.DisposeAsync();
    }

    internal Task StartAsync() => Container.StartAsync();
}
