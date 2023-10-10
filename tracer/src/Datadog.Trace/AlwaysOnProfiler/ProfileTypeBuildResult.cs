using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal class ProfileTypeBuildResult
{
    public bool ContainsData { get; set; }

    public ThreadSample.Time StartTimestamp { get; set; }

    public ThreadSample.Time EndTimestamp { get; set; }

    public ProfileType ProfileType { get; set; }
}
