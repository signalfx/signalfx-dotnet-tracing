using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics
{
    /// <summary>
    /// Exports metric in SignalFx proto format.
    /// </summary>
    internal interface ISignalFxMetricExporter
    {
        void Send(DataPointUploadMessage msg);
    }
}
