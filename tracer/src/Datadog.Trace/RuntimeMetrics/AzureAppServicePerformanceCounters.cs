// <copyright file="AzureAppServicePerformanceCounters.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#if NETFRAMEWORK

using System;
using Datadog.Trace.Logging;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.Newtonsoft.Json;
using Datadog.Trace.Vendors.StatsdClient;
using MetricType = Datadog.Tracer.SignalFx.Metrics.Protobuf.MetricType;

namespace Datadog.Trace.RuntimeMetrics
{
    internal class AzureAppServicePerformanceCounters : IRuntimeMetricsListener
    {
        internal const string EnvironmentVariableName = "WEBSITE_COUNTERS_CLR";
        private const string GarbageCollectionMetrics = $"{MetricsNames.NetRuntime.Gc.HeapSize}, {MetricsNames.NetRuntime.Gc.CollectionsCount}";

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<AzureAppServicePerformanceCounters>();
        private readonly ISignalFxMetricSender _metricSender;

        public AzureAppServicePerformanceCounters(ISignalFxMetricSender metricSender)
        {
            _metricSender = metricSender;
        }

        public void Dispose()
        {
        }

        public void Refresh()
        {
            var rawValue = EnvironmentHelpers.GetEnvironmentVariable(EnvironmentVariableName);
            var value = JsonConvert.DeserializeObject<PerformanceCountersValue>(rawValue);

            _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, value.Gen0Size, MetricType.GAUGE, GcMetrics.Tags.Gen0);
            _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, value.Gen1Size, MetricType.GAUGE, GcMetrics.Tags.Gen1);
            _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, value.Gen2Size, MetricType.GAUGE, GcMetrics.Tags.Gen2);
            _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, value.LohSize, MetricType.GAUGE, GcMetrics.Tags.LargeObjectHeap);

            Log.Debug("Sent the following metrics: {metrics}", GarbageCollectionMetrics);
        }

        private class PerformanceCountersValue
        {
            [JsonProperty("gen0HeapSize")]
            public int Gen0Size { get; set; }

            [JsonProperty("gen1HeapSize")]
            public int Gen1Size { get; set; }

            [JsonProperty("gen2HeapSize")]
            public int Gen2Size { get; set; }

            [JsonProperty("largeObjectHeapSize")]
            public int LohSize { get; set; }
        }
    }
}

#endif
