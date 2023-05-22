using System;
using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics;

internal interface ISignalFxMetricSender : IDisposable
{
    /// <summary>
    /// Sends double-valued metric.
    /// </summary>
    /// <param name="name">Name of the metric.</param>
    /// <param name="value">Value of the metric.</param>
    /// <param name="metricType">Metric type from signalfx proto.</param>
    /// <param name="tags">Additional tags added to the metric.</param>
    void SendDouble(string name, double value, MetricType metricType, string[] tags = null);

    /// <summary>
    /// Sends long-valued metric.
    /// </summary>
    /// <param name="name">Name of the metric.</param>
    /// <param name="value">Value of the metric.</param>
    /// <param name="metricType">Metric type from signalfx proto.</param>
    /// <param name="tags">Additional tags added to the metric.</param>
    void SendLong(string name, long value, MetricType metricType, string[] tags = null);
}
