// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Logging;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics
{
    internal class SignalFxMetricSender : ISignalFxMetricSender
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(SignalFxMetricSender));
        private static readonly Func<ISignalFxMetricExporter, int, ISignalFxMetricWriter> InitializeListenerFunc = InitializeWriter;

        private readonly ISignalFxMetricWriter _writer;
        private readonly List<Dimension> _globalDimensions;

        public SignalFxMetricSender(string[] globalTags, ISignalFxMetricExporter exporter, int maxQueueSize)
            : this(globalTags, exporter, maxQueueSize, InitializeListenerFunc)
        {
        }

        public SignalFxMetricSender(string[] globalTags, ISignalFxMetricExporter exporter, int maxQueueSize, Func<ISignalFxMetricExporter, int, ISignalFxMetricWriter> initializeWriter)
        {
            if (globalTags == null)
            {
                throw new ArgumentNullException(nameof(globalTags));
            }

            _writer = initializeWriter(exporter, maxQueueSize);
            _globalDimensions = globalTags.Select(tag => ToDimension(tag)).ToList();
        }

        public void Dispose()
        {
            _writer?.Dispose();
        }

        public void SendDouble(string name, double value, MetricType metricType, string[] tags)
        {
            var dataPoint = CreateDataPoint(metricType, name, tags);
            dataPoint.value.doubleValue = value;
            Write(dataPoint);
        }

        public void SendLong(string name, long value, MetricType metricType, string[] tags)
        {
            var dataPoint = CreateDataPoint(metricType, name, tags);
            dataPoint.value.intValue = value;
            Write(dataPoint);
        }

        private static ISignalFxMetricWriter InitializeWriter(ISignalFxMetricExporter exporter, int maxQueueSize)
        {
            return new AsyncSignalFxMetricWriter(exporter, maxQueueSize);
        }

        private static Dimension ToDimension(string t)
        {
            var kv = t.Split(separator: ':');
            return new Dimension
            {
                key = kv[0],
                value = kv[1]
            };
        }

        private void Write(DataPoint dataPoint)
        {
            if (!_writer.TryWrite(dataPoint))
            {
                Log.Warning("Metric upload failed, worker queue full.");
            }
        }

        private DataPoint CreateDataPoint(MetricType metricType, string name, string[] tags)
        {
            // TODO splunk: consider pooling data points
            var dataPoint = new DataPoint
            {
                metricType = metricType,
                metric = name,
                value = new Datum(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            dataPoint.dimensions.AddRange(_globalDimensions);

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    dataPoint.dimensions.Add(ToDimension(tag));
                }
            }

            return dataPoint;
        }
    }
}
