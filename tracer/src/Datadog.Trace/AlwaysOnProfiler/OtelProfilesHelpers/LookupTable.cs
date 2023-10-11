using System.Collections.Generic;

namespace Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers
{
    /// <summary>
    /// Simple class used to build a sequence of unique items that can be indexed by integers and
    /// easily passed to protobuf types.
    /// </summary>
    /// <typeparam name="T">Type of items in the indexed table</typeparam>
    internal sealed class LookupTable<T> : List<T>
    {
        private readonly Dictionary<T, uint> _entriesDictionary = new();
        private uint _index = 0;

        public uint GetOrCreateIndex(T entry)
        {
            if (_entriesDictionary.TryGetValue(entry, out var index))
            {
                return index;
            }

            Add(entry);
            _entriesDictionary.Add(entry, _index);
            return _index++;
        }
    }
}
