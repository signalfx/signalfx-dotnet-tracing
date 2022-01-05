// <copyright file="AerospikeTags.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace.Tagging
{
    internal partial class AerospikeTags : InstrumentationTags
    {
        [Tag(Trace.Tags.SpanKind)]
        public override string SpanKind => SpanKinds.Client;

        [Tag(Trace.Tags.InstrumentationName)]
        public string InstrumentationName => "Aerospike";

        [Tag(Trace.Tags.DbType)]
        public string DbType => "aerospike";

        [Tag(Trace.Tags.AerospikeKey)]
        public string Key { get; set; }

        [Tag(Trace.Tags.AerospikeNamespace)]
        public string Namespace { get; set; }

        [Tag(Trace.Tags.AerospikeSetName)]
        public string SetName { get; set; }

        [Tag(Trace.Tags.AerospikeUserKey)]
        public string UserKey { get; set; }
    }
}
