using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;

namespace Datadog.Trace.AlwaysOnProfiler;

internal interface IProfileTypeBuilder
{
    ProfileTypeBuildResult Build(ProfileLookupTables profileLookupTables);
}
