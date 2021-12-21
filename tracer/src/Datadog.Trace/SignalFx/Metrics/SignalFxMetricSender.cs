using System;
using System.Linq;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics
{
    internal class SignalFxMetricSender
    {
        private readonly ISignalFxReporter _reporter;
        private readonly string[] _globalTags;

        public SignalFxMetricSender(ISignalFxReporter reporter, string[] globalTags)
        {
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
            _globalTags = globalTags ?? throw new ArgumentNullException(nameof(globalTags));
        }

        /// <summary>
        /// Sends gauge metric using configured reporter.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value of the metric.</param>
        /// <param name="tags">Tags added to the metric.</param>
        public void SendGaugeMetric(string name, double value, string[] tags = null)
        {
            Send(MetricType.GAUGE, name, value, tags);
        }

        /// <summary>
        /// Sends counter metric using configured reporter.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value of the metric.</param>
        /// <param name="tags">Tags added to the metric.</param>
        public void SendCounterMetric(string name, double value, string[] tags = null)
        {
            Send(MetricType.COUNTER, name, value, tags);
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

        private void Send(MetricType metricType, string name, double value, string[] tags)
        {
            var message = CreateUploadMessage(metricType, name, value, tags);
            _reporter.Send(message);
        }

        private DataPointUploadMessage CreateUploadMessage(MetricType metricType, string name, double value, string[] tags)
        {
            var dataPoint = new DataPoint
            {
                metricType = metricType,
                metric = name,
                value = new Datum { doubleValue = value }
            };
            var requestedTags = tags ?? Array.Empty<string>();
            var finalTags = requestedTags.Concat(_globalTags);
            dataPoint.dimensions.AddRange(finalTags.Select(t => ToDimension(t)));

            var message = new DataPointUploadMessage();
            message.datapoints.Add(dataPoint);
            return message;
        }
    }
}
