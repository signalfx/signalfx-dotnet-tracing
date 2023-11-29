using System;
using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal class ProfileBuilder : IProfileBuilder
{
    private readonly IList<IProfileTypeBuilder> _profileTypeBuilders;

    public ProfileBuilder(IList<IProfileTypeBuilder> profileTypeBuilders)
    {
        _profileTypeBuilders = profileTypeBuilders;
    }

    public Profile Build()
    {
        var startTimeUnixNano = ulong.MaxValue;
        var endTimeUnixNano = ulong.MinValue;
        var profileLookupTables = new ProfileLookupTables();
        var profileTypes = new List<ProfileType>(_profileTypeBuilders.Count);

        foreach (var profileTypeBuilder in _profileTypeBuilders)
        {
            var result = profileTypeBuilder.Build(profileLookupTables);
            if (!result.ContainsData)
            {
                // No data to add.
                continue;
            }

            if (result.StartTimestamp.Nanoseconds < startTimeUnixNano)
            {
                startTimeUnixNano = result.StartTimestamp.Nanoseconds;
            }

            if (result.EndTimestamp.Nanoseconds > endTimeUnixNano)
            {
                endTimeUnixNano = result.StartTimestamp.Nanoseconds;
            }

            profileTypes.Add(result.ProfileType);
        }

        if (profileTypes.Count == 0)
        {
            return null;
        }

        var profile = new Profile
        {
            ProfileId = Guid.Empty.ToByteArray(),
            StartTimeUnixNano = startTimeUnixNano,
            EndTimeUnixNano = endTimeUnixNano
        };

        profileLookupTables.CopyLookupTablesToProfile(profile);
        profile.ProfileTypes.AddRange(profileTypes);

        return profile;
    }
}
