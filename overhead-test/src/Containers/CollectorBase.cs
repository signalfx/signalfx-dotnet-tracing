﻿using System;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Modules;

namespace SignalFx.OverheadTest.Containers;

internal abstract class CollectorBase : IAsyncDisposable
{
    protected const string ImageName = "otel/opentelemetry-collector-contrib:0.51.0";
    protected const string CollectorConfigDestination = "/etc/otelcol-contrib/config.yaml";

    protected const int TraceReceiverPort = 9411;
    protected const int LogReceiverPort = 4318;
    protected readonly Stream Stream;

    protected CollectorBase(string resultsDirectory)
    {
        if (resultsDirectory == null) throw new ArgumentNullException(nameof(resultsDirectory));
        Stream = File.Create(Path.Combine(resultsDirectory, "collector.txt"));
    }

    protected abstract TestcontainersContainer Container { get; }
    protected abstract string Address { get; }

    internal string TraceReceiverUrl => $"http://{Address}:{TraceReceiverPort}/api/v2/spans";
    internal string LogsReceiverUrl => $"http://{Address}:{LogReceiverPort}/v1/logs";

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
        await Stream.DisposeAsync();
    }

    internal Task StartAsync() => Container.StartAsync();
}