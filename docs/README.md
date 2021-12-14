# SignalFx Tracing Library for .NET

The SignalFx Tracing Library for .NET provides an
OpenTracing-compatible tracer and automatically configured instrumentations
for popular .NET libraries and frameworks.

Where applicable, context propagation uses
[B3 headers](https://github.com/openzipkin/b3-propagation).

---

## Requirements

## Supported .NET versions

- .NET Core 3.1, .NET 5.0 and higher on Windows and Linux
- .NET Framework 4.6.2 and higher on Windows

## Supported libraries and frameworks

<!-- markdownlint-disable MD013 -->

| Library | Versions Supported | Notes |
| ---     | ---                | ---   |
| Elasticsearch.Net | `Elasticsearch.Net` NuGet 5.3 - 7.x | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES=false` (`true` by default, which may introduce overhead for direct streaming users). |
| HttpClient | Supported .NET versions | by way of `System.Net.Http.HttpClientHandler` and `HttpMessageHandler` instrumentations |
| Npgsql | `Npqsql` NuGet 4.0+ | Provided via enhanced ADO.NET instrumentation |
| ServiceStack.Redis | `ServiceStack.Redis` NuGet 4.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| StackExchange.Redis | `StackExchange.Redis` NuGet 1.0+ | Disable `db.statement` tagging with `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS=false` (`true` by default). |
| WebClient | Supported .NET versions | by way of `System.Net.WebRequest` instrumentation |

<!-- markdownlint-enable MD013 -->

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

1. Optionally, enable trace injection in logs:

   ```batch
   setx SIGNALFX_LOGS_INJECTION true /m
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

1. Optionally, enable trace injection in logs:

    ```bash
    export SIGNALFX_LOGS_INJECTION=true
    ```

1. Optionally, create the default logging directory:

    ```bash
    /opt/signalfx/createLogPath.sh
    ```

1. Run your application, e.g.:

    ```bash
    dotnet run
    ```

## Manually instrument an application

You can build upon the provided tracing functionality by modifying and adding
to automatically generated traces. The OpenTelemetry Tracing library for .NET
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

1. Obtain the `OpenTracing.Util.GlobalTracer` instance and create spans that
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

Further reading:

- [OpenTracing documentation](https://github.com/opentracing/opentracing-csharp)
- [OpenTracing terminology](https://github.com/opentracing/specification/blob/master/specification.md)

## Advanced configuration

See [advanced-config.md](advanced-config.md).

## Correlating traces with logs

The SignalFx-Tracing Library for .NET implements the
[Profiling API](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
and should only require basic configuration of your application environment.

You can link individual log entries with trace IDs and span IDs associated with
corresponding events. If your application uses a supported logger, enable trace
injection to automatically include trace context in your application's logs.
For more information, see [Inject traces in logs](../tracer/samples/AutomaticTraceIdInjection/README.md).

## Troubleshooting

Check if you are not hitting one of the issues listed below.

### Linux instrumentation not working

The proper binary needs to be selected when deploying to Linux,
eg.: the default Microsoft .NET images are based on Debian and should use
the `deb` package, see the [Linux](#Linux) setup section.

If you are not sure what is the Linux distribution being used try the following commands:

```terminal
lsb_release -a
cat /etc/*release
cat /etc/issue*
cat /proc/version
```

### High CPU usage

The default installation of auto-instrumentation enables tracing all .NET processes
on the box.
In the typical scenarios (dedicated VMs or containers), this is not a problem.
Use the environment variables `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` and `SIGNALFX_PROFILER_PROCESSES`
to include/exclude applications from the tracing auto-instrumentation.
These are ";" delimited lists that control the inclusion/exclusion of processes.

### Investigating other issues

If none of the suggestions above solves your issue, detailed logs are necessary.
Follow the steps below to get the detailed logs from
SignalFx Tracing Library for .NET.

Set the environment variable `SIGNALFX_TRACE_DEBUG` to `true` before the instrumented process starts.
By default, the library writes the log files under the below predefined locations.
If needed, change the default location by updating the environment variable `SIGNALFX_TRACE_LOG_DIRECTORY` to an appropriate path.
On Linux, the default log location is `/var/log/signalfx/dotnet/`
On Windows, the default log location is `%ProgramData%\SignalFx .NET Tracing\logs\`
Compress the whole folder to capture the multiple log files and send
the compressed folder to us.
After obtaining the logs, remember to remove the environment variable
`SIGNALFX_TRACE_DEBUG` to avoid unnecessary overhead.

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) before creating an issue or
a pull request.

## License

The SignalFx Tracing Library is a redistribution of the
[.NET Tracer for Datadog APM](https://github.com/DataDog/dd-trace-dotnet).
It is licensed under the terms of the Apache Software License version 2.0.
For more details, see [the license file](../LICENSE).

The third-party dependencies list can be found [here](../LICENSE-3rdparty.csv).
