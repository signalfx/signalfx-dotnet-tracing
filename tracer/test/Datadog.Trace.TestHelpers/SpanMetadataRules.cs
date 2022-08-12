// <copyright file="SpanMetadataRules.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.TestHelpers
{
#pragma warning disable SA1601 // Partial elements should be documented
    public static partial class SpanMetadataRules
    {
        public static Result IsAdoNet(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "fake.query")
                .Matches(Type, "sql"))
            .Tags(s => s
                .IsOptional("db.name")
                .IsPresent("version")
                .Matches("db.system", "fake")
                .Matches("component", "AdoNet")
                .Matches("span.kind", "client"));

        public static Result IsAerospike(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "aerospike.command")
                .Matches(LogicScope, "aerospike.command")
                .Matches(Type, "aerospike"))
            .Tags(s => s
                .IsOptional("aerospike.key")
                .IsOptional("aerospike.namespace")
                .IsOptional("aerospike.setname")
                .IsOptional("aerospike.userkey")
                .Matches("db.system", "aerospike")
                .Matches("component", "aerospike")
                .Matches("span.kind", "client"));

        public static Result IsAspNet(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, span.Tags["http.method"])
                .Matches(LogicScope, "aspnet.request")
                .Matches(Type, "web"))
            .Tags(s => s
                .IsPresent("http.method")
                .IsPresent("http.request.headers.host")
                .IsPresent("http.status_code")
                .IsPresent("http.url")
                // BUG: component tag is not set
                // .Matches("component", "aspnet")
                .Matches("span.kind", "server"));

        public static Result IsAspNetMvc(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, span.Tags["http.method"])
                .Matches(LogicScope, "aspnet-mvc.request")
                .Matches(Type, "web"))
            .Tags(s => s
                .IsPresent("aspnet.action")
                .IsOptional("aspnet.area")
                .IsPresent("aspnet.controller")
                .IsPresent("aspnet.route")
                .IsPresent("http.method")
                .IsPresent("http.request.headers.host")
                .IsPresent("http.status_code")
                .IsPresent("http.url")
                // BUG: component tag is not set
                // .Matches("component", "aspnet")
                .Matches("span.kind", "server"));

        public static Result IsAspNetWebApi2(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "aspnet-webapi.request")
                .Matches(Type, "web"))
            .Tags(s => s
                .IsOptional("aspnet.action")
                .IsOptional("aspnet.controller")
                .IsPresent("aspnet.route")
                .IsPresent("http.method")
                .IsPresent("http.request.headers.host")
                // BUG: When WebApi2 throws an exception, we cannot immediately set the
                // status code because the response hasn't been written yet.
                // For ASP.NET, we register a callback to populate http.status_code
                // when the request has completed, but on OWIN there is no such mechanism.
                // What we should do is instrument OWIN and assert that that has the
                // "http.status_code" tag
                // .IsPresent("http.status_code")
                .IsPresent("http.url")
                // BUG: component tag is not set
                // .Matches("component", "aspnet")
                .Matches("span.kind", "server"));

        public static Result IsAspNetCore(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "aspnet_core.request")
                .Matches(Type, "web"))
            .Tags(s => s
                .IsPresent("http.method")
                .IsPresent("http.request.headers.host")
                .IsPresent("http.status_code")
                .IsPresent("http.url")
                .Matches("component", "aspnet_core")
                .Matches("span.kind", "server"));

        public static Result IsAspNetCoreMvc(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "aspnet_core_mvc.request")
                .Matches(Type, "web"))
            .Tags(s => s
                .IsPresent("aspnet_core.action")
                .IsOptional("aspnet_core.area")
                .IsPresent("aspnet_core.controller")
                .IsOptional("aspnet_core.page")
                .Matches("component", "aspnet_core")
                .Matches("span.kind", "server"));

        public static Result IsAwsSqs(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "sqs.request")
                .Matches(Type, "http"))
            .Tags(s => s
                .Matches("aws.agent", "dotnet-aws-sdk")
                .IsPresent("aws.operation")
                .IsOptional("aws.region")
                .IsPresent("aws.requestId")
                .Matches("aws.service", "SQS")
                .IsOptional("aws.queue.name")
                .IsOptional("aws.queue.url")
                .IsPresent("http.method")
                .IsPresent("http.status_code")
                .IsPresent("http.url")
                .Matches("component", "aws-sdk")
                .Matches("span.kind", "client"));

        public static Result IsCosmosDb(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "cosmosdb.query")
                .Matches(Type, "sql"))
            .Tags(s => s
                .IsOptional("cosmosdb.container")
                .IsOptional("db.name")
                .IsPresent("net.peer.name")
                .IsPresent("version")
                .Matches("db.system", "cosmosdb")
                .Matches("component", "CosmosDb")
                .Matches("span.kind", "client"));

        public static Result IsCouchbase(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "couchbase.query")
                .Matches(Type, "db"))
            .Tags(s => s
                .IsOptional("couchbase.operation.bucket")
                .IsPresent("couchbase.operation.code")
                .IsPresent("couchbase.operation.key")
                .IsPresent("version")
                .IsOptional("net.peer.port")
                .IsOptional("net.peer.name")
                .Matches("component", "Couchbase")
                .Matches("span.kind", "client"));

        public static Result IsElasticsearchNet(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "elasticsearch.query")
                .Matches(Type, "elasticsearch"))
            .Tags(s => s
                .IsPresent("elasticsearch.action")
                .IsPresent("elasticsearch.method")
                .IsPresent("elasticsearch.url")
                .IsPresent("version")
                .IsOptional("db.statement")
                .Matches("db.system", "elasticsearch")
                .Matches("component", "elasticsearch-net")
                .Matches("span.kind", "client"));

        public static Result IsGraphQL(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .MatchesOneOf(LogicScope, "graphql.execute", "graphql.validate")
                .Matches(Type, "graphql"))
            .Tags(s => s
                .IsOptional("graphql.operation.name")
                .IsOptional("graphql.operation.type")
                .IsPresent("graphql.source")
                .IsPresent("version")
                .Matches("component", "GraphQL")
                .Matches("span.kind", "server"));

        public static Result IsGrpc(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "grpc.request")
                .Matches(LogicScope, "grpc.request")
                .Matches(Type, "grpc"))
            .Tags(s => s
                .IsPresent("grpc.method.kind")
                .IsPresent("grpc.method.name")
                .IsPresent("grpc.method.package")
                .IsPresent("grpc.method.path")
                .IsPresent("grpc.method.service")
                .IsPresent("grpc.status.code")
                .Matches("component", "Grpc")
                .MatchesOneOf("span.kind", "client", "server"));

        public static Result IsHttpMessageHandler(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, span.Tags["http.method"])
                .Matches(LogicScope, "http.request")
                .Matches(Type, "http"))
            .Tags(s => s
                .IsPresent("http-client-handler-type")
                .IsPresent("http.method")
                .IsPresent("http.status_code")
                .IsPresent("http.url")
                .IsPresent("version")
                .Matches("component", "HttpMessageHandler")
                .Matches("span.kind", "client"));

        public static Result IsKafka(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .MatchesOneOf(LogicScope, "kafka.consume", "kafka.produce")
                .Matches(Type, "topic"))
            .Tags(s => s
                .IsOptional("kafka.offset")
                .IsOptional("kafka.partition")
                .IsOptional("kafka.tombstone")
                .IsOptional("message.queue_time_ms")
                .Matches("component", "kafka")
                .IsPresent("span.kind"));

        public static Result IsMongoDB(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "mongodb.query")
                .Matches(Type, "mongodb"))
            .Tags(s => s
                .IsOptional("db.name")
                .IsOptional("db.statement")
                .IsOptional("mongodb.collection")
                .IsOptional("mongodb.query")
                .IsPresent("net.peer.name")
                .IsPresent("net.peer.port")
                .Matches("component", "MongoDb")
                .Matches("db.system", "mongodb")
                .Matches("span.kind", "client"));

        public static Result IsMsmq(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "msmq.command")
                .Matches(Type, "queue"))
            .Tags(s => s
                .IsPresent("msmq.command")
                .IsOptional("msmq.message.transactional")
                .IsPresent("msmq.queue.path")
                .IsPresent("version")
                .IsOptional("msmq.queue.transactional")
                .Matches("component", "msmq")
                .MatchesOneOf("span.kind", "client", "producer", "consumer"));

        public static Result IsMySql(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "mysql.query")
                .Matches(Type, "sql"))
            .Tags(s => s
                .IsPresent("db.name")
                .IsPresent("db.statement")
                .IsPresent("version")
                .Matches("db.system", "mysql")
                .Matches("component", "MySql")
                .Matches("span.kind", "client"));

        public static Result IsNpgsql(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "postgresql.query")
                .Matches(Type, "sql"))
            .Tags(s => s
                .IsPresent("db.name")
                .IsPresent("db.statement")
                .IsPresent("version")
                .Matches("db.system", "postgresql")
                .Matches("component", "Npgsql")
                .Matches("span.kind", "client"));

        public static Result IsOracle(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "oracle.query")
                .Matches(Type, "sql"))
            .Tags(s => s
                .IsPresent("db.name")
                .Matches("db.system", "oracle")
                .Matches("component", "Oracle")
                .Matches("span.kind", "client"));

        public static Result IsRabbitMQ(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "amqp.command")
                .Matches(Type, "queue"))
            .Tags(s => s
                .IsPresent("amqp.command")
                .IsOptional("amqp.delivery_mode")
                .IsOptional("amqp.exchange")
                .IsOptional("amqp.routing_key")
                .IsOptional("amqp.queue")
                .IsOptional("message.size")
                .Matches("component", "RabbitMQ")
                .Matches("messaging.system", "rabbitmq")
                .IsPresent("span.kind"));

        public static Result IsServiceFabric(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .MatchesOneOf(Name, "service_remoting.client", "service_remoting.server"))
            .Tags(s => s
                .IsPresent("service-fabric.application-id")
                .IsPresent("service-fabric.application-name")
                .IsPresent("service-fabric.partition-id")
                .IsPresent("service-fabric.node-id")
                .IsPresent("service-fabric.node-name")
                .IsPresent("service-fabric.service-name")
                .IsPresent("service-fabric.service-remoting.uri")
                .IsPresent("service-fabric.service-remoting.method-name")
                .IsOptional("service-fabric.service-remoting.method-id")
                .IsOptional("service-fabric.service-remoting.interface-id")
                .IsOptional("service-fabric.service-remoting.invocation-id")
                .MatchesOneOf("span.kind", "client", "server"));

        public static Result IsServiceRemoting(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .MatchesOneOf(Name, "service_remoting.client", "service_remoting.server"))
            .Tags(s => s
                .IsPresent("service-fabric.service-remoting.uri")
                .IsPresent("service-fabric.service-remoting.method-name")
                .IsOptional("service-fabric.service-remoting.method-id")
                .IsOptional("service-fabric.service-remoting.interface-id")
                .IsOptional("service-fabric.service-remoting.invocation-id")
                .MatchesOneOf("span.kind", "client", "server"));

        public static Result IsServiceStackRedis(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "redis.command")
                .Matches(Type, "redis"))
            .Tags(s => s
                .IsPresent("db.system")
                .IsPresent("db.statement")
                .IsPresent("net.peer.port")
                .IsPresent("net.peer.port")
                .Matches("component", "ServiceStackRedis")
                .Matches("span.kind", "client"));

        public static Result IsStackExchangeRedis(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "redis.command")
                .Matches(Type, "redis"))
            .Tags(s => s
                .IsPresent("db.system")
                .IsPresent("db.statement")
                .IsPresent("net.peer.name")
                .IsPresent("net.peer.port")
                .Matches("component", "StackExchangeRedis")
                .Matches("span.kind", "client"));

        public static Result IsSqlite(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "sqlite.query")
                .Matches(Type, "sql"))
            .Tags(s => s
                .IsOptional("db.name")
                .IsPresent("db.statement")
                .IsPresent("version")
                .Matches("db.system", "sqlite")
                .Matches("component", "Sqlite")
                .Matches("span.kind", "client"));

        public static Result IsSqlClient(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, "mssql.query")
                .Matches(Type, "sql"))
            .Tags(s => s
                .IsOptional("db.name")
                .IsPresent("db.statement")
                .IsPresent("version")
                .Matches("db.system", "mssql")
                .Matches("component", "SqlClient")
                .Matches("span.kind", "client"));

        public static Result IsWcf(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(LogicScope, "wcf.request")
                .Matches(Type, "web"))
            .Tags(s => s
                .IsPresent("http.url")
                .IsOptional("http.method")
                .Matches("component", "Wcf")
                .Matches("span.kind", "server"));

        public static Result IsWebRequest(this MockSpan span) => Result.FromSpan(span)
            .Properties(s => s
                .Matches(Name, span.Tags["http.method"])
                .Matches(LogicScope, "http.request")
                .Matches(Type, "http"))
            .Tags(s => s
                .IsPresent("http.method")
                .IsPresent("http.status_code")
                .IsPresent("http.url")
                .IsPresent("version")
                .MatchesOneOf("component", "HttpMessageHandler", "WebRequest")
                .Matches("span.kind", "client"));
    }
}
