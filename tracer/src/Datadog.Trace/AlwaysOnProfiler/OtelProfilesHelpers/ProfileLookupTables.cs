using System;
using System.Collections.Generic;
using System.Text;
using Datadog.Tracer.OpenTelemetry.Proto.Common.V1;
using Datadog.Tracer.OpenTelemetry.Proto.Profiles.V1;

namespace Datadog.Trace.AlwaysOnProfiler.OtelProfilesHelpers;

internal class ProfileLookupTables
{
    private readonly CustomEntryKeyLookupTable<Indices, Stacktrace> _stackTraceTable = new();
    private readonly CustomEntryKeyLookupTable<string, Location> _locationTable = new();
    private readonly CustomEntryKeyLookupTable<string, Function> _functionTable = new();
    private readonly CustomEntryKeyLookupTable<string, Link> _linkTable = new();
    private readonly CustomEntryKeyLookupTable<string, AttributeSet> _attributeSetTable = new();
    private readonly LookupTable<string> _stringTable = new();

    public ProfileLookupTables()
    {
        _stringTable.GetOrCreateIndex(string.Empty);
        _linkTable.GetOrCreateIndex(string.Empty, () => new Link());
        _attributeSetTable.GetOrCreateIndex(string.Empty, () => new AttributeSet());
    }

    public uint GetStringIndex(string s) => _stringTable.GetOrCreateIndex(s);

    public uint GetAttributeSetIndex(KeyValue[] attributes)
    {
        return _attributeSetTable.GetOrCreateIndex(
            AttributeSetKey(attributes),
            () =>
            {
                var attributeSet = new AttributeSet();
                attributeSet.Attributes.AddRange(attributes);
                return attributeSet;
            });

        string AttributeSetKey(IEnumerable<KeyValue> attributeSet)
        {
            var sb = new StringBuilder();
            foreach (var attribute in attributeSet)
            {
                sb.Append(attribute.Key);
                sb.Append(':');

                // OTLP_PROFILES: TODO: For now assuming that the value is always an IntValue, an indext to stringTable
                if (!attribute.Value.ShouldSerializeIntValue())
                {
                    throw new NotSupportedException("Only 'IntValue' is supported.");
                }

                sb.Append(attribute.Value.IntValue);
                sb.Append(';');
            }

            return sb.ToString();
        }
    }

    public uint GetLinkIndex(ThreadSample threadSample)
    {
        // Process trace context, if any
        uint linkIndex = 0; // Default is thread sample not associated to any span and trace context.
        if (threadSample.SpanId != 0 || threadSample.TraceIdHigh != 0 || threadSample.TraceIdLow != 0)
        {
            // OTLP_PROFILES: TODO: the key needs to be unique, for now, something very simple. It can be optimized later.
            var linkKey = $"{threadSample.SpanId}{threadSample.TraceIdLow}{threadSample.TraceIdHigh}";
            linkIndex = _linkTable.GetOrCreateIndex(
                linkKey,
                () =>
                {
                    // OTLP_PROFILES: TODO: check the conversion below, for now just to compile
                    var traceId = new List<byte>(BitConverter.GetBytes(threadSample.TraceIdLow));
                    traceId.AddRange(BitConverter.GetBytes(threadSample.TraceIdHigh));
                    var link = new Link
                    {
                        SpanId = BitConverter.GetBytes(threadSample.SpanId), TraceId = traceId.ToArray()
                    };

                    return link;
                });
        }

        return linkIndex;
    }

    public uint GetStacktraceIndex(IList<string> frames)
    {
        var stackTraceLocations = new List<uint>(frames.Count);
        foreach (var functionName in frames)
        {
            var functionIndex = _functionTable.GetOrCreateIndex(
                functionName,
                () => new Function
                {
                    NameIndex = _stringTable.GetOrCreateIndex(functionName ?? string.Empty),
                    FilenameIndex = _stringTable.GetOrCreateIndex("unknown")
                });

            var locationIndex = _locationTable.GetOrCreateIndex(
                functionName,
                () =>
                {
                    var location = new Location();
                    location.Lines.Add(new Line { FunctionIndex = functionIndex });
                    return location;
                });

            stackTraceLocations.Add(locationIndex);
        }

        return _stackTraceTable.GetOrCreateIndex(
            new Indices(stackTraceLocations),
            () =>
            {
                var stackTrace = new Stacktrace
                {
                    LocationIndices = stackTraceLocations.ToArray()
                };
                return stackTrace;
            });
    }

    public void CopyLookupTablesToProfile(Profile profile)
    {
        CopySingleLookupTable(_stackTraceTable, profile.Stacktraces);
        CopySingleLookupTable(_locationTable, profile.Locations);
        CopySingleLookupTable(_functionTable, profile.Functions);
        CopySingleLookupTable(_linkTable, profile.Links);
        CopySingleLookupTable(_attributeSetTable, profile.AttributeSets);
        CopySingleLookupTable(_stringTable, profile.StringTables);

        void CopySingleLookupTable<T>(List<T> src, List<T> dst)
        {
            if (dst.Count > 0)
            {
                throw new InvalidOperationException("A destination table on the profile object is not empty.");
            }

            dst.AddRange(src);
        }
    }
}
