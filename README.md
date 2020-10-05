# SignalFx Tracing Library for .NET

The SignalFx Tracing Library for .NET provides an
OpenTracing-compatible tracer and automatically configured instrumentations
for popular .NET libraries and frameworks.  It supports .NET Core 2.0+ on
Linux and Windows and .NET Framework 4.5+ on Windows.

Where applicable, context propagation uses
[B3 headers](https://github.com/openzipkin/b3-propagation).

The SignalFx-Tracing Library for .NET implements the
[Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
and should only require basic configuration of your application environment.

You can link individual log entries with trace IDs and span IDs associated with
corresponding events. If your application uses a supported logger, enable trace
injection to automatically include trace context in your application's logs.
For more information, see [Inject traces in logs](/customer-samples/AutomaticTraceIdInjection/README.md).

## Supported libraries and frameworks

There are [known .NET Core runtime issues](https://github.com/dotnet/coreclr/issues/18448)
for version 2.1.0 and 2.1.2.

| Library | Versions Supported | Notes |
| ---     | ---                | ---   |
| ADO.NET | Supported .NET versions | Disable sanitization of `db.statement` with `SIGNALFX_SANITIZE_SQL_STATEMENTS=false` (`true` by default) |
| ASP.NET Core MVC | 2.0+ | `Microsoft.AspNet.Mvc.Core` NuGet and built-in packages.  Include additional applicable Diagnostic Listeners with `SIGNALFX_INSTRUMENTATION_ASPNETCORE_DIAGNOSTIC_LISTENERS='Listener.One,Listener.Two'` |
| ASP.NET MVC on .NET Framework | `System.Web.Mvc` 4.x and 5.x | |
| ASP.NET Web API 2 on .NET Framework | `System.Web.Http` 5.1+ | |  
| Elasticsearch.Net | `Elasticsearch.Net` NuGet 5.3 - 6.x | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES=false` (`true` by default, which may introduce overhead for direct streaming users). |
| HttpClient | Supported .NET versions | by way of `System.Net.Http.HttpClientHandler` and `HttpMessageHandler` instrumentations |
| GraphQL | `GraphQL` NuGet [2.3, 3) | Currently only instruments validation and execution functionality. |
| MongoDB | `MongoDB.Driver.Core` NuGet 2.1.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS=false` (`true` by default). |
| Npgsql | `Npqsql` NuGet 4.0+ | Provided via enhanced ADO.NET instrumentation |
| ServiceStack.Redis | `ServiceStack.Redis` NuGet 4.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| StackExchange.Redis | `StackExchange.Redis` NuGet 1.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| WebClient | Supported .NET versions | by way of `System.Net.WebRequest` instrumentation |
| WCF (Server) | `System.ServiceModel` 4.x | Client requests using `WSHttpBinding` or `BasicHttpBinding` are instrumented via the `WebClient` instrumentation. There is no client-side span for `NetTcpBinding`. |

## Configure the SignalFx Tracing Library for .NET

### Configuration values

Use these environment variables to configure the tracing library:

| Environment variable | Default value | Description |
|-|-|-|
| `SIGNALFX_ENV` |  | The value for the `env` tag added to every span. This determines the environment in which the service is available in SignalFx µAPM.  |
| `SIGNALFX_SERVICE_NAME` |  | The name of the service. |
| `SIGNALFX_SERVICE_NAME_PER_SPAN_ENABLED` |  | Enable to allow manual instrumentation to have a different service name than the one you specify with `SIGNALFX_SERVICE_NAME`.  Add a tag `service.name` with the desired name to the manual instrumentation. |
| `SIGNALFX_TRACING_ENABLED` | `true` | Enable to activate the tracer. |
| `SIGNALFX_TRACE_DEBUG` | `false` | Enable to activate debugging mode for the tracer. |
| `SIGNALFX_ENDPOINT_URL` | `http://localhost:9080/v1/trace` | The hostname and port for a SignalFx Smart Agent or OpenTelemetry Collector. |
| `SIGNALFX_ACCESS_TOKEN` |  | The access token for your SignalFx organization. Providing a token enables you to send traces to a SignalFx ingest endpoint. |
| `SIGNALFX_TRACE_GLOBAL_TAGS` |  | Comma-separated list of key-value pairs to specify global span tags. For example: `"key1:val1,key2:val2"` |
| `SIGNALFX_LOGS_INJECTION` | `false` | Enable to inject trace IDs and span IDs in logs. This requires a compatible logger or manual configuration. |
| `SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS` |  | Enable to tag the Mongo command `BsonDocument` as a `db.statement`. |
| `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES` |  | Enable to tag the Elasticsearch command `PostData` as a `db.statement`. |
| `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS` |  | Enable to tag Redis commands as a `db.statement`. |
| `SIGNALFX_SANITIZE_SQL_STATEMENTS` |  | Enable to stop sanitizing each SQL `db.statement`. |
| `SIGNALFX_INSTRUMENTATION_ASPNETCORE_DIAGNOSTIC_LISTENERS` |  | Comma-separated list of diagnostic listeners that you subscribe to an observer. |
| `SIGNALFX_MAX_LOGFILE_SIZE` | `10 MB` | The maximum size for tracer log files, in bytes. |
| `SIGNALFX_TRACE_LOG_PATH` | Linux: `/var/log/signalfx/dotnet/dotnet-profiler.log`<br>Windows: `%ProgramData%"\SignalFx .NET Tracing\logs\dotnet-profiler.log` | The path of the profiler log file. |
| `SIGNALFX_APPEND_URL_PATH_TO_NAME` | `false` | Enable to append the absolute URI path to the span name. |
| `SIGNALFX_USE_WEBSERVER_RESOURCE_AS_OPERATION_NAME` | `true` | Enable to specify the resource name as the span name. This applies only to AspNetMvc and AspNetWebApi. |
| `SIGNALFX_ADD_CLIENT_IP_TO_SERVER_SPANS` | `false` | Enable to add the client IP as a span tag when creating a server span. |
| `SIGNALFX_DIAGNOSTIC_SOURCE_ENABLED` | `true` | Enable to generate troubleshooting logs with the `System.Diagnostics.DiagnosticSource` class. |
| `SIGNALFX_DISABLED_INTEGRATIONS` |  | The integrations you want to disable, if any, separated by a semi-colon. These are the supported integrations: AspNetMvc, AspNetWebApi2, DbCommand, ElasticsearchNet5, ElasticsearchNet6, GraphQL, HttpMessageHandler, IDbCommand, MongoDb, NpgsqlCommand, OpenTracing, ServiceStackRedis, SqlCommand, StackExchangeRedis, Wcf, WebRequest |
| `SIGNALFX_RECORDED_VALUE_MAX_LENGTH` | `1200` | The maximum length an attribute value can have. Values longer than this are truncated. |

### Linux

After downloading the library, install the CLR Profiler and its components
via your system's package manager.

1. Download the [latest release](https://github.com/signalfx/signalfx-dotnet-tracing/releases/latest)
of the library.
2. Install the CLR Profiler and its components with your system's package
manager:
    ```bash
    # Use dpkg:
    $ dpkg -i signalfx-dotnet-tracing.deb

    # Use rpm:
    $ rpm -ivh signalfx-dotnet-tracing.rpm

    # Install directly from the release bundle:
    $ tar -xf signalfx-dotnet-tracing.tar.gz -C /

    # Install directly from the release bundle for musl-using systems (Alpine Linux):
    $ tar -xf signalfx-dotnet-tracing-musl.tar.gz -C /
    ```
3. Configure the required environment variables:
    ```bash
    $ source /opt/signalfx-dotnet-tracing/defaults.env
    ```
4. Set the service name:
    ```bash
    $ export SIGNALFX_SERVICE_NAME='MyCoreService'
    ```
5. Set the endpoint of a Smart Agent or OpenTelemetry Collector:
    ```bash
    $ export SIGNALFX_ENDPOINT_URL='http://<YourSmartAgentOrCollector>:9080/v1/trace'
    ```
6. Optionally, enable trace injection in logs:
    ```bash
    $ export SIGNALFX_LOGS_INJECTION=true
    ```
7. Optionally, create the default logging directory:
    ```bash
    $ source /opt/signalfx-dotnet-tracing/createLogPath.sh
    ```
8. Run your application:
    ```bash
    $ dotnet run
    ```

### Azure Function Custom Linux Docker Image

The CLR Profiler can be added to Docker images using any of the provided packages,
below an example of adding it to an Azure Function image:

```dockerfile
# These first lines are from the example but in principle could be
# build outside of docker, but they make the example clear showing
# what should be copied to the Azure VM machine.
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS installer-env

COPY . /src/dotnet-function-app
RUN cd /src/dotnet-function-app && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet:3.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Custom lines Adding the SignalFx Auto-Instrumentation to the image:

# First install the package. This example downloads the latest version
# alternatively download a specific version or use a local copy.
ADD https://github.com/signalfx/signalfx-dotnet-tracing/releases/latest/download/signalfx-dotnet-tracing.deb  /signalfx-package/signalfx-dotnet-tracing.deb
RUN dpkg -i /signalfx-package/signalfx-dotnet-tracing.deb
RUN rm -rf /signalfx-package

# Prepare the log directory (useful for local tests).
RUN mkdir -p /var/log/signalfx/dotnet && \
    chmod a+rwx /var/log/signalfx/dotnet

# Set the required environment variables. In the case of Azure Functions more
# can be set either here or on the application settings. 
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER='{B4C89B0F-9908-4F73-9F59-0D77C5A06874}' \
    CORECLR_PROFILER_PATH=/opt/signalfx-dotnet-tracing/SignalFx.Tracing.ClrProfiler.Native.so \
    SIGNALFX_INTEGRATIONS=/opt/signalfx-dotnet-tracing/integrations.json \
    SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx-dotnet-tracing
# End of SignalFx customization.

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
```

For more information on how to configure a custom Linux Azure Function image refer to
the [Azure documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-linux-custom-image?tabs=bash%2Cportal&pivots=programming-language-csharp).

### Windows

1. Install the CLR Profiler using an installer file (`.msi` file) from the
[latest release](https://github.com/signalfx/signalfx-dotnet-tracing/releases/latest).
Choose the installer (x64 or x86) according to the architecture of the application
you're instrumenting.
2. Configure the required environment variables to enable the CLR Profiler:
    - For .NET Framework applications:
    ```batch
    set COR_ENABLE_PROFILING=1
    set COR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874} 
    ```
   - For .NET Core applications:
   ```batch
   set CORECLR_ENABLE_PROFILING=1
   set CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
   ```
3. Set the "service name" that better describes your application:
   ```batch
   set SIGNALFX_SERVICE_NAME=MyServiceName
   ```
4. Set the endpoint of a Smart Agent or OpenTelemetry Collector that will forward
the trace data:
   ```batch
   set SIGNALFX_ENDPOINT_URL='http://<YourSmartAgentOrCollector>:9080/v1/trace'
   ```
5. Optionally, enable trace injection in logs:
   ```bash
   $ export SIGNALFX_LOGS_INJECTION=true
   ```
6. Restart your application ensuring that all environment variables above are properly
configured. If you need to check the environment variables for a process use a tool
like [Process Explorer](https://docs.microsoft.com/en-us/sysinternals/downloads/process-explorer).

## Configure custom instrumentation

You can build upon the provided tracing functionality by modifying and adding
to automatically generated traces. The SignalFx Tracing library for .NET
provides and registers an [OpenTracing-compatible](https://github.com/opentracing/opentracing-csharp)
global tracer you can use.

OpenTracing versions 0.11.0+ are supported and the provided tracer offers a
complete implementation of the OpenTracing API.

The auto-instrumentation provides a base you can build on by adding your own
custom instrumentation. By using both instrumentation approaches, you'll be
able to present a more detailed representation of the logic and functionality
of your application, clients, and framework.

1. Add the OpenTracing dependency to your project:
    ```xml
    <PackageReference Include="OpenTracing" Version="0.12.1" />
    ```
2. Obtain the `OpenTracing.Util.GlobalTracer` instance and create spans that
automatically become child spans of any existing spans in the same context:
    ```csharp
    using OpenTracing;
    using OpenTracing.Util;

    namespace MyProject
    {
        public class MyClass
        {
            public static async void MyMethod()
            {
                // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
                var tracer = GlobalTracer.Instance;

                // Create an active span that will be automatically parented by any existing span in this context
                using (IScope scope = tracer.BuildSpan("MyTracedFunctionality").StartActive(finishSpanOnDispose: true))
                {
                    var span = scope.Span;
                    span.SetTag("MyImportantTag", "MyImportantValue");
                    span.Log("My Important Log Statement");

                    var ret = await MyAppFunctionality();

                    span.SetTag("FunctionalityReturned", ret.ToString());
                }
            }
        }
    }
    ```

When using manual instrumentation it is possible to set different service names under the same process.
This is disabled by default but can be enabled by configuring the environment variable
`SIGNALFX_SERVICE_NAME_PER_SPAN_ENABLED` to `true` and following the [OpenTracing semantic
conventions](https://github.com/opentracing/specification/blob/master/semantic_conventions.md#semantic-conventions)
by setting the tag `service.name` to the desired service name.

For more examples and information on how to do manual instrumentation see:

- https://github.com/signalfx/tracing-examples/tree/master/dotnet-manual-instrumentation
- https://github.com/signalfx/tracing-examples/tree/master/signalfx-tracing/signalfx-dotnet-tracing

## About
The SignalFx-Tracing Library for .NET is a fork of the .NET
Tracer for Datadog APM that has been modified to provide Zipkin v2 JSON
formatting, a complete OpenTracing API implementation, B3 propagation, and
properly annotated trace data for handling by
[SignalFx Microservices APM](https://docs.signalfx.com/en/latest/apm/apm-overview/index.html).
