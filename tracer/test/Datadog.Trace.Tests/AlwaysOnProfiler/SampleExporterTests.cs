using System;
using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler;
using Datadog.Trace.Configuration;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler;

public class SampleExporterTests
{
    [Fact]
    public void Profile_data_is_reused_between_export_attempts()
    {
        var testProfileBuilder = new TestProfileBuilder();
        var testSender = new TestSender();
        var exporter = new SampleExporter(DefaultSettings(), testSender, testProfileBuilder);

        ProfilesData fstProfilesData, sndProfilesData;

        exporter.Export();

        using (new AssertionScope())
        {
            testSender.SentProfilesData.Should().HaveCount(1);

            fstProfilesData = testSender.SentProfilesData[0];

            fstProfilesData.ResourceProfiles.Should().HaveCount(1);
            fstProfilesData.ResourceProfiles[0].ScopeProfiles.Should().HaveCount(1);

            testSender.AllSentProfiles.Should().HaveCount(1);
            var profile = testSender.AllSentProfiles[0];
            profile.ProfileId.Should().BeEquivalentTo(Guid.Empty.ToByteArray());
            profile.StartTimeUnixNano.Should().Be(0);
        }

        exporter.Export();

        using (new AssertionScope())
        {
            testSender.SentProfilesData.Should().HaveCount(2);

            sndProfilesData = testSender.SentProfilesData[1];

            sndProfilesData.ResourceProfiles.Should().HaveCount(1);
            sndProfilesData.ResourceProfiles[0].ScopeProfiles.Should().HaveCount(1);

            testSender.AllSentProfiles.Should().HaveCount(2);
            var profile = testSender.AllSentProfiles[1];
            profile.ProfileId.Should().BeEquivalentTo(Guid.Empty.ToByteArray());
            profile.StartTimeUnixNano.Should().Be(1);
        }

        fstProfilesData.ResourceProfiles.Should().BeSameAs(sndProfilesData.ResourceProfiles);
    }

    private static ImmutableTracerSettings DefaultSettings()
    {
        return new ImmutableTracerSettings(new TracerSettings
        {
            Environment = "test",
            GlobalTags = new Dictionary<string, string>()
        });
    }

    private class TestSender : IOtlpSender
    {
        public List<ProfilesData> SentProfilesData { get; } = new();

        public List<Profile> AllSentProfiles { get; } = new();

        public void Send(ProfilesData profilesData)
        {
            SentProfilesData.Add(profilesData);

            // Capture all profiles that were sent.
            profilesData.ResourceProfiles.ForEach(
                resourceProfile => resourceProfile.ScopeProfiles.ForEach(
                    scpProfile => AllSentProfiles.AddRange(scpProfile.Profiles)));
        }
    }

    private class TestProfileBuilder : IProfileBuilder
    {
        private ulong _buildCallsCount = 0;

        public Profile Build()
        {
            var profile = new Profile
            {
                ProfileId = Guid.Empty.ToByteArray(),
                StartTimeUnixNano = _buildCallsCount++
            };

            // Exporter only exports if there is at least one profile type.
            profile.ProfileTypes.Add(new ProfileType());

            return profile;
        }
    }
}
