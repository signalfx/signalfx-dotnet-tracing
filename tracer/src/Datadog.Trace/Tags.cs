// <copyright file="Tags.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;

namespace Datadog.Trace
{
    /// <summary>
    /// Standard span tags used by integrations.
    /// </summary>
    public static class Tags
    {
        /// <summary>
        /// The deployment environment of the profiled service.
        /// </summary>
        /// <remarks>
        /// This tag matches with the OpenTelemetry specification v0.1.7, see
        /// https://github.com/open-telemetry/opentelemetry-specification/blob/v1.7.0/specification/resource/semantic_conventions/deployment_environment.md.
        /// Legacy used the name "environment" until version 0.1.13 and upstream uses "env".
        /// </remarks>
        public const string Env = "deployment.environment";

        /// <summary>
        /// The version of the profiled service.
        /// </summary>
        public const string Version = "version";

        /// <summary>
        /// SignalFx Language tag, applied to root spans.
        /// </summary>
        public const string SignalFxLibrary = "signalfx.tracing.library";

        /// <summary>
        /// SignalFx Version tag, applied to root spans.
        /// </summary>
        public const string SignalFxVersion = "signalfx.tracing.version";

        /// <summary>
        /// The name of the integration that generated the span.
        /// Use OpenTracing tag "component"
        /// </summary>
        public const string InstrumentationName = "component";

        /// <summary>
        /// The name of the method that was instrumented to generate the span.
        /// </summary>
        public const string InstrumentedMethod = "instrumented.method";

        /// <summary>
        /// The kind of span (e.g. client, server). Not to be confused with <see cref="Span.Type"/>.
        /// </summary>
        /// <seealso cref="SpanKinds"/>
        public const string SpanKind = "span.kind";

        /// <summary>
        /// The URL of an HTTP request
        /// </summary>
        public const string HttpUrl = "http.url";

        /// <summary>
        /// The method of an HTTP request
        /// </summary>
        public const string HttpMethod = "http.method";

        /// <summary>
        /// The host of an HTTP request
        /// </summary>
        /// <remarks>
        /// Upstream uses "http.request.headers.host", however, to better align with OpenTelemetry we use
        /// "http.host" instead.
        /// </remarks>
        public const string HttpRequestHeadersHost = "http.host";

        /// <summary>
        /// The status code of an HTTP response
        /// </summary>
        public const string HttpStatusCode = "http.status_code";

        /// <summary>
        /// The end point requested
        /// </summary>
        internal const string HttpEndpoint = "http.endpoint";

        /// <summary>
        /// The user agent
        /// </summary>
        internal const string HttpUserAgent = "http.user_agent";

        /// <summary>
        /// Whether a span denotes an error.
        /// </summary>
        /// <remarks>
        /// Upstream does not have this tag, this is needed to better align with SFx.
        /// </remarks>
        public const string Error = "error";

        /// <summary>
        /// The error message of an exception
        /// </summary>
        /// <remarks>
        /// Upstream uses "error.msg", however, to better align with SFx we use
        /// "sfx.error.message" instead.
        /// </remarks>
        public const string ErrorMsg = "sfx.error.message";

        /// <summary>
        /// The type of an exception
        /// </summary>
        /// <remarks>
        /// Upstream uses "error.type", however, to better align with SFx we use
        /// "sfx.error.kind" instead.
        /// </remarks>
        public const string ErrorType = "sfx.error.kind";

        /// <summary>
        /// The stack trace of an exception
        /// </summary>
        /// <remarks>
        /// Upstream uses "error.stack", however, to better align with SFx we use
        /// "sfx.error.stack" instead.
        /// </remarks>
        public const string ErrorStack = "sfx.error.stack";

        /// <summary>
        /// The type of database (e.g. "mssql", "mysql", "postgresql", "sqlite", "oracle")
        /// </summary>
        /// <remarks>
        /// Upstream uses "db.type", however, to better align with OpenTelemetry we use
        /// "db.system" instead.
        /// </remarks>
        public const string DbType = "db.system";

