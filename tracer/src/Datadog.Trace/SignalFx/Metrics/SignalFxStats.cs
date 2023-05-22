using System;
using Datadog.Trace.DogStatsd;
using Datadog.Trace.Vendors.StatsdClient;
using MetricType = Datadog.Tracer.SignalFx.Metrics.Protobuf.MetricType;

namespace Datadog.Trace.SignalFx.Metrics
{
    /// <summary>
    /// Supports sending counter and gauge metrics with provided sender.
    /// Additional metric types supported by StatsD and not SignalFx backend
    /// are ignored.
    /// </summary>
    internal class SignalFxStats : IDogStatsd
    {
        private readonly ISignalFxMetricSender _metricSender;

        public SignalFxStats(ISignalFxMetricSender metricSender)
        {
            _metricSender = metricSender ?? throw new ArgumentNullException(nameof(metricSender));
            // TODO splunk: consider supporting telemetry with our metric exporter
            TelemetryCounters = new Vendors.StatsdClient.Telemetry();
        }

        public ITelemetryCounters TelemetryCounters { get; }

        public void Dispose()
        {
            // nothing to dispose
        }

        public void Configure(StatsdConfig config)
        {
            // nothing to configure
        }

        public void Counter(string statName, long value, double sampleRate = 1, string[] tags = null)
        {
            _metricSender.SendLong(statName, value, MetricType.CUMULATIVE_COUNTER, tags);
        }

        public void Decrement(string statName, int value = 1, double sampleRate = 1, params string[] tags)
        {
        }

        public void Timer(string statName, double value, double sampleRate = 1, string[] tags = null)
        {
            _metricSender.SendDouble(statName, value, MetricType.GAUGE, tags);
        }

        public void Event(string title, string text, string alertType = null, string aggregationKey = null, string sourceType = null, int? dateHappened = null, string priority = null, string hostname = null, string[] tags = null)
        {
            // noop
        }

        public void Gauge(string statName, double value, double sampleRate = 1, string[] tags = null)
        {
            _metricSender.SendDouble(statName, value, MetricType.GAUGE, tags);
        }

        public void Histogram(string statName, double value, double sampleRate = 1, string[] tags = null)
        {
            // noop
        }

        public void Distribution(string statName, double value, double sampleRate = 1, string[] tags = null)
        {
            // noop
        }

        public void Increment(string statName, int value = 1, double sampleRate = 1, string[] tags = null)
        {
            _metricSender.SendLong(statName, value, MetricType.COUNTER, tags);
        }

        public void Set<T>(string statName, T value, double sampleRate = 1, string[] tags = null)
        {
            // noop
        }

        public void Set(string statName, string value, double sampleRate = 1, string[] tags = null)
        {
            // noop
        }

        public IDisposable StartTimer(string name, double sampleRate = 1, string[] tags = null)
        {
            return NoOpTimer.Instance;
        }

        public void Time(Action action, string statName, double sampleRate = 1, string[] tags = null)
        {
            // noop
        }

        public T Time<T>(Func<T> func, string statName, double sampleRate = 1, string[] tags = null)
        {
            return func();
        }

        public void ServiceCheck(string name, Status status, int? timestamp = null, string hostname = null, string[] tags = null, string message = null)
        {
            // noop
        }
    }
}
