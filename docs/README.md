# SignalFx Instrumentation for .NET

The SignalFx Instrumentationy for .NET provides automatic instrumentations
for popular .NET libraries and frameworks.

The SignalFx Instrumentation for .NET is a [.NET Profiler](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/profiling-overview)
which instruments supported libraries and frameworks with bytecode manipulation
to capture and send telemetry data (metrics, traces, and logs).

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
to correlate the spans created with [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
and [`ActivitySource`](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs).

---

## Requirements

### Supported .NET versions

- .NET Core 3.1, .NET 5.0 and higher on Windows and Linux (except Alpine Linux)
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

Make sure you set up the [Splunk OpenTelemtry Collector](https://github.com/signalfx/splunk-otel-collector)
to receive telemetry data.

### Installation

You can find the latest installation packages on the
[Releases](https://github.com/signalfx/signalfx-dotnet-tracing/releases/latest)
page.

| File         | Operating system    | Architecture | Install command | Notes |
| ---           | ---                 | ---          | ---          | ---   |
| `x86_64.rpm`  | Red Hat-based Linux distributions | x64 | `rpm -ivh signalfx-dotnet-tracing.rpm` | RPM package |
| `x64.msi`     | Windows 64-bit | x64 |  `msiexec /i signalfx-dotnet-tracing-x64.msi /quiet` | |
| `x86.msi`     | Windows 32-bit | x86 | `msiexec /i signalfx-dotnet-tracing-x86.msi /quiet` | |
| `tar.gz` | Linux distributions using [qlibc](https://wiki.musl-libc.org/projects-using-musl.html) | x64 | `tar -xf signalfx-dotnet-tracing.tar.gz -C /` | Currently, all [officially supported Linux distribtions](https://docs.microsoft.com/dotnet/core/install/linux) except Alpine use glibc |
| `amd64.deb`   | Debian-based Linux distributions | x64 | `dpkg -i signalfx-dotnet-tracing.debm` | DEB package |
<!-- TODO: | `musl.tar.gz` | x64 Linux distributions using [musl](https://wiki.musl-libc.org/projects-using-musl.html) | x64 | `tar -xf signalfx-dotnet-tracing-musl.tar.gz -C /` | Alpine Linux uses musl | -->

On Linux, after the installation, you can optionally create the log directory:

```bash
/opt/signalfx/createLogPath.sh
```

### Instrument a .NET application on Windows

```powershell
$Env:COR_ENABLE_PROFILING = "1"                                   # Enable the .NET Framework Profiler
$Env:COR_PROFILER = "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"      # Select the .NET Framework Profiler
$Env:CORECLR_ENABLE_PROFILING = "1"                               # Enable the .NET (Core) Profiler
$Env:CORECLR_PROFILER = "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"  # Select the .NET (Core) Profiler
# Now the autoinstrumentation is configured in this shell session.
# You can set additional settings and run your application, for example:
$Env:SIGNALFX_SERVICE_NAME = "my-service-name"                    # Set the service name
dotnet run                                                        # Run your application                                                     
```

### Instrument a .NET application on Linux

```bash
export CORECLR_ENABLE_PROFILING="1"                                                 # Enable the .NET (Core) Profiler
export CORECLR_PROFILER="{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"                    # Select the .NET (Core) Profiler
export CORECLR_PROFILER_PATH="/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so" # Select the .NET (Core) Profiler file path
export SIGNALFX_DOTNET_TRACER_HOME="/opt/signalfx"                                  # Select the SignalFx Instrumentation for .NET home folder
# Now the autoinstrumentation is configured in this shell session.
# You can set additional settings and run your application, for example:
export SIGNALFX_SERVICE_NAME="my-service-name"  # Set the service name
dotnet run                                      # Run your application 
```

### Instrument a Windows Service running a .NET application running

<!-- TODO:

Update this section to use a PowerShell script that sets all of the following env vars:
COR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
COR_ENABLE_PROFILING=1
CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
CORECLR_ENABLE_PROFILING=1
-->

Enable instrumentation for a specific Windows service:

- For .NET Framework applications:

   ```batch
   reg add HKLM\SYSTEM\CurrentControlSet\Services\<ServiceName>\Environment /v COR_ENABLE_PROFILING /d 1
   ```

- For .NET or .NET Core applications:

   ```batch
   reg add HKLM\SYSTEM\CurrentControlSet\Services\<ServiceName>\Environment /v CORECLR_ENABLE_PROFILING /d 1
   ```

### Instrument an ASP.NET application deployed on IIS

<!-- TODO -->

## Advanced configuration

See [advanced-config.md](advanced-config.md).

## Manual instrumentation

See [manual-instrumentation.md](manual-instrumentation.md).

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
