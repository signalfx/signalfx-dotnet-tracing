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
| Confluent.Kafka | `Confluent.Kafka` NuGet [1.4.0, 2) | |
| Elasticsearch.Net | `Elasticsearch.Net` NuGet 5.3 - 7.x | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES=false` (`true` by default, which may introduce overhead for direct streaming users). |
| GraphQL | `GraphQL` NuGet [2.3, 3) | Currently only instruments validation and execution functionality. |
| HttpClient | Supported .NET versions | by way of `System.Net.Http.HttpClientHandler` and `HttpMessageHandler` instrumentations |
| MongoDB | `MongoDB.Driver.Core` NuGet 2.1.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS=false` (`true` by default). |
| Npgsql | `Npqsql` NuGet 4.0+ | Provided via enhanced ADO.NET instrumentation |
| RabbitMQ | `RabbitMQ.Client` NuGet [3.6.9, 7) | |
| ServiceStack.Redis | `ServiceStack.Redis` NuGet 4.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| StackExchange.Redis | `StackExchange.Redis` NuGet 1.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| WCF (Server) | `System.ServiceModel` 4.x | Client requests using `WSHttpBinding` or `BasicHttpBinding` are instrumented via the `WebClient` instrumentation. There is no client-side span for `NetTcpBinding`. |
| WebClient | Supported .NET versions | by way of `System.Net.WebRequest` instrumentation |

## Configure the SignalFx Tracing Library for .NET

### Configuration values

Use these environment variables to configure the tracing library:

