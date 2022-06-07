// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using Datadog.Trace.AlwaysOnProfiler.Builder;
using Datadog.Tracer.Pprof.Proto.Profile.V1;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class Pprof
    {
        private readonly StringTable _stringTable;
        private readonly FunctionTable _functionTable;
        private readonly LocationTable _locationTable;

        public Pprof()
        {
            ProfileBuilder = new ProfileBuilder();
            _stringTable = new StringTable(ProfileBuilder);
            _functionTable = new FunctionTable(ProfileBuilder, _stringTable);
            _locationTable = new LocationTable(ProfileBuilder, _functionTable);
        }

        public ProfileBuilder ProfileBuilder { get; }

        public long GetStringId(string str) => _stringTable.Get(str);

        public ulong GetLocationId(string file, string function, long line) => _locationTable.Get(file, function, line);

        public void AddLabel(SampleBuilder sample, string name, string value)
        {
            if (value == null)
            {
                return;
            }

            AddLabel(sample, name, label => label.SetStr(_stringTable.Get(value)));
        }

        public void AddLabel(SampleBuilder sample, string key, bool value)
        {
            AddLabel(sample, key, value.ToString());
        }

        public void AddLabel(SampleBuilder sample, string name, long value)
        {
            AddLabel(sample, name, label => label.SetNum(value));
        }

        private void AddLabel(SampleBuilder sample, string name, Action<LabelBuilder> setLabel)
        {
            var labelBuilder = new LabelBuilder();
            labelBuilder.SetKey(_stringTable.Get(name));
            setLabel(labelBuilder);
            sample.AddLabel(labelBuilder.Build());
        }

        private class StringTable
        {
            private readonly ProfileBuilder _profileBuilder;
            private readonly Dictionary<string, long> _table = new();
            private long _index;

            public StringTable(ProfileBuilder profileBuilder)
            {
                _profileBuilder = profileBuilder;
                Get(string.Empty); // 0 is reserved for the empty string
            }

            public long Get(string str)
            {
                if (_table.ContainsKey(str))
                {
                    return _table[str];
                }

                _profileBuilder.AddStringTable(str);
                _table[str] = _index;
                return _index++;
            }
        }

        private class FunctionKey
        {
            public FunctionKey(string file, string function)
            {
                File = file;
                Function = function;
            }

            public string File { get; }

            public string Function { get; }

            public override bool Equals(object obj)
            {
                return Equals(obj as FunctionKey);
            }

            public bool Equals(FunctionKey other)
            {
                return other != null &&
                       File == other.File &&
                       Function == other.Function;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(File, Function);
            }
        }

        private class FunctionTable
        {
            private readonly ProfileBuilder _profileBuilder;
            private readonly StringTable _stringTable;
            private readonly Dictionary<FunctionKey, ulong> _table = new();
            private ulong _index = 1; // 0 is reserved

            public FunctionTable(ProfileBuilder profile, StringTable stringTable)
            {
                _profileBuilder = profile;
                _stringTable = stringTable;
            }

            public ulong Get(FunctionKey functionKey)
            {
                if (_table.ContainsKey(functionKey))
                {
                    return _table[functionKey];
                }

                var function = new Function { Id = _index, Filename = _stringTable.Get(functionKey.File), Name = _stringTable.Get(functionKey.Function) };

                _profileBuilder.AddFunction(function);
                _table[functionKey] = _index;
                return _index++;
            }
        }

        private class LocationKey
        {
            private readonly long _line;

            public LocationKey(FunctionKey functionKey, long line)
            {
                FunctionKey = functionKey;
                _line = line;
            }

            public FunctionKey FunctionKey { get; }

            public override bool Equals(object obj)
            {
                return Equals(obj as LocationKey);
            }

            public bool Equals(LocationKey other)
            {
                return other != null &&
                       FunctionKey.Equals(other.FunctionKey) &&
                       _line == other._line;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(FunctionKey, _line);
            }
        }

        private class LocationTable
        {
            private readonly ProfileBuilder _profileBuilder;
            private readonly FunctionTable _functionTable;
            private readonly Dictionary<LocationKey, ulong> _table = new();
            private ulong _index = 1; // 0 is reserved

            public LocationTable(Builder.ProfileBuilder profileBuilder, FunctionTable functionTable)
            {
                _profileBuilder = profileBuilder;
                _functionTable = functionTable;
            }

            public ulong Get(string file, string function, long line)
            {
                var functionKey = new FunctionKey(file, function);
                var locationKey = new LocationKey(functionKey, line);

                if (_table.ContainsKey(locationKey))
                {
                    return _table[locationKey];
                }

                var location = new LocationBuilder { Id = _index }
                              .AddLine(new Line { FunctionId = _functionTable.Get(functionKey), line = line })
                              .Build();

                _profileBuilder.AddLocation(location);
                _table[locationKey] = _index;
                return _index++;
            }
        }
    }
}
