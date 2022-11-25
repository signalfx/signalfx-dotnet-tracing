// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Trace.Vendors.StatsdClient;
using MetricType = Datadog.Tracer.SignalFx.Metrics.Protobuf.MetricType;

namespace Datadog.Trace.RuntimeMetrics;

// source originated from: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/bc947a00c3f859cc436f050e81172fc1f8bc09d7/src/OpenTelemetry.Instrumentation.Runtime/RuntimeMetrics.cs
internal static class GcMetrics
{
    private const int NumberOfGenerations = 3;

    private enum GcGeneration
    {
        Gen0,
        Gen1,
        Gen2,
        LargeObjectHeap,
        PinnedObjectHeap
    }

    public static ReadOnlyCollection<string> GenNames { get; } = new(new List<string> { "gen0", "gen1", "gen2", "loh", "poh" });

    public static void PushCollectionCounts(ISignalFxMetricSender metricSender)
    {
        long collectionsFromHigherGeneration = 0;

        for (var gen = NumberOfGenerations - 1; gen >= 0; --gen)
        {
            long collectionsFromThisGeneration = GC.CollectionCount(gen);
            var currentValue = collectionsFromThisGeneration - collectionsFromHigherGeneration;

            metricSender.SendLong(MetricsNames.NetRuntime.Gc.CollectionsCount, currentValue, MetricType.CUMULATIVE_COUNTER, Tags.GenerationTags[gen]);

            collectionsFromHigherGeneration = collectionsFromThisGeneration;
        }
    }

    internal static class Tags
    {
        public static string[] Gen0 { get; } = new[] { GenerationTag(GcGeneration.Gen0) };

        public static string[] Gen1 { get; } = new[] { GenerationTag(GcGeneration.Gen1) };

        public static string[] Gen2 { get; } = new[] { GenerationTag(GcGeneration.Gen2) };

        public static string[] LargeObjectHeap { get; } = new[] { GenerationTag(GcGeneration.LargeObjectHeap) };

        public static string[] PinnedObjectHeap { get; } = new[] { GenerationTag(GcGeneration.PinnedObjectHeap) };

        public static ReadOnlyCollection<string[]> GenerationTags { get; } = new(new List<string[]>
        {
            Gen0,
            Gen1,
            Gen2,
            LargeObjectHeap,
            PinnedObjectHeap
        });

        private static string GenerationTag(GcGeneration gcGeneration)
        {
            return $"generation:{GenNames[(int)gcGeneration]}";
        }
    }
}
