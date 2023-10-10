using System.Collections.Generic;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal interface IProfileAppender
{
    /// <summary>
    /// Processes samples captured on the native side and
    /// appends profiles to the provided list.
    /// </summary>
    /// <param name="profiles">The collection to add records to.</param>
    void AppendTo(List<Profile> profiles);

    // AppendProfileTypeResult AppendProfileType(List<ProfileType> profileTypes);
}

// internal readonly record struct AppendProfileTypeResult(
//     bool AppendedData,
//     ThreadSample.Time StartTimestamp,
//     ThreadSample.Time EndTimestamp);
