// <copyright file="RuntimeEventListenerTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

// Disabled on .NET Core 3.1 as we were running into this issue: https://github.com/dotnet/runtime/issues/51579

#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using Datadog.Trace.RuntimeMetrics;
using Datadog.Trace.SignalFx.Metrics;
using Moq;
using Xunit;
using MetricType = Datadog.Tracer.SignalFx.Metrics.Protobuf.MetricType;

namespace Datadog.Trace.Tests.RuntimeMetrics
{
    [CollectionDefinition(nameof(RuntimeEventListenerTests), DisableParallelization = true)]
    [Collection(nameof(RuntimeEventListenerTests))]
    public class RuntimeEventListenerTests
    {
        [Fact]
        public void PushEvents()
        {
            var metricSender = new Mock<ISignalFxMetricSender>();

            var settings = SettingsGenerator.Generate();
            using var listener = new RuntimeEventListener(settings, metricSender.Object, TimeSpan.FromSeconds(10));

            listener.Refresh();

            metricSender.Verify(s => s.SendLong(MetricsNames.NetRuntime.ContentionCount, It.IsAny<long>(), MetricType.CUMULATIVE_COUNTER, null), Times.Once);
            metricSender.Verify(s => s.SendLong(MetricsNames.NetRuntime.ThreadPoolWorkersCount, It.IsAny<long>(), MetricType.GAUGE, null), Times.Once);
        }

        [Fact]
        public void MonitorGarbageCollections()
        {
            var metricSender = new Mock<ISignalFxMetricSender>();

            var mutex = new ManualResetEventSlim();

            // TotalPauseTime is pushed on the GcRestartEnd event, which should be the last event for any GC
            metricSender.Setup(s => s.SendDouble(MetricsNames.NetRuntime.Gc.PauseTime, It.IsAny<double>(), MetricType.COUNTER, It.IsAny<string[]>()))
                .Callback(() => mutex.Set());

            var settings = SettingsGenerator.Generate();
            using var listener = new RuntimeEventListener(settings, metricSender.Object, TimeSpan.FromSeconds(10));

            metricSender.Invocations.Clear();

            mutex.Reset(); // In case a GC was triggered when creating the listener

            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

            listener.Refresh();

            if (!mutex.Wait(TimeSpan.FromSeconds(30)))
            {
                throw new TimeoutException("Timed-out waiting for pause times to be reported.");
            }

            metricSender.Verify(s => s.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, It.IsAny<long>(), MetricType.GAUGE, new[] { "generation:gen0" }), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, It.IsAny<long>(), MetricType.GAUGE, new[] { "generation:gen1" }), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, It.IsAny<long>(), MetricType.GAUGE, new[] { "generation:gen2" }), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, It.IsAny<long>(), MetricType.GAUGE, new[] { "generation:loh" }), Times.AtLeastOnce);

            metricSender.Verify(s => s.SendLong(MetricsNames.NetRuntime.Gc.AllocatedBytes, It.IsAny<long>(), MetricType.CUMULATIVE_COUNTER, It.IsAny<string[]>()), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendDouble(MetricsNames.NetRuntime.Gc.PauseTime, It.IsAny<double>(), MetricType.COUNTER, It.IsAny<string[]>()), Times.AtLeastOnce);

#if NET6_0_OR_GREATER
            metricSender.Verify(s => s.SendLong(MetricsNames.NetRuntime.Gc.HeapSize, It.IsAny<long>(), MetricType.GAUGE, new[] { "generation:poh" }), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendLong(MetricsNames.NetRuntime.Gc.HeapCommittedMemory, It.IsAny<long>(), MetricType.GAUGE, It.IsAny<string[]>()), Times.AtLeastOnce);
#endif
        }

        [Fact]
        public void PushEventCounters()
        {
            // Pretending we're aspnetcore
            var eventSource = new EventSource("Microsoft.AspNetCore.Hosting");

            var mutex = new ManualResetEventSlim();

            Func<double> callback = () =>
            {
                mutex.Set();
                return 0.0;
            };

            var counters = new List<DiagnosticCounter>
            {
                new PollingCounter("current-requests", eventSource, () => 1.0),
                new PollingCounter("failed-requests", eventSource, () => 2.0),
                new PollingCounter("total-requests", eventSource, () => 4.0),
                new PollingCounter("request-queue-length", eventSource, () => 8.0),
                new PollingCounter("connection-queue-length", eventSource, () => 16.0),
                new PollingCounter("total-connections", eventSource, () => 32.0),

                // This counter sets the mutex, so it needs to be created last
                new PollingCounter("Dummy", eventSource, callback)
            };

            var metricSender = new Mock<ISignalFxMetricSender>();
            var settings = SettingsGenerator.Generate();

            using var listener = new RuntimeEventListener(settings, metricSender.Object, TimeSpan.FromSeconds(1));

            // Wait for the counters to be refreshed
            mutex.Wait();

            metricSender.Verify(s => s.SendDouble(MetricsNames.AspNetCore.CoreCurrentRequests, 1.0, MetricType.GAUGE, null), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendDouble(MetricsNames.AspNetCore.CoreFailedRequests, 2.0, MetricType.GAUGE, null), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendDouble(MetricsNames.AspNetCore.CoreTotalRequests, 4.0, MetricType.GAUGE, null), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendDouble(MetricsNames.AspNetCore.CoreRequestQueueLength, 8.0, MetricType.GAUGE, null), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendDouble(MetricsNames.AspNetCore.CoreConnectionQueueLength, 16.0, MetricType.GAUGE, null), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendDouble(MetricsNames.AspNetCore.CoreTotalConnections, 32.0, MetricType.GAUGE, null), Times.AtLeastOnce);

            foreach (var counter in counters)
            {
                counter.Dispose();
            }
        }
    }
}
#endif
