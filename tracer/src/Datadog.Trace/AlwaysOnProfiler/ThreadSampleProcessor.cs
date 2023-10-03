// Modified by Splunk Inc.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Datadog.Trace.Configuration;
using Datadog.Trace.Vendors.Newtonsoft.Json.Utilities;
using Datadog.Trace.Vendors.ProtoBuf;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;
using OtelProfile = Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1.Profile;
using PprofProfile = Datadog.Tracer.Pprof.Proto.Profile.Profile;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal class ThreadSampleProcessor
    {
        private readonly TimeSpan _threadSamplingPeriod;

        public ThreadSampleProcessor(ImmutableTracerSettings tracerSettings)
        {
            _threadSamplingPeriod = tracerSettings.ThreadSamplingPeriod;
        }

        public OtelProfile ProcessThreadSamples(List<ThreadSample> threadSamples)
        {
            if (threadSamples == null || threadSamples.Count < 1)
            {
                return null;
            }

            var stringTable = new LookupTable<string>();
            stringTable.GetOrCreateIndex(string.Empty);
            var keyValue = new KeyValue { Key = "test", Value = new AnyValue { IntValue = 1 } };
            var stackTraceIndicesTable = new LookupTable<Indices>();
            var stackTraceTable = new DependentLookupTable<Indices, Stacktrace>(stackTraceIndicesTable);

            var functionTable = new DependentLookupTable<string, Function>(stringTable);
            var locationTable = new DependentLookupTable<string, Location>(stringTable);

            var startTimeUnixNano = threadSamples[0].Timestamp.Nanoseconds;
            var endTimeUnixNano = threadSamples[0].Timestamp.Nanoseconds;
            
            // Arrays to create ProfileType after all samples are processed.
            var samplesCount = threadSamples.Count;
            var stackTracesIndices = new uint[samplesCount];
            var timestamps = new ulong[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                var threadSample = threadSamples[0];

                // Check start and end times
                var sampleTimeUnixNano = threadSample.Timestamp.Nanoseconds;
                if (startTimeUnixNano > sampleTimeUnixNano)
                {
                    startTimeUnixNano = sampleTimeUnixNano;
                }
                else if (endTimeUnixNano < sampleTimeUnixNano)
                {
                    endTimeUnixNano = sampleTimeUnixNano;
                }

                // Process thread information
                var threadNameIndex = stringTable.GetOrCreateIndex(threadSample.ThreadName);

                var stackTraceLocations = new List<uint>(threadSample.Frames.Count);
                foreach (var functionName in threadSample.Frames)
                {
                    var functionIndex = functionTable.GetOrCreateIndex(
                        functionName,
                        () => new Function
                        {
                            NameIndex = stringTable.GetOrCreateIndex(functionName),
                            FilenameIndex = stringTable.GetOrCreateIndex("unknown")
                        });

                    var locationIndex = locationTable.GetOrCreateIndex(
                        functionName,
                        () =>
                        {
                            var location = new Location();
                            location.Lines.Add(new Line { FunctionIndex = functionIndex });
                            return location;
                        });

                    stackTraceLocations.Add(locationIndex);
                }
                
                var stackTraceIndex = stackTraceTable.GetOrCreateIndex(
                    new Indices(stackTraceLocations),
                    () =>
                    {
                        var stackTrace = new Stacktrace();
                        stackTrace.LocationIndices.AddRange(stackTraceLocations);
                        return stackTrace;
                    });
                stackTracesIndices[i] = stackTraceIndex;
                timestamps[i] = sampleTimeUnixNano;
            }

            var cpuProfileType = new ProfileType
            {
                StacktraceIndices = stackTracesIndices,
                Timestamps = timestamps
            };

            var profile = new OtelProfile
            {
                StartTimeUnixNano = startTimeUnixNano,
                EndTimeUnixNano = endTimeUnixNano
            };

            // Lookup tables that require default element on first entry
            profile.Links.Add(default);
            profile.Attributes.Add(d);
            // Set the lookup tables
            profile.StringTables.AddRange(stringTable);
            profile.Stacktraces.AddRange(stackTraceTable);
            profile.Functions.AddRange(functionTable);
            profile.Locations.AddRange(locationTable);

            profile.ProfileTypes.Add(cpuProfileType);

            return profile;
        }

        public OtelProfile ProcessAllocationSamples(List<AllocationSample> allocationSamples)
        {
            if (allocationSamples == null || allocationSamples.Count < 1)
            {
                return null;
            }

            // var allocationProfile = BuildPProfForAllocationProfile(allocationSamples);
            // var totalFrameCount = CountFrames(allocationSamples);

            // OTLP_PROFILES: TODO: Add actual data.
            return new OtelProfile();
        }

        private static int CountFrames(List<AllocationSample> samples)
        {
            var sum = 0;
            for (var i = 0; i < samples.Count; i++)
            {
                sum += samples[i].ThreadSample.Frames.Count;
            }

            return sum;
        }

        private static int CountFrames(List<ThreadSample> samples)
        {
            var sum = 0;
            for (var i = 0; i < samples.Count; i++)
            {
                sum += samples[i].Frames.Count;
            }

            return sum;
        }

        private static SampleBuilder CreatePprofProfileBuilder(Pprof pprof, ThreadSample threadSample)
        {
            var sampleBuilder = new SampleBuilder();

            pprof.AddLabel(sampleBuilder, "source.event.time", threadSample.Timestamp.Milliseconds);

            if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
            {
                pprof.AddLabel(sampleBuilder, "span_id", threadSample.SpanId.ToString("x16"));
                pprof.AddLabel(sampleBuilder, "trace_id", TraceIdHelper.ToString(threadSample.TraceIdHigh, threadSample.TraceIdLow));
            }

            for (var index = 0; index < threadSample.Frames.Count; index++)
            {
                var methodName = threadSample.Frames[index];
                sampleBuilder.AddLocationId(pprof.GetLocationId(methodName));
            }

            pprof.AddLabel(sampleBuilder, "thread.id", threadSample.ManagedId);
            pprof.AddLabel(sampleBuilder, "thread.name", threadSample.ThreadName);
            return sampleBuilder;
        }

        private static PprofProfile BuildPProfForAllocationProfile(List<AllocationSample> allocationSamples)
        {
            var pprof = new Pprof();
            for (var index = 0; index < allocationSamples.Count; index++)
            {
                var allocationSample = allocationSamples[index];
                var profileBuilder = CreatePprofProfileBuilder(pprof, allocationSample.ThreadSample);

                profileBuilder.SetValue(allocationSample.AllocationSizeBytes);
                pprof.Profile.Samples.Add(profileBuilder.Build());
            }

            return pprof.Profile;
        }

        private static OtelProfile BuildOtelProfileForAllocationProfile(List<AllocationSample> allocationSamples)
        {
            var startTimeUnixNano = ulong.MaxValue;
            var endTimeUnixNano = ulong.MinValue;
            var profile = new OtelProfile();

            for (var index = 0; index < allocationSamples.Count; index++)
            {
                var allocationSample = allocationSamples[index];
                var threadSample = allocationSample.ThreadSample;
                if (threadSample.Timestamp.Nanoseconds < startTimeUnixNano)
                {
                    startTimeUnixNano = threadSample.Timestamp.Nanoseconds;
                }

                if (threadSample.Timestamp.Nanoseconds > endTimeUnixNano)
                {
                    endTimeUnixNano = threadSample.Timestamp.Nanoseconds;
                }

                var stackTrace = new Stacktrace();
                if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
                {
                    profile.
                    pprof.AddLabel(sampleBuilder, "span_id", threadSample.SpanId.ToString("x16"));
                    pprof.AddLabel(sampleBuilder, "trace_id", TraceIdHelper.ToString(threadSample.TraceIdHigh, threadSample.TraceIdLow));
                }

                for (var index = 0; index < threadSample.Frames.Count; index++)
                {
                    var methodName = threadSample.Frames[index];
                    sampleBuilder.AddLocationId(pprof.GetLocationId(methodName));
                }

                pprof.AddLabel(sampleBuilder, "thread.id", threadSample.ManagedId);
                pprof.AddLabel(sampleBuilder, "thread.name", threadSample.ThreadName);
                return sampleBuilder;

                profileBuilder.SetValue(allocationSample.AllocationSizeBytes);
                pprof.Profile.Samples.Add(profileBuilder.Build());
            }

            return profile;
        }

        private PprofProfile BuildPprofCpuProfile(List<ThreadSample> threadSamples)
        {
            var pprof = new Pprof();
            for (var index = 0; index < threadSamples.Count; index++)
            {
                var threadSample = threadSamples[index];
                var sampleBuilder = CreatePprofProfileBuilder(pprof, threadSample);

                pprof.AddLabel(sampleBuilder, "source.event.period", (long)_threadSamplingPeriod.TotalMilliseconds);
                pprof.Profile.Samples.Add(sampleBuilder.Build());
            }

            return pprof.Profile;
        }

        private class Indices : IEnumerable<uint>, IEquatable<Indices>
        {
            private readonly uint[] _indices;
            private readonly int _hashCode;

            public Indices(IList<uint> indices)
            {
                _indices = new uint[indices.Count];

                // Since they are going to be in a Lookup go ahead and already calculate the hash code.
                // Nothing fancy here, just adding a hash function that takes into account the indices contents.
                var hashCode = indices.Count;
                for (var i = 0; i < indices.Count; i++)
                {
                    var index = indices[i];
                    hashCode = (int)unchecked((hashCode * 314159) + index);
                }

                _hashCode = hashCode;
            }

            public override bool Equals(object obj) => obj is Indices anotherIndices && Equals(anotherIndices);

            public bool Equals(Indices other)
            {
                if (_hashCode != other?._hashCode)
                {
                    return false;
                }

                for (var i = 0; i < _indices.Length; i++)
                {
                    if (_indices[i] != other._indices[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode() => _hashCode;

            public IEnumerator<uint> GetEnumerator() => ((IEnumerable<uint>)_indices).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _indices.GetEnumerator();
        }

        private class LookupTable<T> : List<T>
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

        private class DependentLookupTable<TEntryKey, TEntry> : List<TEntry>
        {
            private readonly Dictionary<TEntryKey, uint> _entryDictionary = new();
            private readonly LookupTable<TEntryKey> _baseTable;
            private uint _index = 0;

            public DependentLookupTable(LookupTable<TEntryKey> baseTable)
            {
                _baseTable = baseTable;
            }

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
}
