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
using Datadog.Trace.Vendors.StatsdClient;
using Moq;
using Xunit;

namespace Datadog.Trace.Tests.RuntimeMetrics
{
    [CollectionDefinition(nameof(RuntimeEventListenerTests), DisableParallelization = true)]
    [Collection(nameof(RuntimeEventListenerTests))]
    public class RuntimeEventListenerTests
    {
        [Fact]
        public void PushEvents()
        {
            var statsd = new Mock<IDogStatsd>();

            using var listener = new RuntimeEventListener(statsd.Object, TimeSpan.FromSeconds(10));

            listener.Refresh();

            statsd.Verify(s => s.Gauge(MetricsNames.ContentionTime, It.IsAny<double>(), 1, null), Times.Once);
            statsd.Verify(s => s.Counter(MetricsNames.ContentionCount, It.IsAny<long>(), 1, null), Times.Once);
            statsd.Verify(s => s.Gauge(MetricsNames.ThreadPoolWorkersCount, It.IsAny<double>(), 1, null), Times.Once);
        }

        [Fact]
        public void MonitorGarbageCollections()
        {
            var statsd = new Mock<IDogStatsd>();

            // number of reported heap sizes depends on the version of the runtime
            var expectedCount = Environment.Version.Major >= 6 ? 5 : 4;

            // needed for runtime version < net6
            var countdownEvent = new CountdownEvent(expectedCount);

            statsd.Setup(s => s.Gauge(MetricsNames.Gc.HeapSize, It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string[]>()))
                .Callback(() => countdownEvent.Signal());

            using var listener = new RuntimeEventListener(statsd.Object, TimeSpan.FromSeconds(10));

            statsd.Invocations.Clear();

            countdownEvent.Reset(); // In case a GC was triggered when creating the listener

            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

            // refresh for collection counts metrics to be pushed
            listener.Refresh();

            // GC events are pushed asynchronously for runtime version < net6, wait for the last one to be processed
            if (!countdownEvent.Wait(TimeSpan.FromSeconds(30)))
            {
                throw new TimeoutException("Timed-out waiting for heap sizes to be reported.");
            }

            statsd.Verify(s => s.Gauge(MetricsNames.Gc.HeapSize, It.IsAny<double>(), It.IsAny<double>(), new[] { "generation:gen0" }), Times.AtLeastOnce);
            statsd.Verify(s => s.Gauge(MetricsNames.Gc.HeapSize, It.IsAny<double>(), It.IsAny<double>(), new[] { "generation:gen1" }), Times.AtLeastOnce);
            statsd.Verify(s => s.Gauge(MetricsNames.Gc.HeapSize, It.IsAny<double>(), It.IsAny<double>(), new[] { "generation:gen2" }), Times.AtLeastOnce);
            statsd.Verify(s => s.Gauge(MetricsNames.Gc.HeapSize, It.IsAny<double>(), It.IsAny<double>(), new[] { "generation:loh" }), Times.AtLeastOnce);

            statsd.Verify(s => s.Counter(MetricsNames.Gc.AllocatedBytes, It.IsAny<long>(), It.IsAny<double>(), It.IsAny<string[]>()), Times.AtLeastOnce);

            statsd.Verify(s => s.Counter(MetricsNames.Gc.CollectionsCount, It.IsAny<long>(), It.IsAny<double>(), new[] { "generation:gen0" }), Times.AtLeastOnce);
            statsd.Verify(s => s.Counter(MetricsNames.Gc.CollectionsCount, It.IsAny<long>(), It.IsAny<double>(), new[] { "generation:gen1" }), Times.AtLeastOnce);
            statsd.Verify(s => s.Counter(MetricsNames.Gc.CollectionsCount, It.IsAny<long>(), It.IsAny<double>(), new[] { "generation:gen2" }), Times.AtLeastOnce);

#if NET6_0_OR_GREATER
            statsd.Verify(s => s.Gauge(MetricsNames.Gc.HeapCommittedMemory, It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string[]>()), Times.AtLeastOnce);
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

            var statsd = new Mock<IDogStatsd>();
            using var listener = new RuntimeEventListener(statsd.Object, TimeSpan.FromSeconds(1));

            // Wait for the counters to be refreshed
            mutex.Wait();

            statsd.Verify(s => s.Gauge(MetricsNames.AspNetCoreCurrentRequests, 1.0, 1, null), Times.AtLeastOnce);
            statsd.Verify(s => s.Gauge(MetricsNames.AspNetCoreFailedRequests, 2.0, 1, null), Times.AtLeastOnce);
            statsd.Verify(s => s.Gauge(MetricsNames.AspNetCoreTotalRequests, 4.0, 1, null), Times.AtLeastOnce);
            statsd.Verify(s => s.Gauge(MetricsNames.AspNetCoreRequestQueueLength, 8.0, 1, null), Times.AtLeastOnce);
            statsd.Verify(s => s.Gauge(MetricsNames.AspNetCoreConnectionQueueLength, 16.0, 1, null), Times.AtLeastOnce);
            statsd.Verify(s => s.Gauge(MetricsNames.AspNetCoreTotalConnections, 32.0, 1, null), Times.AtLeastOnce);

            foreach (var counter in counters)
            {
                counter.Dispose();
            }
        }
    }
}
#endif
