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
using Datadog.Trace.Vendors.StatsdClient;

namespace Datadog.Trace.RuntimeMetrics
{
    internal class RuntimeEventListener : EventListener, IRuntimeMetricsListener
    {
        private const string RuntimeEventSourceName = "Microsoft-Windows-DotNETRuntime";
        private const string AspNetCoreHostingEventSourceName = "Microsoft.AspNetCore.Hosting";
        private const string AspNetCoreKestrelEventSourceName = "Microsoft-AspNetCore-Server-Kestrel";
        private const string ThreadStatsMetrics = $"{MetricsNames.ContentionTime}, {MetricsNames.ContentionCount}, {MetricsNames.ThreadPoolWorkersCount}";

        private const int EventContentionStop = 91;

        private const int EventGcHeapStats = 4;
        private const int EventGcSuspendBegin = 9;
        private const int EventGcRestartEnd = 3;

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<RuntimeEventListener>();

        private static readonly IReadOnlyDictionary<string, string> MetricsMapping;

#if NET6_0_OR_GREATER
        private static readonly Func<int, ulong> GetGenerationSize;
#endif
        private static bool _isGcInfoAvailable;
        private readonly Timing _contentionTime = new Timing();

        private readonly string _delayInSeconds;

        private readonly IDogStatsd _statsd;
        private DateTime? _gcStart;

        static RuntimeEventListener()
        {
            MetricsMapping = new Dictionary<string, string>
            {
                ["current-requests"] = MetricsNames.AspNetCoreCurrentRequests,
                ["failed-requests"] = MetricsNames.AspNetCoreFailedRequests,
                ["total-requests"] = MetricsNames.AspNetCoreTotalRequests,
                ["request-queue-length"] = MetricsNames.AspNetCoreRequestQueueLength,
                ["current-connections"] = MetricsNames.AspNetCoreCurrentConnections,
                ["connection-queue-length"] = MetricsNames.AspNetCoreConnectionQueueLength,
                ["total-connections"] = MetricsNames.AspNetCoreTotalConnections
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

        public RuntimeEventListener(IDogStatsd statsd, TimeSpan delay)
        {
            _statsd = statsd;
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
            // Can't use a Timing because Dogstatsd doesn't support local aggregation
            // It means that the aggregations in the UI would be wrong
            _statsd.Gauge(MetricsNames.ContentionTime, _contentionTime.Clear());
            _statsd.Counter(MetricsNames.ContentionCount, Monitor.LockContentionCount);

            // TODO splunk: opentelemetry-dotnet-contrib plans to change to ObservableUpDownCounter, will need to be adjusted
            _statsd.Gauge(MetricsNames.ThreadPoolWorkersCount, ThreadPool.ThreadCount);

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
                _statsd.Gauge(MetricsNames.Gc.HeapSize, GetGenerationSize(i), tags: GcMetrics.Tags.GenerationTags[i]);
            }

            if (IsGcInfoAvailable)
            {
                // TODO splunk: opentelemetry-dotnet-contrib plans to change to ObservableUpDownCounter, will need to be adjusted
                _statsd.Gauge(MetricsNames.Gc.HeapCommittedMemory, GC.GetGCMemoryInfo().TotalCommittedBytes);
            }
#endif

            _statsd.Counter(MetricsNames.Gc.AllocatedBytes, GC.GetTotalAllocatedBytes());

            Log.Debug("Sent the following metrics: {metrics}", ThreadStatsMetrics);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (_statsd == null)
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
                _statsd.Gauge(MetricsNames.Gc.HeapSize, stats.Gen0Size, tags: GcMetrics.Tags.Gen0);
                _statsd.Gauge(MetricsNames.Gc.HeapSize, stats.Gen1Size, tags: GcMetrics.Tags.Gen1);
                _statsd.Gauge(MetricsNames.Gc.HeapSize, stats.Gen2Size, tags: GcMetrics.Tags.Gen2);
                _statsd.Gauge(MetricsNames.Gc.HeapSize, stats.LohSize, tags: GcMetrics.Tags.LargeObjectHeap);
            }
#endif

            if (eventData.EventName == "EventCounters")
            {
                ExtractCounters(eventData.Payload);
            }
            else if (eventData.EventId == EventGcSuspendBegin)
            {
                _gcStart = eventData.TimeStamp;
            }
            else if (eventData.EventId == EventGcRestartEnd)
            {
                var start = _gcStart;

                if (start != null)
                {
                    _statsd.IncrementDouble(MetricsNames.Gc.PauseTime, (eventData.TimeStamp - start.Value).TotalMilliseconds);
                }
            }
            else
            {
                if (eventData.EventId == EventContentionStop)
                {
                    var durationInNanoseconds = (double)eventData.Payload[2];

                    _contentionTime.Time(durationInNanoseconds / 1_000_000);
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

                    _statsd.Gauge(statName, value);
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
