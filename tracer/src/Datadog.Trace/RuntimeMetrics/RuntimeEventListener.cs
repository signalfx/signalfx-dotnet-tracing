// <copyright file="RuntimeEventListener.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Threading;
using Datadog.Trace.Logging;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.RuntimeMetrics
{
    internal class RuntimeEventListener : EventListener, IRuntimeMetricsListener
    {
        private const string RuntimeEventSourceName = "Microsoft-Windows-DotNETRuntime";
        private const string AspNetCoreHostingEventSourceName = "Microsoft.AspNetCore.Hosting";
        private const string AspNetCoreKestrelEventSourceName = "Microsoft-AspNetCore-Server-Kestrel";
        private const string ThreadStatsMetrics = $"{MetricsNames.NetRuntime.ContentionCount}, {MetricsNames.NetRuntime.ThreadPoolWorkersCount}";

        private const int EventGcHeapStats = 4;

        // https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-garbage-collection-events#gcsuspendeebegin_v1-event
        private const int EventGcSuspendBegin = 9;

        // https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-garbage-collection-events#gcrestarteeend_v1-event
        private const int EventGcRestartEnd = 3;

        // https://github.com/dotnet/runtime/blob/55e2378d86841ec766ee21d5e504d7724c39b53b/src/coreclr/vm/threadsuspend.h#L171
        private const int SuspendForGc = 1;
        private const int SuspendForGcPrep = 6;

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<RuntimeEventListener>();

        private static readonly IReadOnlyDictionary<string, string> MetricsMapping;

#if NET6_0_OR_GREATER
        private static readonly Func<int, ulong> GetGenerationSize;
#endif
        private static bool _isGcInfoAvailable;

        private readonly string _delayInSeconds;

        private readonly ISignalFxMetricSender _metricSender;
        private DateTime? _gcStart;

        static RuntimeEventListener()
        {
            MetricsMapping = new Dictionary<string, string>
            {
                ["current-requests"] = MetricsNames.AspNet.CoreCurrentRequests,
                ["failed-requests"] = MetricsNames.AspNet.CoreFailedRequests,
                ["total-requests"] = MetricsNames.AspNet.CoreTotalRequests,
                ["request-queue-length"] = MetricsNames.AspNet.CoreRequestQueueLength,
                ["current-connections"] = MetricsNames.AspNet.CoreCurrentConnections,
                ["connection-queue-length"] = MetricsNames.AspNet.CoreConnectionQueueLength,
                ["total-connections"] = MetricsNames.AspNet.CoreTotalConnections
            };

            // source originated from: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/bc947a00c3f859cc436f050e81172fc1f8bc09d7/src/OpenTelemetry.Instrumentation.Runtime/RuntimeMetrics.cs
#if NET6_0_OR_GREATER
            var mi = typeof(GC).GetMethod("GetGenerationSize", BindingFlags.NonPublic | BindingFlags.Static);
            if (mi != null)
            {
                GetGenerationSize = mi.CreateDelegate<Func<int, ulong>>();
            }
#endif
        }

        public RuntimeEventListener(ISignalFxMetricSender metricSender, TimeSpan delay)
        {
            _metricSender = metricSender;
            _delayInSeconds = ((int)delay.TotalSeconds).ToString();

            EventSourceCreated += (_, e) => EnableEventSource(e.EventSource);
        }

        private static bool IsGcInfoAvailable
        {
            get
            {
                if (_isGcInfoAvailable)
                {
                    return true;
                }

                if (GC.CollectionCount(0) > 0)
                {
                    _isGcInfoAvailable = true;
                }

                return _isGcInfoAvailable;
            }
        }

        public void Refresh()
        {
            _metricSender.SendLong(MetricsNames.NetRuntime.ContentionCount, Monitor.LockContentionCount, MetricType.CUMULATIVE_COUNTER);

            // TODO splunk: opentelemetry-dotnet-contrib plans to change to ObservableUpDownCounter, will need to be adjusted
            _metricSender.SendLong(MetricsNames.NetRuntime.ThreadPoolWorkersCount, ThreadPool.ThreadCount, MetricType.GAUGE);

#if NET6_0_OR_GREATER
            // source originated from: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/bc947a00c3f859cc436f050e81172fc1f8bc09d7/src/OpenTelemetry.Instrumentation.Runtime/RuntimeMetrics.cs
            var generationInfo = GC.GetGCMemoryInfo().GenerationInfo;
            var maxSupportedLength = Math.Min(generationInfo.Length, GcMetrics.GenNames.Count);
            for (var i = 0; i < maxSupportedLength; i++)
            {
                // heap size returned by GetGenerationSize, based on value of i:
                // 0 -> gen0
                // 1 -> gen1
                // 2 -> gen2
                // 3 -> loh
                // 4 -> poh

                // TODO splunk: opentelemetry-dotnet-contrib plans to change to ObservableUpDownCounter, will need to be adjusted
                _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, (long)GetGenerationSize(i), MetricType.GAUGE, tags: GcMetrics.Tags.GenerationTags[i]);
            }

            if (IsGcInfoAvailable)
            {
                // TODO splunk: opentelemetry-dotnet-contrib plans to change to ObservableUpDownCounter, will need to be adjusted
                _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapCommittedMemory, GC.GetGCMemoryInfo().TotalCommittedBytes, MetricType.GAUGE);
            }
#endif

            _metricSender.SendLong(MetricsNames.NetRuntime.Gc.AllocatedBytes, GC.GetTotalAllocatedBytes(), MetricType.CUMULATIVE_COUNTER);

            Log.Debug("Sent the following metrics: {metrics}", ThreadStatsMetrics);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (_metricSender == null)
            {
                // I know it sounds crazy at first, but because OnEventSourceCreated is called from the base constructor,
                // and EnableEvents is called from OnEventSourceCreated, it's entirely possible that OnEventWritten
                // gets called before the child constructor is called.
                // In that case, just bail out.
                return;
            }

            try
            {
                HandleEvent(eventData);
            }
            catch (Exception ex)
            {
                Log.Warning<int, string>(ex, "Error while processing event {EventId} {EventName}", eventData.EventId, eventData.EventName);
            }
        }

        private void HandleEvent(EventWrittenEventArgs eventData)
        {
#if !NET6_0_OR_GREATER
            if (eventData.EventId == EventGcHeapStats)
            {
                var stats = HeapStats.FromPayload(eventData.Payload);

                // TODO splunk: opentelemetry-dotnet-contrib plans to change to ObservableUpDownCounter, will need to be adjusted
                _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, (long)stats.Gen0Size, MetricType.GAUGE, tags: GcMetrics.Tags.Gen0);
                _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, (long)stats.Gen1Size, MetricType.GAUGE, tags: GcMetrics.Tags.Gen1);
                _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, (long)stats.Gen2Size, MetricType.GAUGE, tags: GcMetrics.Tags.Gen2);
                _metricSender.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, (long)stats.LohSize, MetricType.GAUGE, tags: GcMetrics.Tags.LargeObjectHeap);
            }