        /// <summary>
        /// The user used to sign into a database
        /// </summary>
        public const string DbUser = "db.user";

        /// <summary>
        /// The name of the database.
        /// </summary>
        public const string DbName = "db.name";

        /// <summary>
        /// The name of the operation.
        /// </summary>
        public const string DbOperation = "db.operation";

        /// <summary>
        /// The command/query text
        /// </summary>
        public const string DbStatement = "db.statement";

        /// <summary>
        /// The query text
        /// </summary>
        public const string SqlQuery = "sql.query";

        /// <summary>
        /// The number of rows returned by a query
        /// </summary>
        public const string SqlRows = "sql.rows";

        /// <summary>
        /// The ASP.NET routing template.
        /// </summary>
        internal const string AspNetRoute = "aspnet.route";

        /// <summary>
        /// The MVC or Web API controller name.
        /// </summary>
        internal const string AspNetController = "aspnet.controller";

        /// <summary>
        /// The MVC or Web API action name.
        /// </summary>
        internal const string AspNetAction = "aspnet.action";

        /// <summary>
        /// The MVC or Web API area name.
        /// </summary>
        internal const string AspNetArea = "aspnet.area";

        /// <summary>
        /// The ASP.NET routing template.
        /// </summary>
        internal const string AspNetCoreRoute = "aspnet_core.route";

        /// <summary>
        /// The MVC or Web API controller name.
        /// </summary>
        internal const string AspNetCoreController = "aspnet_core.controller";

        /// <summary>
        /// The MVC or Web API action name.
        /// </summary>
        internal const string AspNetCoreAction = "aspnet_core.action";

        /// <summary>
        /// The MVC or Web API area name.
        /// </summary>
        internal const string AspNetCoreArea = "aspnet_core.area";

        /// <summary>
        /// The Razor Pages page name.
        /// </summary>
        internal const string AspNetCorePage = "aspnet_core.page";

        /// <summary>
        /// The Endpoint name in ASP.NET Core endpoint routing.
        /// </summary>
        internal const string AspNetCoreEndpoint = "aspnet_core.endpoint";

        /// <summary>
        /// The hostname of a outgoing server connection.
        /// </summary>
        /// <remarks>
        /// Upstream uses "out.host", however, to better align with OpenTelemetry we use
        /// "net.peer.name" instead.
        /// </remarks>>
        public const string OutHost = Net.PeerName;

        /// <summary>
        /// The port of a outgoing server connection.
        /// </summary>
        /// <remarks>
        /// Upstream uses "out.port", however, to better align with OpenTelemetry we use
        /// "net.peer.port" instead.
        /// </remarks>>
        public const string OutPort = Net.PeerPort;

        /// <summary>
        /// The raw command sent to Redis.
        /// </summary>
        internal const string RedisRawCommand = DbStatement;

        /// <summary>
        /// A MongoDB query.
        /// </summary>
        internal const string MongoDbQuery = "mongodb.query";

        /// <summary>
        /// A MongoDB collection name.
        /// </summary>
        internal const string MongoDbCollection = "mongodb.collection";

        /// <summary>
        /// The operation name of the GraphQL request.
        /// </summary>
        internal const string GraphQLOperationName = "graphql.operation.name";

        /// <summary>
        /// The operation type of the GraphQL request.
        /// </summary>
        internal const string GraphQLOperationType = "graphql.operation.type";

        /// <summary>
        /// The source defining the GraphQL request.
        /// </summary>
        internal const string GraphQLSource = "graphql.source";

        /// <summary>
        /// The AMQP method.
        /// </summary>
        internal const string AmqpCommand = "amqp.command";

        /// <summary>
        /// The name of the AMQP exchange the message was originally published to.
        /// </summary>
        internal const string AmqpExchange = "amqp.exchange";

        /// <summary>
        /// The routing key for the AMQP message.
        /// </summary>
        internal const string AmqpRoutingKey = "amqp.routing_key";

        /// <summary>
        /// The name of the queue for the AMQP message.
        /// </summary>
        internal const string AmqpQueue = "amqp.queue";

