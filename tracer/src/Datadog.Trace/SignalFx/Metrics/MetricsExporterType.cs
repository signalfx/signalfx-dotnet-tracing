namespace Datadog.Trace.SignalFx.Metrics
{
    /// <summary>
    /// Enumeration for the available metric exporter types.
    /// </summary>
    public enum MetricsExporterType
    {
        /// <summary>
        /// The default exporter.
        /// </summary>
        Default,

        /// <summary>
        /// StatsD based exporter.
        /// </summary>
        StatsD,

        /// <summary>
        /// SignalFx exporter.
        /// </summary>
        SignalFx = Default
    }
}
