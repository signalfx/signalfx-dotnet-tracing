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
    private readonly ProfileBuilder _profileBuilder;

    private readonly ProfilesData _profilesData;

    public SampleExporter(ImmutableTracerSettings tracerSettings, IOtlpSender otlpSender, ProfileBuilder profileBuilder)
    {
        _otlpSender = otlpSender ?? throw new ArgumentNullException(nameof(otlpSender));
        _profileBuilder = profileBuilder ?? throw new ArgumentNullException(nameof(profileBuilder));

        // The same ProfilesData instance is used on all export messages. With the exception of the list of
        // Profiles, the Profiles property, all other fields are pre-populated.
        _profilesData = CreateProfilesData(tracerSettings);
    }

    public void Export()
    {
        var profiles = _profilesData.ResourceProfiles[0].ScopeProfiles[0].Profiles;
        try
        {
            var profile = _profileBuilder.Build();
            if (profile != null && profile.ProfileTypes.Count > 0)
            {
                profiles.Add(profile);
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