        /// <summary>
        /// The delivery mode of the AMQP message.
        /// </summary>
        internal const string AmqpDeliveryMode = "amqp.delivery_mode";

        /// <summary>
        /// The partition associated with a record
        /// </summary>
        /// <remarks>
        /// Upstream uses "kafka.partition", however, to better align with OpenTelemetry we use
        /// "messaging.kafka.partition" instead.
        /// </remarks>
        internal const string KafkaPartition = "messaging.kafka.partition";

        /// <summary>
        /// The offset inside a partition associated with a record
        /// </summary>
        /// <remarks>
        /// Upstream uses "kafka.offset", however, to better align with OpenTelemetry we use
        /// "messaging.kafka.offset" instead.
        /// </remarks>
        internal const string KafkaOffset = "messaging.kafka.offset";

        /// <summary>
        /// Whether the record was a "tombstone" record
        /// </summary>
        /// <remarks>
        /// Upstream uses "kafka.tombstone", however, to better align with OpenTelemetry we use
        /// "messaging.kafka.tombstone" instead.
        /// </remarks>
        internal const string KafkaTombstone = "messaging.kafka.tombstone";

        /// <summary>
        /// The size of the message.
        /// </summary>
        public const string MessageSize = "message.size";

        /// <summary>
        /// The agent that instrumented the associated AWS SDK span.
        /// </summary>
        internal const string AwsAgentName = "aws.agent";

        /// <summary>
        /// The operation associated with the AWS SDK span.
        /// </summary>
        internal const string AwsOperationName = "aws.operation";

        /// <summary>
        /// The region associated with the AWS SDK span.
        /// </summary>
        internal const string AwsRegion = "aws.region";

        /// <summary>
        /// The request ID associated with the AWS SDK span.
        /// </summary>
        internal const string AwsRequestId = "aws.requestId";

        /// <summary>
        /// The service associated with the AWS SDK span.
        /// </summary>
        internal const string AwsServiceName = "aws.service";

        /// <summary>
        /// The queue name associated with the AWS SDK span.
        /// </summary>
        internal const string AwsQueueName = "aws.queue.name";

        /// <summary>
        /// The queue URL associated with the AWS SDK span.
        /// </summary>
        internal const string AwsQueueUrl = "aws.queue.url";

        /// <summary>
        /// The sampling priority for the entire trace.
        /// </summary>
        public const string SamplingPriority = "sampling.priority";

        /// <summary>
        /// A user-friendly tag that sets the sampling priority to <see cref="Trace.SamplingPriority.UserKeep"/>.
        /// </summary>
        public const string ManualKeep = "manual.keep";

        /// <summary>
        /// A user-friendly tag that sets the sampling priority to <see cref="Trace.SamplingPriority.UserReject"/>.
        /// </summary>
        public const string ManualDrop = "manual.drop";

        /// <summary>
        /// Configures Trace Analytics.
        /// </summary>
        internal const string Analytics = "_dd1.sr.eausr";

        /// <summary>
        /// Language tag, applied to root spans that are .NET runtime (e.g., ASP.NET)
        /// </summary>
        public const string Language = "language";

        /// <summary>
        /// The runtime family tag, it will be placed on the service entry span, the first span opened for a
        /// service. For this library it will always have the value "dotnet".
        /// </summary>
        internal const string RuntimeFamily = "_dd.runtime_family";

        /// <summary>
        /// The resource ID of the site instance in Azure App Services where the traced application is running.
        /// </summary>
        internal const string AzureAppServicesResourceId = "aas.resource.id";

        /// <summary>
        /// The resource group of the site instance in Azure App Services where the traced application is running.
        /// </summary>
        internal const string AzureAppServicesResourceGroup = "aas.resource.group";

        /// <summary>
        /// The site name of the site instance in Azure where the traced application is running.
        /// </summary>
        internal const string AzureAppServicesSiteName = "aas.site.name";

        /// <summary>
        /// The version of the extension installed where the traced application is running.
        /// </summary>
        internal const string AzureAppServicesExtensionVersion = "aas.environment.extension_version";

