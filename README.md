# SignalFx Tracing Library for .NET Core on Linux

The SignalFx Tracing Library for .NET Core on Linux provides an
OpenTracing-compatible tracer and automatically configured instrumentations
for popular .NET Core libraries and frameworks.  It supports .NET Core 2.0+ on
Linux distributions.

Where applicable, context propagation uses
[B3 headers](https://github.com/openzipkin/b3-propagation).

The SignalFx-Tracing Library for .NET Core on Linux implements the
[Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
and should only require basic configuration of your application environment.

## Supported libraries and frameworks

All instrumentations are in beta. There are [known .NET Core runtime issues](https://github.com/dotnet/coreclr/issues/18448)
for version 2.1.0 and 2.1.2.

| Library | Versions Supported | Notes |
| ---     | ---                | ---   |
| ADO.NET | Supported .NET Core versions | Disable sanitization of `db.statement` with `SIGNALFX_SANITIZE_SQL_STATEMENTS=false` (`true` by default) |
| ASP.NET Core MVC | 2.0+ | `Microsoft.AspNet.Mvc.Core` NuGet and built-in packages.  Include additional applicable Diagnostic Listeners with `SIGNALFX_INSTRUMENTATION_ASPNETCORE_DIAGNOSTIC_LISTENERS='Listener.One,Listener.Two'` |
| Elasticsearch.Net | `Elasticsearch.Net` Nuget 5.3 - 6.x | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES=false` (`true` by default, which may introduce overhead for direct streaming users). |
| HttpClient | Supported .NET Core versions | by way of `System.Net.Http.HttpClientHandler` and `HttpMessageHandler` instrumentations |
| MongoDB | `MongoDB.Driver.Core` Nuget 2.1.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS=false` (`true` by default). |
| Npgsql | `Npqsql` Nuget 4.0+ | Provided via enhanced ADO.NET instrumentation |
| ServiceStack.Redis | `ServiceStack.Redis` Nuget 4.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| StackExchange.Redis | `StackExchange.Redis` Nuget 1.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| WebClient | Supported .NET Core versions | by way of `System.Net.WebRequest` instrumentation |

## Configure the SignalFx Tracing Library for .NET Core on Linux

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
6. Optionally, create the default logging directory:
    ```bash
    $ mkdir /var/log/signalfx
    ```
7. Run your application:
    ```bash
    $ dotnet run
    ```

## Configure custom instrumentation

You can build upon the provided tracing functionality by modifying and adding
to automatically generated traces. The SignalFx Tracing library for .NET Core
provides and registers an [OpenTracing-compatible](https://github.com/opentracing/opentracing-csharp)
global tracer you can use.

OpenTracing versions 0.12.0+ are supported and the provided tracer offers a
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

## About
The SignalFx-Tracing Library for .NET Core on Linux is a fork of the .NET
Tracer for Datadog APM that has been modified to provide Zipkin v2 JSON
formatting, a complete OpenTracing API implementation, B3 propagation, and
properly annotated trace data for handling by
[SignalFx Microservices APM](https://docs.signalfx.com/en/latest/apm/apm-overview/index.html).
