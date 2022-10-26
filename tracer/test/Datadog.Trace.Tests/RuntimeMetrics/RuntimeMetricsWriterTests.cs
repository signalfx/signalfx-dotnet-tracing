// <copyright file="RuntimeMetricsWriterTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Threading;
using Datadog.Trace.RuntimeMetrics;
using Datadog.Trace.SignalFx.Metrics;
using Moq;
using Xunit;
using MetricType = Datadog.Tracer.SignalFx.Metrics.Protobuf.MetricType;

namespace Datadog.Trace.Tests.RuntimeMetrics
{
    [CollectionDefinition(nameof(RuntimeMetricsWriterTests), DisableParallelization = true)]
    [Collection(nameof(RuntimeMetricsWriterTests))]
    public class RuntimeMetricsWriterTests
    {
        [Fact]
        public void PushEvents()
        {
            var listener = new Mock<IRuntimeMetricsListener>();
            var mutex = new ManualResetEventSlim();

            listener.Setup(l => l.Refresh())
                .Callback(() => mutex.Set());

            using (new RuntimeMetricsWriter(Mock.Of<ISignalFxMetricSender>(), TimeSpan.FromMilliseconds(10), (_, _) => listener.Object))
            {
                Assert.True(mutex.Wait(10000), "Method Refresh() wasn't called on the listener");
            }
        }

        [Fact]
        public void PushGcMetrics()
        {
            var listener = new Mock<IRuntimeMetricsListener>();

            var metricSender = new Mock<ISignalFxMetricSender>();
            using var runtimeMetricsWriter = new RuntimeMetricsWriter(metricSender.Object, TimeSpan.FromSeconds(10), (_, _) => listener.Object);

            runtimeMetricsWriter.PushEvents();

            metricSender.Verify(s => s.SendLong(MetricsNames.Gc.CollectionsCount, It.IsAny<long>(), MetricType.CUMULATIVE_COUNTER, new[] { "generation:gen0" }), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendLong(MetricsNames.Gc.CollectionsCount, It.IsAny<long>(), MetricType.CUMULATIVE_COUNTER, new[] { "generation:gen1" }), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendLong(MetricsNames.Gc.CollectionsCount, It.IsAny<long>(), MetricType.CUMULATIVE_COUNTER, new[] { "generation:gen2" }), Times.AtLeastOnce);
        }

        [Fact]
        public void PushProcessMetrics()
        {
            var listener = new Mock<IRuntimeMetricsListener>();

            var metricSender = new Mock<ISignalFxMetricSender>();
            using var runtimeMetricsWriter = new RuntimeMetricsWriter(metricSender.Object, TimeSpan.FromSeconds(10), (_, _) => listener.Object);

            runtimeMetricsWriter.PushEvents();

            metricSender.Verify(s => s.SendDouble(MetricsNames.Process.CpuTime, It.IsAny<double>(), MetricType.CUMULATIVE_COUNTER, new[] { "state:user" }), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendDouble(MetricsNames.Process.CpuTime, It.IsAny<double>(), MetricType.CUMULATIVE_COUNTER, new[] { "state:system" }), Times.AtLeastOnce);

            metricSender.Verify(s => s.SendDouble(MetricsNames.Process.CpuUtilization, It.IsAny<double>(), MetricType.GAUGE, new[] { "state:user" }), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendDouble(MetricsNames.Process.CpuUtilization, It.IsAny<double>(), MetricType.GAUGE, new[] { "state:system" }), Times.AtLeastOnce);

            metricSender.Verify(s => s.SendLong(MetricsNames.Process.MemoryUsage, It.IsAny<long>(), MetricType.GAUGE, It.IsAny<string[]>()), Times.AtLeastOnce);
            metricSender.Verify(s => s.SendLong(MetricsNames.Process.MemoryVirtual, It.IsAny<long>(), MetricType.GAUGE, It.IsAny<string[]>()), Times.AtLeastOnce);

            metricSender.Verify(s => s.SendLong(MetricsNames.Process.ThreadsCount, It.IsAny<long>(), MetricType.GAUGE, It.IsAny<string[]>()), Times.AtLeastOnce);
        }

        [Fact]
        public void ShouldSwallowFactoryExceptions()
        {
            var writer = new RuntimeMetricsWriter(Mock.Of<ISignalFxMetricSender>(), TimeSpan.FromMilliseconds(10), (_, _) => throw new InvalidOperationException("This exception should be caught"));
            writer.Dispose();
        }

        [Fact(Skip = "Uninstable test see https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/67")]
        public void ShouldCaptureFirstChanceExceptions()
        {
            var metricSender = new Mock<ISignalFxMetricSender>();
            var listener = new Mock<IRuntimeMetricsListener>();

            using (var writer = new RuntimeMetricsWriter(metricSender.Object, TimeSpan.FromMilliseconds(Timeout.Infinite), (_, _) => listener.Object))
            {
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        throw new CustomException1();
                    }
                    catch
                    {
                        // ignored
                    }

                    if (i % 2 == 0)
                    {
                        try
                        {
                            throw new CustomException2();
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }

                metricSender.Verify(
                    s => s.SendLong(MetricsNames.ExceptionsCount, It.IsAny<int>(), MetricType.COUNTER, It.IsAny<string[]>()),
                    Times.Never);

                writer.PushEvents();

                metricSender.Verify(
                    s => s.SendLong(MetricsNames.ExceptionsCount, 10, MetricType.COUNTER, new[] { "exception_type:CustomException1" }),
                    Times.Once);

                metricSender.Verify(
                    s => s.SendLong(MetricsNames.ExceptionsCount, 5, MetricType.COUNTER, new[] { "exception_type:CustomException2" }),
                    Times.Once);

                metricSender.Invocations.Clear();

                // Make sure metricSender are reset when pushed
                writer.PushEvents();

                metricSender.Verify(
                    s => s.SendLong(MetricsNames.ExceptionsCount, It.IsAny<int>(), MetricType.COUNTER, new[] { "exception_type:CustomException1" }),
                    Times.Never);

                metricSender.Verify(
                    s => s.SendLong(MetricsNames.ExceptionsCount, It.IsAny<int>(), MetricType.COUNTER, new[] { "exception_type:CustomException2" }),
                    Times.Never);
            }
        }

        [Fact]
        public void CleanupResources()
        {
            var metricSender = new Mock<ISignalFxMetricSender>();
            var listener = new Mock<IRuntimeMetricsListener>();

            var writer = new RuntimeMetricsWriter(metricSender.Object, TimeSpan.FromMilliseconds(Timeout.Infinite), (_, _) => listener.Object);
            writer.Dispose();

            listener.Verify(l => l.Dispose(), Times.Once);

            // Make sure that the writer unsubscribed from the global exception handler
            try
            {
                throw new CustomException1();
            }
            catch
            {
                // ignored
            }

            writer.ExceptionCounts.TryGetValue(nameof(CustomException1), out var count);

            Assert.Equal(0, count);
        }

        private class CustomException1 : Exception
        {
        }

        private class CustomException2 : Exception
        {
        }
    }
}
