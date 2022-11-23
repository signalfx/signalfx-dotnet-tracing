using System;
using System.Collections.Generic;
using System.Linq;

namespace Datadog.Trace.Configuration;

internal static class MetricsIntegrationRegistry
{
    internal static readonly string[] Names;

    internal static readonly IReadOnlyDictionary<string, int> Ids;

    static MetricsIntegrationRegistry()
    {
        var values = Enum.GetValues(typeof(MetricsIntegrationId));
        var ids = new Dictionary<string, int>(values.Length, StringComparer.OrdinalIgnoreCase);

        Names = new string[values.Cast<int>().Max() + 1];

        foreach (MetricsIntegrationId value in values)
        {
            var name = value.ToString();

            Names[(int)value] = name;
            ids.Add(name, (int)value);
        }

        Ids = ids;
    }

    internal static string GetName(MetricsIntegrationId integration)
        => Names[(int)integration];

    internal static bool TryGetIntegrationId(string integrationName, out MetricsIntegrationId integration)
    {
        if (Ids.TryGetValue(integrationName, out var id))
        {
            integration = (MetricsIntegrationId)id;
            return true;
        }

        integration = default;
        return false;
    }
}