        /// <summary>
        /// The instance name in Azure where the traced application is running.
        /// </summary>
        internal const string AzureAppServicesInstanceName = "aas.environment.instance_name";

        /// <summary>
        /// The instance ID in Azure where the traced application is running.
        /// </summary>
        internal const string AzureAppServicesInstanceId = "aas.environment.instance_id";

        /// <summary>
        /// The operating system in Azure where the traced application is running.
        /// </summary>
        internal const string AzureAppServicesOperatingSystem = "aas.environment.os";

        /// <summary>
        /// The runtime in Azure where the traced application is running.
        /// </summary>
        internal const string AzureAppServicesRuntime = "aas.environment.runtime";

        /// <summary>
        /// The kind of application instance running in Azure.
        /// Possible values: app, api, mobileapp, app_linux, app_linux_container, functionapp, functionapp_linux, functionapp_linux_container
        /// </summary>
        internal const string AzureAppServicesSiteKind = "aas.site.kind";

        /// <summary>
        /// The type of application instance running in Azure.
        /// Possible values: app, function
        /// </summary>
        internal const string AzureAppServicesSiteType = "aas.site.type";

        /// <summary>
        /// The subscription ID of the site instance in Azure App Services where the traced application is running.
        /// </summary>
        internal const string AzureAppServicesSubscriptionId = "aas.subscription.id";

        /// <summary>
        /// The type of trigger for an azure function
        /// </summary>
        internal const string AzureFunctionTriggerType = "aas.function.trigger";

        /// <summary>
        /// The UI name of the azure function
        /// </summary>
        internal const string AzureFunctionName = "aas.function.name";

        /// <summary>
        /// The full method name of the azure function
        /// </summary>
        internal const string AzureFunctionMethod = "aas.function.method";

        /// <summary>
        /// The literal type of the binding for the azure function trigger
        /// </summary>
        internal const string AzureFunctionBindingSource = "aas.function.binding";

        /// <summary>
        /// Configures the origin of the trace
        /// </summary>
        internal const string Origin = "_dd.origin";

        /// <summary>
        /// Configures the measured metric for a span.
        /// </summary>
        internal const string Measured = "_dd.measured";

        /// <summary>
        /// Marks a span as a partial snapshot.
        /// </summary>
        internal const string PartialSnapshot = "_dd.partial_version";

        /// <summary>
        /// The name of the Msmq command the message was published to.
        /// </summary>
        internal const string MsmqCommand = "msmq.command";

        /// <summary>
        /// Is the msmq queue supporting transactional messages
        /// </summary>
        internal const string MsmqIsTransactionalQueue = "msmq.queue.transactional";

        /// <summary>
        /// The name of the Msmq queue the message was published to, containing host name and path.
        /// </summary>
        internal const string MsmqQueuePath = "msmq.queue.path";

        /// <summary>
        /// A boolean indicating if it's part of a transaction.
        /// </summary>
        internal const string MsmqMessageWithTransaction = "msmq.message.transactional";

        /// <summary>
        /// A CosmosDb container name.
        /// </summary>
        internal const string CosmosDbContainer = "cosmosdb.container";

        /// <summary>
        /// Should contain the public IP of the host initiating the request.
        /// </summary>
        internal const string ActorIp = "actor.ip";

        /// <summary>
        /// The ip as reported by the framework.
        /// </summary>
        internal const string NetworkClientIp = "network.client.ip";

        internal const string ElasticsearchAction = "elasticsearch.action";

        internal const string ElasticsearchMethod = DbOperation;

        internal const string ElasticsearchUrl = "elasticsearch.url";

        internal const string RuntimeId = "runtime-id";

        internal const string AerospikeKey = "db.aerospike.key";

        internal const string AerospikeNamespace = "db.aerospike.namespace";

        internal const string AerospikeSetName = "db.aerospike.setname";

        internal const string AerospikeUserKey = "db.aerospike.userkey";

        internal const string CouchbaseOperationCode = "couchbase.operation.code";
        internal const string CouchbaseOperationBucket = "couchbase.operation.bucket";
        internal const string CouchbaseOperationKey = "couchbase.operation.key";

