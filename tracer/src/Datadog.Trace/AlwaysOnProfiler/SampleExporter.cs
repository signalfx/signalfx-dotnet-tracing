using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Resource.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

/// <summary>
/// Exports Cpu/Allocation samples, accumulating LogRecords created by provided native buffer processors.
/// </summary>
internal class SampleExporter
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(SampleExporter));

    private readonly IOtlpSender _otlpSender;
    private readonly IList<IProfileAppender> _profileAppenders;

    private readonly ProfilesData _profilesData;

    public SampleExporter(ImmutableTracerSettings tracerSettings, IOtlpSender otlpSender, IList<IProfileAppender> profileAppenders)
    {
        _otlpSender = otlpSender ?? throw new ArgumentNullException(nameof(otlpSender));
        _profileAppenders = profileAppenders ?? throw new ArgumentNullException(nameof(profileAppenders));

        // The same ProfilesData instance is used on all export messages. With the exception of the list of
        // Profiles, the Profiles property, all other fields are pre-populated.
        _profilesData = CreateProfilesData(tracerSettings);
    }

    public void Export()
    {
        var profiles = _profilesData.ResourceProfiles[0].ScopeProfiles[0].Profiles;
        try
        {
            foreach (var profileAppender in _profileAppenders)
            {
                // Accumulate results in profiles object, an alias, to _profilesData.ResourceProfiles[0].ScopeProfiles[0].Profiles
                // Allocation samples appender adds at most 1 Profile object,
                // cpu samples appender may add 2.
                profileAppender.AppendTo(profiles);
            }

            if (profiles.Exists(p => p.ProfileTypes.Count > 0))
            {
                _otlpSender.Send(_profilesData);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing samples.");
        }
        finally
        {
            // The exporter reuses the profiles object, but the individual Profile objects are not
            // needed after serialization, release the Profile objects so they can be garbage collected.
            profiles.Clear();
        }
    }

    private static ProfilesData CreateProfilesData(ImmutableTracerSettings tracerSettings)
    {
        var resource = new Resource();
        var profilingAttributes = OtelResource
                                 .GetCommonAttributes(tracerSettings, CorrelationIdentifier.Service)
                                 .Select(kv =>
                                             new KeyValue
                                             {
                                                 Key = kv.Key,
                                                 Value = new AnyValue
                                                 {
                                                     StringValue = kv.Value
                                                 }
                                             });
        resource.Attributes.AddRange(profilingAttributes);

        return new ProfilesData
        {
            ResourceProfiles =
            {
                new ResourceProfiles
                {
                    Resource = resource,
                    ScopeProfiles =
                    {
                        new ScopeProfiles
                        {
                            Scope = new InstrumentationScope
                            {
                                Name = "otlp.profiles@154f871",
                                Version = "0.0.1"
                            }
                        }
                    }
                }
            }
        };
    }
}
