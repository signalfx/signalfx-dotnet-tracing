// <copyright file="PerformanceCountersListener.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#if NETFRAMEWORK
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.Logging;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.StatsdClient;
using MetricType = Datadog.Tracer.SignalFx.Metrics.Protobuf.MetricType;

namespace Datadog.Trace.RuntimeMetrics
{
    internal class PerformanceCountersListener : IRuntimeMetricsListener
    {
        private const string MemoryCategoryName = ".NET CLR Memory";
        private const string ThreadingCategoryName = ".NET CLR LocksAndThreads";
        private const string GarbageCollectionMetrics = $"{MetricsNames.Gc.HeapSize}, {MetricsNames.Gc.CollectionsCount}";

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<PerformanceCountersListener>();

        private readonly ISignalFxMetricSender _metricSender;
        private readonly string _processName;
        private readonly int _processId;

        private string _instanceName;
        private PerformanceCounterCategory _memoryCategory;
        private bool _fullInstanceName;

        private PerformanceCounterWrapper _gen0Size;
        private PerformanceCounterWrapper _gen1Size;
        private PerformanceCounterWrapper _gen2Size;
        private PerformanceCounterWrapper _lohSize;
        private PerformanceCounterWrapper _contentionCount;

        private Task _initializationTask;

        public PerformanceCountersListener(ISignalFxMetricSender metricSender)
        {
            _metricSender = metricSender;

            ProcessHelpers.GetCurrentProcessInformation(out _processName, out _, out _processId);

            // To prevent a potential deadlock when hosted in a service, performance counter initialization must be asynchronous
            // That's because performance counters may rely on wmiApSrv being started,
            // and the windows service manager only allows one service at a time to be starting: https://docs.microsoft.com/en-us/windows/win32/services/service-startup
            _initializationTask = Task.Run(InitializePerformanceCounters);
        }

        public Task WaitForInitialization() => _initializationTask;

        public void Dispose()
        {
            _gen0Size?.Dispose();
            _gen1Size?.Dispose();
            _gen2Size?.Dispose();
            _lohSize?.Dispose();
            _contentionCount?.Dispose();
        }

        public void Refresh()
        {
            if (_initializationTask.Status != TaskStatus.RanToCompletion)
            {
                return;
            }

            if (!_fullInstanceName)
            {
                _instanceName = GetSimpleInstanceName();
            }

            TryUpdateGauge(MetricsNames.Gc.HeapSize, _gen0Size, GcMetrics.Tags.Gen0);
            TryUpdateGauge(MetricsNames.Gc.HeapSize, _gen1Size, GcMetrics.Tags.Gen1);
            TryUpdateGauge(MetricsNames.Gc.HeapSize, _gen2Size, GcMetrics.Tags.Gen2);
            TryUpdateGauge(MetricsNames.Gc.HeapSize, _lohSize, GcMetrics.Tags.LargeObjectHeap);

            TryUpdateCounter(MetricsNames.ContentionCount, _contentionCount);

            Log.Debug("Sent the following metrics: {metrics}", GarbageCollectionMetrics);
        }

        protected virtual void InitializePerformanceCounters()
        {
            try
            {
                _memoryCategory = new PerformanceCounterCategory(MemoryCategoryName);

                var instanceName = GetInstanceName();
                _fullInstanceName = instanceName.Item2;
                _instanceName = instanceName.Item1;

                _gen0Size = new PerformanceCounterWrapper(MemoryCategoryName, "Gen 0 heap size", _instanceName);
                _gen1Size = new PerformanceCounterWrapper(MemoryCategoryName, "Gen 1 heap size", _instanceName);
                _gen2Size = new PerformanceCounterWrapper(MemoryCategoryName, "Gen 2 heap size", _instanceName);
                _lohSize = new PerformanceCounterWrapper(MemoryCategoryName, "Large Object Heap size", _instanceName);
                _contentionCount = new PerformanceCounterWrapper(ThreadingCategoryName, "Total # of Contentions", _instanceName);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.Contains("'Global'"))
            {
                // Catching error UnauthorizedAccessException: Access to the registry key 'Global' is denied.
                // The 'Global' part seems consistent across localizations

                Log.Error(ex, "The process does not have sufficient permissions to read performance counters. Please refer to https://dtdg.co/net-runtime-metrics to learn how to grant those permissions.");
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occured while initializing the performance counters");
                throw;
            }
        }

        private void TryUpdateGauge(string path, PerformanceCounterWrapper counter, string[] tags)
        {
            var value = counter.GetValue(_instanceName);

            if (value != null)
            {
                _metricSender.SendLong(path, value.Value, MetricType.GAUGE, tags);
            }
        }

        private void TryUpdateCounter(string path, PerformanceCounterWrapper counter)
        {
            var value = counter.GetValue(_instanceName);

            if (value == null)
            {
                return;
            }

            _metricSender.SendLong(path, value.Value, MetricType.CUMULATIVE_COUNTER);
        }

        private Tuple<string, bool> GetInstanceName()
        {
            var instanceNames = _memoryCategory.GetInstanceNames().Where(n => n.StartsWith(_processName)).ToArray();

            // The instance can contain the pid, which will avoid looking through multiple processes that would have the same name
            // See https://docs.microsoft.com/en-us/dotnet/framework/debug-trace-profile/performance-counters-and-in-process-side-by-side-applications#performance-counters-for-in-process-side-by-side-applications
            var fullName = instanceNames.FirstOrDefault(n => n.StartsWith($"{_processName}_p{_processId}_r"));

            if (fullName != null)
            {
                return Tuple.Create(fullName, true);
            }

            if (instanceNames.Length == 1)
            {
                return Tuple.Create(instanceNames[0], false);
            }

            return Tuple.Create(GetSimpleInstanceName(), false);
        }

        private string GetSimpleInstanceName()
        {
            var instanceNames = _memoryCategory.GetInstanceNames().Where(n => n.StartsWith(_processName)).ToArray();

            if (instanceNames.Length == 1)
            {
                return instanceNames[0];
            }

            foreach (var name in instanceNames)
            {
                int instancePid;

                using (var counter = new PerformanceCounter(MemoryCategoryName, "Process ID", name, true))
                {
                    instancePid = (int)counter.NextValue();
                }

                if (instancePid == _processId)
                {
                    return name;
                }
            }

            return null;
        }
    }
}
#endif
