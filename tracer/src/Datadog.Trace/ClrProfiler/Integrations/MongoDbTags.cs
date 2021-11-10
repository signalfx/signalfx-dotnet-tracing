// <copyright file="MongoDbTags.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal class MongoDbTags : InstrumentationTags
    {
        protected static readonly IProperty<string>[] MongoDbTagsProperties =
            InstrumentationTagsProperties.Concat(
                new ReadOnlyProperty<MongoDbTags, string>(Tags.InstrumentationName, t => t.InstrumentationName),
                new Property<MongoDbTags, string>(Tags.DbType, t => t.DbType, (t, v) => t.DbType = v),
                new Property<MongoDbTags, string>(Tags.DbName, t => t.DbName, (t, v) => t.DbName = v),
                new Property<MongoDbTags, string>(Tags.DbStatement, t => t.DbStatement, (t, v) => t.DbStatement = v),
                new Property<MongoDbTags, string>(Tags.MongoDbQuery, t => t.Query, (t, v) => t.Query = v),
                new Property<MongoDbTags, string>(Tags.MongoDbCollection, t => t.Collection, (t, v) => t.Collection = v),
                new Property<MongoDbTags, string>(Tags.OutHost, t => t.Host, (t, v) => t.Host = v),
                new Property<MongoDbTags, string>(Tags.OutPort, t => t.Port, (t, v) => t.Port = v));

        public override string SpanKind => SpanKinds.Client;

        public string InstrumentationName => MongoDbIntegration.IntegrationName;

        public string DbType { get; set; }

        public string DbName { get; set; }

        public string DbStatement { get; set; }

        public string Query { get; set; }

        public string Collection { get; set; }

        public string Host { get; set; }

        public string Port { get; set; }

        protected override IProperty<string>[] GetAdditionalTags() => MongoDbTagsProperties;
    }
}