#endif

            if (eventData.EventName == "EventCounters")
            {
                ExtractCounters(eventData.Payload);
            }
            else if (eventData.EventId == EventGcSuspendBegin)
            {
                // event is generated also for non-gc related suspends
                // verify suspend reason before setting _gcStart field
                var suspendReason = (uint)eventData.Payload[0];
                if (suspendReason == SuspendForGc || suspendReason == SuspendForGcPrep)
                {
                    _gcStart = eventData.TimeStamp;
                }
            }
            else if (eventData.EventId == EventGcRestartEnd)
            {
                var start = _gcStart;

                // for etw it was possible to miss some events
                // set to null to avoid bogus data
                _gcStart = null;

                if (start != null)
                {
                    _metricSender.SendDouble(MetricsNames.NetRuntime.Gc.PauseTime, (eventData.TimeStamp - start.Value).TotalMilliseconds, MetricType.COUNTER);
                }
            }
        }

        private void EnableEventSource(EventSource eventSource)
        {
            if (eventSource.Name == RuntimeEventSourceName)
            {
                var keywords = Keywords.GC | Keywords.Contention;

                EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)keywords);
            }
            else if (eventSource.Name == AspNetCoreHostingEventSourceName || eventSource.Name == AspNetCoreKestrelEventSourceName)
            {
                var settings = new Dictionary<string, string>
                {
                    ["EventCounterIntervalSec"] = _delayInSeconds
                };

                EnableEvents(eventSource, EventLevel.Critical, EventKeywords.All, settings);
            }
        }

        private void ExtractCounters(ReadOnlyCollection<object> payload)
        {
            for (int i = 0; i < payload.Count; ++i)
            {
                if (!(payload[i] is IDictionary<string, object> eventPayload))
                {
                    continue;
                }

                if (!eventPayload.TryGetValue("Name", out object name)
                    || !MetricsMapping.TryGetValue(name.ToString(), out var statName))
                {
                    continue;
                }

                if (eventPayload.TryGetValue("Mean", out object rawValue)
                    || eventPayload.TryGetValue("Increment", out rawValue))
                {
                    var value = (double)rawValue;

                    _metricSender.SendDouble(statName, value, MetricType.GAUGE);
                    Log.Debug("Sent the following metrics: {metrics}", statName);
                }
                else
                {
                    Log.Debug<object>("EventCounter {CounterName} has no Mean or Increment field", name);
                }
            }
        }
    }
}
#endif
