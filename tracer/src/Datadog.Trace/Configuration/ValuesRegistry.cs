using System;
using System.Collections.Generic;
using System.Linq;

namespace Datadog.Trace.Configuration
{
    internal static class ValuesRegistry<T>
        where T : Enum
    {
        internal static readonly string[] Names;

        internal static readonly IReadOnlyDictionary<string, int> Ids;

        static ValuesRegistry()
        {
            var values = Enum.GetValues(typeof(T));
            var ids = new Dictionary<string, int>(values.Length, StringComparer.OrdinalIgnoreCase);

            Names = new string[values.Cast<int>().Max() + 1];

            foreach (T value in values)
            {
                var name = value.ToString();
                var intValue = Convert.ToInt32(value);

                Names[intValue] = name;
                ids.Add(name, intValue);
            }

            Ids = ids;
        }

        internal static string GetName(T value)
            => Names[Convert.ToInt32(value)];

        internal static bool TryGetValue(string name, out T value)
        {
            if (Ids.TryGetValue(name, out var id))
            {
                value = (T)Enum.ToObject(typeof(T), id);
                return true;
            }

            value = default;
            return false;
        }
    }
}
