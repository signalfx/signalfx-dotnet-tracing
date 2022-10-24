// <copyright file="RuntimeMetricsWriter.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;
using Datadog.Trace.Logging;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.SignalFx.Metrics;
using Datadog.Trace.Util;
using Datadog.Trace.Vendors.StatsdClient;
using MetricType = Datadog.Tracer.SignalFx.Metrics.Protobuf.MetricType;

namespace Datadog.Trace.RuntimeMetrics
{
    internal class RuntimeMetricsWriter : IDisposable
    {
        private const string ProcessMetrics = $"{MetricsNames.Process.ThreadsCount}, {MetricsNames.Process.MemoryUsage}, {MetricsNames.Process.MemoryVirtual}, {MetricsNames.Process.CpuTime}, {MetricsNames.Process.CpuUtilization}";

        private static readonly string[] CpuUserTag = new[] { "state:user" };
        private static readonly string[] CpuSystemTag = new[] { "state:system" };

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<RuntimeMetricsWriter>();
        private static readonly Func<ISignalFxMetricSender, TimeSpan, IRuntimeMetricsListener> InitializeListenerFunc = InitializeListener;

        private readonly TimeSpan _delay;

        private readonly ISignalFxMetricSender _metricSender;
        private readonly Timer _timer;

        private readonly IRuntimeMetricsListener _listener;

        private readonly bool _enableProcessMetrics;

        private readonly ConcurrentDictionary<string, int> _exceptionCounts = new ConcurrentDictionary<string, int>();

        private TimeSpan _previousUserCpu;
        private TimeSpan _previousSystemCpu;

        public RuntimeMetricsWriter(ISignalFxMetricSender metricSender, TimeSpan delay)
            : this(metricSender, delay, InitializeListenerFunc)
        {
        }

        internal RuntimeMetricsWriter(ISignalFxMetricSender metricSender, TimeSpan delay, Func<ISignalFxMetricSender, TimeSpan, IRuntimeMetricsListener> initializeListener)
        {
            _delay = delay;
            _metricSender = metricSender;
            _timer = new Timer(_ => PushEvents(), null, delay, delay);

            try
            {
                AppDomain.CurrentDomain.FirstChanceException += FirstChanceException;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "First chance exceptions won't be monitored");
            }

            try
            {
                ProcessHelpers.GetCurrentProcessRuntimeMetrics(out var userCpu, out var systemCpu, out _, out _, out _);

                _previousUserCpu = userCpu;
                _previousSystemCpu = systemCpu;

                _enableProcessMetrics = true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to get current process information");
                _enableProcessMetrics = false;
            }

            try
            {
                _listener = initializeListener(metricSender, delay);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to initialize runtime listener, some runtime metrics will be missing");
            }
        }

        /// <summary>
        /// Gets the internal exception counts, to be used for tests
        /// </summary>
        internal ConcurrentDictionary<string, int> ExceptionCounts => _exceptionCounts;

        public void Dispose()
        {
            AppDomain.CurrentDomain.FirstChanceException -= FirstChanceException;
            _timer.Dispose();
            _listener?.Dispose();
            _exceptionCounts.Clear();
        }

        internal void PushEvents()
        {
            try
            {
                _listener?.Refresh();

                GcMetrics.PushCollectionCounts(_metricSender);

                if (_enableProcessMetrics)
                {
                    ProcessHelpers.GetCurrentProcessRuntimeMetrics(out var newUserCpu, out var newSystemCpu, out var threadCount, out var privateMemory, out var workingSet);

                    var userCpu = newUserCpu - _previousUserCpu;
                    var systemCpu = newSystemCpu - _previousSystemCpu;

                    _previousUserCpu = newUserCpu;
                    _previousSystemCpu = newSystemCpu;

                    _metricSender.SendLong(MetricsNames.Process.ThreadsCount, threadCount, MetricType.GAUGE);

                    _metricSender.SendLong(MetricsNames.Process.MemoryUsage, workingSet, MetricType.GAUGE);
                    _metricSender.SendLong(MetricsNames.Process.MemoryVirtual, privateMemory, MetricType.GAUGE);

                    _metricSender.SendDouble(MetricsNames.Process.CpuTime, newUserCpu.TotalSeconds, MetricType.CUMULATIVE_COUNTER, CpuUserTag);
                    _metricSender.SendDouble(MetricsNames.Process.CpuTime, newSystemCpu.TotalSeconds, MetricType.CUMULATIVE_COUNTER, CpuSystemTag);

                    // Note: the behavior of Environment.ProcessorCount has changed a lot across version: https://github.com/dotnet/runtime/issues/622
                    // What we want is the number of cores attributed to the container, which is the behavior in 3.1.2+ (and, I believe, in 2.x)
                    var maximumCpu = Environment.ProcessorCount * _delay.TotalSeconds;

                    // https://github.com/open-telemetry/opentelemetry-dotnet-contrib/pull/687#discussion_r995076259
                    _metricSender.SendDouble(MetricsNames.Process.CpuUtilization, Math.Min(userCpu.TotalSeconds / maximumCpu, 1D), MetricType.GAUGE, CpuUserTag);
                    _metricSender.SendDouble(MetricsNames.Process.CpuUtilization, Math.Min(systemCpu.TotalSeconds / maximumCpu, 1D), MetricType.GAUGE, CpuSystemTag);

                    Log.Debug("Sent the following metrics: {metrics}", ProcessMetrics);
                }

                if (!_exceptionCounts.IsEmpty)
                {
                    foreach (var element in _exceptionCounts)
                    {
                        _metricSender.SendLong(MetricsNames.ExceptionsCount, element.Value, MetricType.COUNTER, new[] { $"exception_type:{element.Key}" });
                    }

                    // There's a race condition where we could clear items that haven't been pushed
                    // Having an exact exception count is probably not worth the overhead required to fix it
                    _exceptionCounts.Clear();

                    Log.Debug("Sent the following metrics: {metrics}", MetricsNames.ExceptionsCount);
                }
                else
                {
                    Log.Debug("Did not send the following metrics: {metrics}", MetricsNames.ExceptionsCount);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error while updating runtime metrics");
            }
        }

        private static IRuntimeMetricsListener InitializeListener(ISignalFxMetricSender metricSender, TimeSpan delay)
        {
#if NETCOREAPP
            return new RuntimeEventListener(metricSender, delay);
#elif NETFRAMEWORK
            return AzureAppServices.Metadata.IsRelevant ? new AzureAppServicePerformanceCounters(metricSender) : new PerformanceCountersListener(metricSender);
#else
            return null;
#endif
        }

        private void FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            var name = e.Exception.GetType().Name;

            _exceptionCounts.AddOrUpdate(name, 1, (_, count) => count + 1);
        }
    }
}
