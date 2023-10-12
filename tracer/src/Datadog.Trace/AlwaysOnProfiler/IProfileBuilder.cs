using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal interface IProfileBuilder
{
    Profile Build();
}
