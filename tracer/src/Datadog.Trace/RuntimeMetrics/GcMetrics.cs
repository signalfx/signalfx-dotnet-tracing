// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Datadog.Trace.Vendors.StatsdClient;

namespace Datadog.Trace.RuntimeMetrics;

// source originated from: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/main/src/OpenTelemetry.Instrumentation.Runtime/RuntimeMetrics.cs
internal static class GcMetrics
{
    private const int NumberOfGenerations = 3;

    internal static ReadOnlyCollection<string> GenNames { get; } = new(new List<string> { "gen0", "gen1", "gen2", "loh", "poh" });

    public static string GenerationTag(int i)
    {
        return $"generation:{GenNames[i]}";
    }

    public static void PushCollectionCounts(IDogStatsd dogStatsd)
    {
        long collectionsFromHigherGeneration = 0;

        for (var gen = NumberOfGenerations - 1; gen >= 0; --gen)
        {
            long collectionsFromThisGeneration = GC.CollectionCount(gen);
            var currentValue = collectionsFromThisGeneration - collectionsFromHigherGeneration;

            dogStatsd.Counter(MetricsNames.Gc.CollectionsCount, currentValue, tags: new[] { GenerationTag(gen) });

            collectionsFromHigherGeneration = collectionsFromThisGeneration;
        }
    }
}
