using System;
using System.Collections.Generic;

namespace Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers
{
    /// <summary>
    /// A lookup table that a custom key for each item stored on the table.
    /// </summary>
    /// <typeparam name="TEntryKey">Type of items in the base lookup table.</typeparam>
    /// <typeparam name="TEntry">Type of items in the dependent lookup table.</typeparam>
    internal sealed class CustomEntryKeyLookupTable<TEntryKey, TEntry> : List<TEntry>
    {
        private readonly Dictionary<TEntryKey, uint> _entryDictionary = new();
        private uint _index = 0;

        public uint GetOrCreateIndex(TEntryKey entryKey, Func<TEntry> createEntry)
        {
            if (_entryDictionary.TryGetValue(entryKey, out var index))
            {
                return index;
            }

            var entry = createEntry();

            Add(entry);
            _entryDictionary.Add(entryKey, _index);
            return _index++;
        }
    }
}