        internal const string GrpcMethodKind = "grpc.method.kind";
        internal const string GrpcMethodPath = "grpc.method.path";
        internal const string GrpcMethodPackage = "grpc.method.package";
        internal const string GrpcMethodService = "grpc.method.service";
        internal const string GrpcMethodName = "grpc.method.name";
        internal const string GrpcStatusCode = "grpc.status.code";

        internal static class User
        {
            internal const string Email = "usr.email";
            internal const string Name = "usr.name";
            internal const string Id = "usr.id";
            internal const string SessionId = "usr.session_id";
            internal const string Role = "usr.role";
            internal const string Scope = "usr.scope";
        }

        /// <summary>
        /// Messaging tags
        /// </summary>
        public static class Messaging
        {
            /// <summary>
            /// A string identifying the messaging system
            /// </summary>
            /// <example>kafka; rabbitmq; rocketmq; activemq; AmazonSQS.</example>
            public const string System = "messaging.system";

            /// <summary>
            /// The message destination name.
            /// This might be equal to the span name but is required nevertheless.
            /// </summary>
            public const string Destination = "messaging.destination";

            /// <summary>
            /// The kind of message destination.
            /// </summary>
            /// <example>queue.</example>
            public const string DestinationKind = "messaging.destination_kind";

            /// <summary>
            /// A boolean that is true if the message destination is temporary.
            /// </summary>
            public const string TempDestination = "messaging.temp_destination";

            /// <summary>
            /// The name of the transport protocol.
            /// </summary>
            /// <example>AMQP; MQTT.</example>
            public const string Protocol = "messaging.protocol";

            /// <summary>
            /// The version of the transport protocol.
            /// </summary>
            public const string ProtocolVersion = "messaging.protocol_version";

            /// <summary>
            /// Connection string.
            /// </summary>
            public const string Url = "messaging.url";

            /// <summary>
            /// A string identifying the kind of message consumption.
            /// </summary>
            /// <example>send; receive; process.</example>
            public const string Operation = "messaging.operation";

            /// <summary>
            /// A value used by the messaging system as an identifier for the message, represented as a string.
            /// </summary>
            public const string MessageId = "messaging.message_id";

            /// <summary>
            /// The conversation ID identifying the conversation to which the message belongs,
            /// represented as a string. Sometimes called "Correlation ID".
            /// </summary>
            public const string ConversationId = "messaging.conversation_id";

            /// <summary>
            /// The identifier for the consumer receiving a message.
            /// </summary>
            public const string ConsumerId = "messaging.consumer_id	";

            /// <summary>
            /// The (uncompressed) size of the message payload in bytes.
            /// Also use this attribute if it is unknown whether the compressed or uncompressed payload size is reported.
            /// </summary>
            public const string MessageSize = "messaging.message_payload_size_bytes";

            /// <summary>
            /// The compressed size of the message payload in bytes.
            /// </summary>
            public const string MessageSizeCompressed = "messaging.message_payload_compressed_size_bytes";
        }

        /// <summary>
        /// RabbitMQ tags
        /// </summary>
        public static class RabbitMq
        {
            /// <summary>
            /// RabbitMQ message routing key.
            /// </summary>
            public const string RoutingKey = "messaging.rabbitmq.routing_key";
        }

        /// <summary>
        /// Network tags
        /// </summary>
        public static class Net
        {
            /// <summary>
            /// Remote address of the peer (dotted decimal for IPv4 or RFC5952 for IPv6)
            /// </summary>
            public const string PeerIP = "net.peer.ip";

            /// <summary>
            /// Remote hostname or similar.
            /// This should be the IP/hostname of the broker (or other network-level peer) this specific message is sent to/received from.
            /// </summary>
            public const string PeerName = "net.peer.name";

            /// <summary>
            /// Remote port number.
            /// </summary>
            public const string PeerPort = "net.peer.port";
        }

        internal static class TagPropagation
        {
            internal const string Error = "_dd.propagation_error";
        }
    }
}
