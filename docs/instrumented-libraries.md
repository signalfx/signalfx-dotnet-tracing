# Instumented libraries and framework

## Fully supported

The libraries below should be fully supported and the conventions are inspired by
the [OpenTelemetry Trace Semantic Conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/trace/semantic_conventions).

| ID | Library |
| -- | ---     |
| Aerospike | [Aerospike.Client](https://www.nuget.org/packages/Aerospike.Client/) |
| AspNet | ASP.NET 4.x |
| AspNetCore | ASP.NET Core |
| AspNetMvc | ASP.NET MVC |
| AspNetWebApi2 | ASP.NET Web API 2 |
| CurlHandler | [`System.Net.Http.CurlHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler#httpclienthandler-in-net-core) |
| ElasticsearchNet | [Elasticsearch.Net](https://www.nuget.org/packages/Elasticsearch.Net/) |
| GraphQL | [GraphQL](https://www.nuget.org/packages/GraphQL/) |
| HttpMessageHandler | [`System.Net.Http.MessageHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessagehandler) |
| HttpSocketsHandler | [`System.Net.Http.SocketsHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler) |
| ILogger | [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/) |
| Kafka | [Confluent.Kafka](https://www.nuget.org/packages/Confluent.Kafka/) |
| MongoDb | [MongoDB.Driver.Core](https://www.nuget.org/packages/MongoDB.Driver.Core/) |
| MySql | [MySql.Data](https://www.nuget.org/packages/MySql.Data/) |
| Npgsql | [Npgsql](https://www.nuget.org/packages/Npgsql/) |
| Oracle | [Oracle.ManagedDataAccess](https://www.nuget.org/packages/Oracle.ManagedDataAccess/) |
| RabbitMQ | [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client/) |
| ServiceStackRedis | [ServiceStack.Redis](https://www.nuget.org/packages/ServiceStack.Redis/) |
| SqlClient | [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/) and [`System.Data.SqlClient`](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient) |
| Sqlite | [SQLite](https://www.nuget.org/packages/SQLite/) |
| StackExchangeRedis | [StackExchange.Redis](https://www.nuget.org/packages/StackExchange.Redis/) |
| Wcf | Windows Communication Foundation (WCF) |
| WebRequest | [`System.Net.WebRequest`](https://docs.microsoft.com/en-us/dotnet/api/system.net.webreques) |
| WinHttpHandler | [`System.Net.Http.WinHttpHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.winhttphandler) |

## Partially supported

The libraries below are instrumented, yet the instrumentation, may not
produce appropriate spans.

| ID | Library |
| -- | ---     |
| AwsSdk | [AWSSDK.Core](https://www.nuget.org/packages/AWSSDK.Core/) |
| AwsSqs | [AWSSDK.SQS](https://www.nuget.org/packages/AWSSDK.SQS/) |
| AzureFunctions | [Microsoft.Azure.WebJobs](https://www.nuget.org/packages/Microsoft.Azure.WebJobs/) |
| CosmosDb | [Microsoft.Azure.Cosmos](https://www.nuget.org/packages/Microsoft.Azure.Cosmos/) |
| Couchbase | [CouchbaseNetClient](https://www.nuget.org/packages/CouchbaseNetClient/) |
| Msmq | [`System.Messaging`](https://docs.microsoft.com/en-us/dotnet/api/system.messaging) |
| MsTestV2 | [Microsoft.VisualStudio.TestPlatform](https://www.nuget.org/packages/Microsoft.VisualStudio.TestPlatform/) |
| NUnit | [NUnit](https://www.nuget.org/packages/NUnit/) |
| ServiceRemoting | [Microsoft.ServiceFabric.Services.Remoting](https://www.nuget.org/packages/Microsoft.ServiceFabric.Services.Remoting/) |
| XUnit | [xunit](https://www.nuget.org/packages/xunit) |

## Details

You can get exact information on the instrumented code [here](../tracer/src/Datadog.Trace/ClrProfiler/InstrumentationDefinitions.Generated.cs).

Each line contains following information (in order):

- Assembly name
- Type name
- Method name
- Method signature (arguments and return) types
- Minimum assembly version - major number
- Minimum assembly version - minor number
- Minimum assembly version - patch number
- Maximum assembly version - major number
- Maximum assembly version - minor number
- Maximum assembly version - patch number

For example:

```csharp
new("AerospikeClient", "Aerospike.Client.SyncCommand", "ExecuteCommand",  new[] { "System.Void" }, 4, 0, 0, 4, 65535, 65535, assemblyFullName, "Datadog.Trace.ClrProfiler.AutoInstrumentation.Aerospike.SyncCommandIntegration"),
```

means that the following code is instrumented:

- assembly: `AerospikeClient`, versions: [`4.0.0`, `5.0.0`)
- type: `Aerospike.Client.SyncCommand`
- method: `void ExecuteCommand()`
