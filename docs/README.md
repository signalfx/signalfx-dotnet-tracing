# SignalFx Instrumentation for .NET

The SignalFx Instrumentationy for .NET provides automatic instrumentations
for popular .NET libraries and frameworks.

The SignalFx Instrumentation for .NET is a [.NET Profiler](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/profiling-overview)
which instruments supported libraries and frameworks with bytecode manipulation.

By default:

- all spans are sampled and reported,
- [B3 headers](https://github.com/openzipkin/b3-propagation) are used for context
  propagation,
- Zipkin trace exporter is used to send spans as JSON in the [Zipkin v2 format](https://zipkin.io/zipkin-api/#/default/post_spans).

The SignalFx Instrumentationy for .NET registers an OpenTracing `GlobalTracer`
so you can support existing custom instrumentation or add custom
instrumentation to your application later.

Whenever possible, SignalFx Instrumentation for .NET complies
to the [OpenTelemetry Trace Semantic Conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/trace/semantic_conventions).
[OpenTelemetry Collector Contrib](https://github.com/open-telemetry/opentelemetry-collector-contrib)
with its [Zipkin Receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/zipkinreceiver)
can be used to receive, process and export telemetry data.
However, until [OpenTelemetry .NET Auto-Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation)
is not useable, SignalFx Instrumentation for .NET is not able
to correlated the spans created with [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
and [`ActivitySource`](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs).

---

## Requirements

### Supported .NET versions

- .NET Core 3.1, .NET 5.0 and higher on Windows and Linux
- .NET Framework 4.6.2 and higher on Windows

## Supported libraries and frameworks

| Library | Versions Supported | Notes |
| ---     | ---                | ---   |
| Elasticsearch.Net | `Elasticsearch.Net` NuGet 5.3 - 7.x | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES=false` (`true` by default, which may introduce overhead for direct streaming users). |
| HttpClient | Supported .NET versions | by way of `System.Net.Http.HttpClientHandler` and `HttpMessageHandler` instrumentations |
| Npgsql | `Npqsql` NuGet 4.0+ | Provided via enhanced ADO.NET instrumentation |
| ServiceStack.Redis | `ServiceStack.Redis` NuGet 4.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| StackExchange.Redis | `StackExchange.Redis` NuGet 1.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| WebClient | Supported .NET versions | by way of `System.Net.WebRequest` instrumentation |

## Get started

### Windows

**Warning**: Pay close attention to the scope of environment variables. Ensure they
are properly set prior to launching the targeted process. The following steps set the
environment variables at the machine level, with the exception of the variables used
for finer control of which processes will be instrumented, which are set in the current
command session.

1. Install the CLR Profiler using an installer file (`.msi` file) from the latest release.
Choose the installer (x64 or x86) according to the architecture of the operating
system where it will be running.

1. Configure the required environment variables to enable the CLR Profiler:
    - For .NET Framework applications:

    ```batch
    setx COR_PROFILER "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}" /m
    ```

   - For .NET Core applications:

   ```batch
   setx CORECLR_PROFILER "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}" /m
   ```

1. Set the service name:

   ```batch
   setx SIGNALFX_SERVICE_NAME my-service-name /m
   ```

1. Set the trace endpoint, e.g. [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector):

   ```batch
   setx SIGNALFX_ENDPOINT_URL http://localhost:9411/api/v2/spans /m
   ```

1. Enable instrumentation for the targeted application by setting
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

1. Restart your application ensuring that all environment variables above are properly
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

### Linux

After downloading the library, install the CLR Profiler and its components
via your system's package manager.

1. Download the latest release of the library.

1. Install the CLR Profiler and its components with your system's package
manager:

    ```bash
    # Use dpkg:
    dpkg -i signalfx-dotnet-tracing.deb

    # Use rpm:
    rpm -ivh signalfx-dotnet-tracing.rpm

    # Install directly from the release bundle:
    tar -xf signalfx-dotnet-tracing.tar.gz -C /

    # Install directly from the release bundle for musl-using systems (Alpine Linux):
    tar -xf signalfx-dotnet-tracing-musl.tar.gz -C /
    ```

1. Configure the required environment variables:

    ```bash
    source /opt/signalfx/defaults.env
    ```

1. Set the service name:

    ```bash
    export SIGNALFX_SERVICE_NAME='my-service-name'
    ```

1. Set the trace endpoint, e.g. [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector):

    ```bash
    export SIGNALFX_ENDPOINT_URL='http://<YourCollector>:9411/api/v2/spans'
    ```

1. Optionally, create the default logging directory:

    ```bash
    /opt/signalfx/createLogPath.sh
    ```

1. Run your application, e.g.:

    ```bash
    dotnet run
    ```

## Manual instrumentation

See [manual-instrumentation.md](manual-instrumentation.md).

## Advanced configuration

See [advanced-config.md](advanced-config.md).

## Correlating traces with logs

See [correlating-traces-with-logs.md](correlating-traces-with-logs.md).

## Troubleshooting

See [troubleshooting.md](troubleshooting.md).

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) before creating an issue or
a pull request.

## License

The SignalFx Instrumentation for .NET is a redistribution of the
[.NET Tracer for Datadog APM](https://github.com/DataDog/dd-trace-dotnet).
It is licensed under the terms of the Apache Software License version 2.0.
For more details, see [the license file](../LICENSE).

The third-party dependencies list can be found [here](../LICENSE-3rdparty.csv).
