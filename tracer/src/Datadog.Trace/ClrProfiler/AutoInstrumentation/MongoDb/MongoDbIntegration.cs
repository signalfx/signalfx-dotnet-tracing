// <copyright file="MongoDbIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Net;
using Datadog.Trace.Configuration;
using Datadog.Trace.DuckTyping;
using Datadog.Trace.Logging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.MongoDb
{
    /// <summary>
    /// Tracing integration for MongoDB.Driver.Core.
    /// </summary>
    internal static class MongoDbIntegration
    {
        internal const string IntegrationName = nameof(Configuration.IntegrationId.MongoDb);

        internal const string Major2 = "2";
        internal const string Major2Minor1 = "2.1";
        internal const string Major2Minor2 = "2.2"; // Synchronous methods added in 2.2
        internal const string MongoDbClientAssembly = "MongoDB.Driver.Core";

        private const string DefaultOperationName = "mongodb.query";
        private const string ServiceName = "mongodb";

        /// <summary>
        /// Operation to get the role of the "mongod" instance, see
        /// https://www.docs4dev.com/docs/en/mongodb/v3.6/reference/reference-command-isMaster.html
        /// </summary>
        private const string IsMasterOperation = "isMaster";

        /// <summary>
        /// The MongoDB database that stores system and authorization information.
        /// </summary>
        private const string AdminDatabaseName = "admin";

        internal const IntegrationId IntegrationId = Configuration.IntegrationId.MongoDb;

        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(MongoDbIntegration));

        internal static Scope CreateScope<TConnection>(object wireProtocol, TConnection connection)
            where TConnection : IConnection
        {
            var tracer = Tracer.Instance;

            if (!tracer.Settings.IsIntegrationEnabled(IntegrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            if (GetActiveMongoDbScope(tracer) != null)
            {
                // There is already a parent MongoDb span (nested calls)
                return null;
            }

            string statement = null;
            string operationName = null;
            string collectionName = null;
            string query = null;
            string resourceName = null;
            string databaseName = null;

            if (wireProtocol.TryDuckCast<IWireProtocolWithDatabaseNamespaceStruct>(out var protocolWithDatabaseNamespace))
            {
                databaseName = protocolWithDatabaseNamespace.DatabaseNamespace.DatabaseName;
            }

            if (wireProtocol.TryDuckCast<IWireProtocolWithCommandStruct>(out var protocolWithCommand)
                && protocolWithCommand.Command != null)
            {
                try
                {
                    // the name of the first element in the command BsonDocument will be the operation type (insert, delete, find, etc)
                    // and its value is the collection name
                    var firstElement = protocolWithCommand.Command.GetElement(0);
                    operationName = firstElement.Name;

                    if (operationName == IsMasterOperation || operationName == "hello" || databaseName == AdminDatabaseName)
                    {
                        // Assume that this is the driver doing "Heartbeat" or hello or "RoundTripTimeMonitor", don't create an activity for it.
                        return null;
                    }

                    if (tracer.Settings.TagMongoCommands)
                    {
                        statement = protocolWithCommand.Command.ToString();
                    }

                    collectionName = firstElement.Value?.ToString();
                    query = protocolWithCommand.Command.ToString();
                    resourceName = $"{operationName ?? "operation"} {databaseName ?? "database"}";
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Unable to access IWireProtocol.Command properties.");
                }
            }

            string host = null;
            string port = null;

            if (connection.EndPoint is IPEndPoint ipEndPoint)
            {
                host = ipEndPoint.Address.ToString();
                port = ipEndPoint.Port.ToString();
            }
            else if (connection.EndPoint is DnsEndPoint dnsEndPoint)
            {
                host = dnsEndPoint.Host;
                port = dnsEndPoint.Port.ToString();
            }

            string serviceName = tracer.Settings.GetServiceName(tracer, ServiceName);

            Scope scope = null;

            try
            {
                var tags = new MongoDbTags();
                scope = tracer.StartActiveInternal(operationName ?? DefaultOperationName, serviceName: serviceName, tags: tags);
                var span = scope.Span;
                span.LogicScope = DefaultOperationName;
                span.Type = SpanTypes.MongoDb;
                span.ResourceName = resourceName;
                tags.DbType = SpanTypes.MongoDb;
                tags.DbName = databaseName;
                tags.DbStatement = statement;
                tags.Query = query;
                tags.Collection = collectionName;
                tags.Host = host;
                tags.Port = port;

                tags.SetAnalyticsSampleRate(IntegrationId, tracer.Settings, enabledWithGlobalSetting: false);
                tracer.TracerManager.Telemetry.IntegrationGeneratedSpan(IntegrationId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }

        private static Scope GetActiveMongoDbScope(Tracer tracer)
        {
            var scope = tracer.InternalActiveScope;

            var parent = scope?.Span;

            if (parent != null &&
                parent.Type == SpanTypes.MongoDb &&
                parent.GetTag(Tags.InstrumentationName) != null)
            {
                return scope;
            }

            return null;
        }
    }
}