| Environment variable | Default value | Description |
|-|-|-|
| `SIGNALFX_ACCESS_TOKEN` |  | The access token for your SignalFx organization. Providing a token enables you to send traces to a SignalFx ingest endpoint. |
| `SIGNALFX_ADD_CLIENT_IP_TO_SERVER_SPANS` | `false` | Enable to add the client IP as a span tag when creating a server span. |
| `SIGNALFX_APPEND_URL_PATH_TO_NAME` | `false` | Enable to append the absolute URI path to the span name. |
| `SIGNALFX_ASPNET_TEMPLATE_NAMES_ENABLED` | `true` |  Feature Flag: enables updated resource names on `aspnet.request`, `aspnet-mvc.request`, `aspnet-webapi.request`, and `aspnet_core.request` spans. Enables `aspnet_core_mvc.request` spans and additional features on `aspnet_core.request` spans. |
| `SIGNALFX_DIAGNOSTIC_SOURCE_ENABLED` | `true` | Enable to generate troubleshooting logs with the `System.Diagnostics.DiagnosticSource` class. |
| `SIGNALFX_DISABLED_INTEGRATIONS` |  | The integrations you want to disable, if any, separated by a semi-colon. These are the supported integrations: AspNetMvc, AspNetWebApi2, DbCommand, ElasticsearchNet5, ElasticsearchNet6, GraphQL, HttpMessageHandler, IDbCommand, MongoDb, NpgsqlCommand, OpenTracing, ServiceStackRedis, SqlCommand, StackExchangeRedis, Wcf, WebRequest |
| `SIGNALFX_DOTNET_TRACER_CONFIG_FILE` | ```%WorkingDirectory%/signalfx.json``` | The file path of a JSON configuration file that will be loaded. |
| `SIGNALFX_ENDPOINT_URL` | `http://localhost:9080/v1/trace` | The hostname and port for a SignalFx Smart Agent or OpenTelemetry Collector. |
| `SIGNALFX_ENV` |  | The value for the `environment` tag added to every span. This determines the environment in which the service is available in SignalFx µAPM.  |
| `SIGNALFX_FILE_LOG_ENABLED` | `true` | Enable file logging. This is enabled by default. |
| `SIGNALFX_INSTRUMENTATION_ASPNETCORE_DIAGNOSTIC_LISTENERS` |  | Comma-separated list of diagnostic listeners that you subscribe to an observer. |
| `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES` |  | Enable to tag the Elasticsearch command `PostData` as a `db.statement`. |
| `SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS` |  | Enable to tag the Mongo command `BsonDocument` as a `db.statement`. |
| `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS` |  | Enable to tag Redis commands as a `db.statement`. |
| `SIGNALFX_LOGS_INJECTION` | `false` | Enable to inject trace IDs, span IDs, service name and environment into logs. This requires a compatible logger or manual configuration. |
| `SIGNALFX_MAX_LOGFILE_SIZE` | `104857600` (10MiB) | The maximum size for tracer log files, in bytes. |
| `SIGNALFX_OUTBOUND_HTTP_EXCLUDED_HOSTS` |  | A semicolon-separated list of hosts for which HTTP outbound spans are not created. |
| `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` |  | Sets the filename of executables the profiler cannot attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with semi-colons, for example: `MyApp.exe;dotnet.exe` |
| `SIGNALFX_PROFILER_PROCESSES` |  | Sets the filename of executables the profiler can attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with semi-colons, for example: `MyApp.exe;dotnet.exe` |
| `SIGNALFX_PROPAGATOR` | `B3` | Sets the context propagation format. If not defined (default), the propagation format will be set to B3. The other option is `W3C`, which is recommended to get compatibility with other OpenTelemetry instrumentation. |
| `SIGNALFX_RECORDED_VALUE_MAX_LENGTH` | `1200` | The maximum length an attribute value can have. Values longer than this are truncated. |
| `SIGNALFX_SANITIZE_SQL_STATEMENTS` |  | Enable to stop sanitizing each SQL `db.statement`. |
| `SIGNALFX_SERVICE_NAME_PER_SPAN_ENABLED` |  | Enable to allow manual instrumentation to have a different service name than the one you specify with `SIGNALFX_SERVICE_NAME`.  Add a tag `service.name` with the desired name to the manual instrumentation. |
| `SIGNALFX_SERVICE_NAME` |  | The name of the service. |
| `SIGNALFX_STDOUT_LOG_ENABLED` | `false` | Enables `stdout` logging. This is disabled by default. |
| `SIGNALFX_SYNC_SEND` | `false` | Enable to send spans in synchronous mode when the root span is closed. Sending spans in synchronous mode is generally recommended for only tests, but can also be useful for some special scenarios.|
| `SIGNALFX_TRACE_DEBUG` | `false` | Enable to activate debugging mode for the tracer. |
| `SIGNALFX_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION` | `false` |  Sets whether to intercept method calls when the caller method is inside a domain-neutral assembly. This is recommended when instrumenting IIS applications. |
| `SIGNALFX_TRACE_GLOBAL_TAGS` |  | Comma-separated list of key-value pairs to specify global span tags. For example: `"key1:val1,key2:val2"` |
| `SIGNALFX_TRACE_LOG_PATH` | Linux: `/var/log/signalfx/dotnet/dotnet-profiler.log`<br>Windows: `%ProgramData%"\SignalFx .NET Tracing\logs\dotnet-profiler.log` | The path of the profiler log file. |
| `SIGNALFX_TRACE_RESPONSE_HEADER_ENABLED` | `true` | If set to true enables adding `Server-Timing` header to the server HTTP responses. |
| `SIGNALFX_TRACING_ENABLED` | `true` | Enable to activate the tracer. |
| `SIGNALFX_USE_WEBSERVER_RESOURCE_AS_OPERATION_NAME` | `true` | Enable to specify the resource name as the span name. This applies only to AspNetMvc and AspNetWebApi. |

## Ways to configure

There are following ways to apply configuration settings (priority is from first to last):

