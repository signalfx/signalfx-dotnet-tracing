using Datadog.Tracer.SignalFx.Metrics.Protobuf;

namespace Datadog.Trace.SignalFx.Metrics
{
    /// <summary>
    /// Sends message to metrics endpoint.
    /// </summary>
    internal interface ISignalFxReporter
    {
        void Send(DataPointUploadMessage msg);
    }
}
