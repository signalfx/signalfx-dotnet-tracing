using System.Collections.Generic;
using Datadog.Trace.Configuration;
using Datadog.Trace.PlatformHelpers;
using Datadog.Trace.Util;

namespace Datadog.Trace.AlwaysOnProfiler;

internal static class OtelResource
{
    /// <summary>
    /// Returns attributes commonly added by Otel-based instrumentations to Otel resource.
    /// </summary>
    public static IList<KeyValuePair<string, string>> GetCommonAttributes(ImmutableTracerSettings tracerSettings, string serviceName)
    {
        var attributes = new List<KeyValuePair<string, string>>
        {
            new(CorrelationIdentifier.EnvKey, tracerSettings.Environment),
            new(CorrelationIdentifier.ServiceKey, serviceName),
            new("telemetry.sdk.name", "signalfx-" + TracerConstants.Library),
            new("telemetry.sdk.language", TracerConstants.Language),
            new("telemetry.sdk.version", TracerConstants.AssemblyVersion),
            new("splunk.distro.version", TracerConstants.AssemblyVersion)
        };

        attributes.AddRange(tracerSettings.GlobalTags);

        // TODO Splunk: ensure works if cgroupv2 is used
        var containerId = ContainerMetadata.GetContainerId();
        if (containerId != null)
        {
            attributes.Add(new KeyValuePair<string, string>("container.id", containerId));
        }

        var hostName = HostMetadata.Instance.Hostname;
        if (hostName != null)
        {
            attributes.Add(new KeyValuePair<string, string>("host.name", hostName));
        }

        // theoretically can return -1 for partial trust callers on .NET Framework
        var processId = DomainMetadata.Instance.ProcessId;

        attributes.Add(new KeyValuePair<string, string>("process.pid", processId.ToString()));
        return attributes;
    }
}