1. [Environment variables)(#environment-variables)
2. [`web.config` or `app.config` file](#web.config-and-app.config)
3. [JSON configuration file](#json-configuration-file)

### Environment variables

Environment variables are the main way to configure values.  A setting configured via an environment variable cannot be overridden.

### Web.config and app.config

For an application running on .NET Framework, web configuration file (`web.config`) or application configuration file (`app.config`) can be used to configure settings.

See example with `SIGNALFX_SERVICE_NAME` overload.

```xml
<configuration>
  <appSettings>
    <add key="SIGNALFX_SERVICE_NAME" value="my-service-name" />
  </appSettings>
</configuration>
```

### Json configuration file

By default, if `SIGNALFX_DOTNET_TRACER_CONFIG_FILE` is unset, the application is searching for `signalfx.json` in the current working directory (acquired by [`Environment.CurrentDirectory`](https://docs.microsoft.com/en-us/dotnet/api/system.environment.currentdirectory?view=net-5.0)).

See example with `SIGNALFX_SERVICE_NAME` overload.

```json
{
    "SIGNALFX_SERVICE_NAME": "my-service-name"
}
```

## Setup

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
ARG TRACER_VERSION=0.1.15
ADD https://github.com/signalfx/signalfx-dotnet-tracing/releases/download/v${TRACER_VERSION}/signalfx-dotnet-tracing_${TRACER_VERSION}_amd64.deb /signalfx-package/signalfx-dotnet-tracing.deb
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

**Warning**: Pay close attention to the scope of environment variables. Ensure they
are properly set prior to launching the targeted process. The following steps set the
environment variables at the machine level, with the exception of the variables used
for finer control of which processes will be instrumented, which are set in the current
command session.

1. Install the CLR Profiler using an installer file (`.msi` file) from the
[latest release](https://github.com/signalfx/signalfx-dotnet-tracing/releases/latest).
Choose the installer (x64 or x86) according to the architecture of the application
you're instrumenting.
2. Configure the required environment variables to enable the CLR Profiler:
    - For .NET Framework applications:
    ```batch
    setx COR_PROFILER "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}" /m
    ```
   - For .NET Core applications:
   ```batch
   setx CORECLR_PROFILER "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}" /m
   ```
3. Set the "service name" that better describes your application:
   ```batch
   setx SIGNALFX_SERVICE_NAME MyServiceName /m
   ```
4. Set the endpoint of a Smart Agent or OpenTelemetry Collector that will forward
the trace data:
   ```batch
   setx SIGNALFX_ENDPOINT_URL http://localhost:9080/v1/trace /m
   ```
5. Optionally, enable trace injection in logs:
   ```batch
   setx SIGNALFX_LOGS_INJECTION true /m
   ```
6. Optionally, if instrumenting IIS applications add the following environmet variable set to `true`:
    ```batch
    setx SIGNALFX_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION true /m
    ```
7. Enable instrumentation for the targeted application by setting
the appropriate __CLR enable profiling__ environment variable.
You can enable instrumentation at these levels:
 - For current command session
 - For a specific Windows Service
 - For a specific user
The follow snippet describes how to enable instrumentation for
the current command session according to the .NET runtime.
To enable instrumentation at different levels, see
[this](#enable-instrumentation-at-different-levels) section.
   - For .NET Framework applications:
   ```batch
   set COR_ENABLE_PROFILING=1
   ```
   - For .NET Core applications:
   ```batch
   set CORECLR_ENABLE_PROFILING=1
   ```
8. Restart your application ensuring that all environment variables above are properly
configured. If you need to check the environment variables for a process use a tool
like [Process Explorer](https://docs.microsoft.com/en-us/sysinternals/downloads/process-explorer).

#### Enable instrumentation at different levels

Enable instrumentation for a specific Windows service:
   - For .NET Framework applications:
   ```batch
   reg add HKLM\SYSTEM\CurrentControlSet\Services\<ServiceName>\Environment /v COR_ENABLE_PROFILING /d 1
   ```
   - For .NET Core applications:
   ```batch
   reg add HKLM\SYSTEM\CurrentControlSet\Services\<ServiceName>\Environment /v CORECLR_ENABLE_PROFILING /d 1
   ```

Enable instrumentation for a specific user:
   - For .NET Framework applications:
   ```batch
   setx /s %COMPUTERNAME% /u <[domain/]user> COR_ENABLE_PROFILING 1
   ```
   - For .NET Core applications:
   ```batch
   setx /s %COMPUTERNAME% /u <[domain/]user> CORECLR_ENABLE_PROFILING 1
   ```

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
    <PackageReference Include="OpenTracing" Version="0.12.0" />
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

## Troubleshooting

Check if you are not hitting one of the issues listed below.

### IIS applications not instrumenting expected services

Set the environment variable `SIGNALFX_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION` to `true` - without it
the CLR profiler can't instrument many libraries/frameworks under IIS.

### Linux instrumentation not working

The proper binary needs to be selected when deploying to Linux, eg.: the default Microsoft .NET images are
based on Debian and should use the `deb` package, see the [Linux](#Linux) setup section.

If you are not sure what is the Linux distribution being used try the following commands:
```terminal
$ lsb_release -a
$ cat /etc/*release
$ cat /etc/issue*
$ cat /proc/version
```

### High CPU usage

The default installation of auto-instrumentation enables tracing all .NET processes on the box.
In the typical scenarios (dedicated VMs or containers), this is not a problem.
Use the environment variables `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` and `SIGNALFX_PROFILER_PROCESSES`
to include/exclude applications from the tracing auto-instrumentation.
These are ";" delimited lists that control the inclusion/exclusion of processes.

### Custom instrumentation not being captured

If the code accessing `GlobalTracer.Instance` executes before any auto-instrumentation is injected
into the process the call to `GlobalTracer.Instance` will return the OpenTracing No-Operation tracer.
In this case it is necessary to force the injection of the SignalFx tracer by running a method like the one below
before accessing `GlobalTracer.Instance`.

```c#
        static void InitTracer()
        {
            try
            {
                Assembly tracingAssembly = Assembly.Load(new AssemblyName("SignalFx.Tracing, Culture=neutral, PublicKeyToken=def86d061d0d2eeb"));
                Type tracerType = tracingAssembly.GetType("SignalFx.Tracing.Tracer");

                PropertyInfo tracerInstanceProperty = tracerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                object tracerInstance = tracerInstanceProperty.GetValue(null);
            }
            catch (Exception ex)
            {
                // TODO: Replace Console.WriteLine with proper log of the application.
                Console.WriteLine("Unable to load SignalFx.Tracing.Tracer library. Exception: {0}", ex);
            }
        }
```

### Investigating other issues

If none of the suggestions above solves your issue, detailed logs are necessary.
Follow the steps below to get the detailed logs from SignalFx Tracing for .NET:

Set the environment variable `SIGNALFX_TRACE_DEBUG` to `true` before the instrumented process starts.
By default, the library writes the log files under the below predefined locations.
If needed, change the default location by updating the environment variable `SIGNALFX_TRACE_LOG_PATH` to an appropriate path. 
On Linux, the default log location is `/var/log/signalfx/dotnet/`
On Windows, the default log location is `%ProgramData%\SignalFx .NET Tracing\logs\`
Compress the whole folder to capture the multiple log files and send the compressed folder to us.
After obtaining the logs, remember to remove the environment variable `SIGNALFX_TRACE_DEBUG` to avoid unnecessary overhead.

## Contributing

See [docs/README.md](docs/README.md) and [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md).

## About

The SignalFx-Tracing Library for .NET is a fork of the [.NET Tracer for Datadog APM](https://github.com/DataDog/dd-trace-dotnet)
that has been modified to provide Zipkin v2 JSON formatting, a complete OpenTracing API implementation, B3 propagation,
and properly annotated trace data for handling by 
[SignalFx Microservices APM](https://docs.signalfx.com/en/latest/apm/apm-overview/index.html).
