// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Logging;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics
{
    internal class SignalFxMetricSender : IDisposable
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(SignalFxMetricSender));

        private readonly ISignalFxMetricWriter _writer;
        private readonly List<Dimension> _globalDimensions;

        public SignalFxMetricSender(ISignalFxMetricWriter writer, string[] globalTags)
        {
            if (globalTags == null)
            {
                throw new ArgumentNullException(nameof(globalTags));
            }

            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _globalDimensions = globalTags.Select(tag => ToDimension(tag)).ToList();
        }

        /// <summary>
        /// Sends gauge metric using configured reporter.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value of the metric.</param>
        /// <param name="tags">Tags added to the metric.</param>
        public void SendGaugeMetric(string name, double value, string[] tags = null)
        {
            Send(MetricType.GAUGE, name, datum => datum.doubleValue = value, tags);
        }

        /// <summary>
        /// Sends cumulative counter metric using configured reporter.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value of the metric.</param>
        /// <param name="tags">Tags added to the metric.</param>
        public void SendCumulativeCounterMetric(string name, long value, string[] tags = null)
        {
            Send(MetricType.CUMULATIVE_COUNTER, name, datum => datum.intValue = value, tags);
        }

        /// <summary>
        /// Sends counter metric using configured reporter.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value of the metric.</param>
        /// <param name="tags">Tags added to the metric.</param>
        public void SendCounterMetric(string name, long value, string[] tags = null)
        {
            Send(MetricType.COUNTER, name, datum => datum.intValue = value, tags);
        }

        /// <summary>
        /// Sends counter metric with a double value using configured reporter.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value of the metric.</param>
        /// <param name="tags">Tags added to the metric.</param>
        public void SendDoubleCounterMetric(string name, double value, string[] tags = null)
        {
            Send(MetricType.COUNTER, name, datum => datum.doubleValue = value, tags);
        }

        public void Dispose()
        {
            _writer?.Dispose();
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

        private void Send(MetricType metricType, string name, Action<Datum> valueSetter, string[] tags)
        {
            var dataPoint = CreateDataPoint(metricType, name, valueSetter, tags);
            if (!_writer.TryWrite(dataPoint))
            {
                Log.Warning("Metric upload failed, worker queue full.");
            }
        }

        private DataPoint CreateDataPoint(MetricType metricType, string name, Action<Datum> valueSetter, string[] tags)
        {
            // TODO splunk: consider pooling data points
            var dataPoint = new DataPoint
            {
                metricType = metricType,
                metric = name,
                value = new Datum(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            valueSetter(dataPoint.value);
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
