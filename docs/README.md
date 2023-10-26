>ℹ️&nbsp;&nbsp;SignalFx was acquired by Splunk in October 2019. See [Splunk SignalFx](https://www.splunk.com/en_us/investor-relations/acquisitions/signalfx.html) for more information.

# SignalFx Instrumentation for .NET

The SignalFx Instrumentation for .NET provides automatic instrumentations
for popular .NET libraries and frameworks.

The SignalFx Instrumentation for .NET is a [.NET Profiler](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/profiling-overview)
which instruments supported libraries and frameworks with bytecode manipulation
to capture and send telemetry data (metrics, traces, and logs).

By default:

- all spans are sampled and reported,
- [B3 headers](https://github.com/openzipkin/b3-propagation) and [W3C headers](https://www.w3.org/TR/trace-context/)
  are used for context propagation,
- Zipkin trace exporter is used to send spans as JSON in the [Zipkin v2 format](https://zipkin.io/zipkin-api/#/default/post_spans).

The SignalFx Instrumentation for .NET registers an OpenTracing `GlobalTracer`
so you can support existing custom instrumentation or add custom
instrumentation to your application later.

The conventions used by SignalFx Instrumentation for .NET are inspired by
the [OpenTelemetry Trace Semantic Conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/trace/semantic_conventions).
[OpenTelemetry Collector Contrib](https://github.com/open-telemetry/opentelemetry-collector-contrib)
with its [Zipkin Receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/zipkinreceiver)
can be used to receive, process and export telemetry data.
However, until [OpenTelemetry .NET Auto-Instrumentation](https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation)
is not useable, SignalFx Instrumentation for .NET is not able
to correlate the spans created with [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
and [`ActivitySource`](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs).

---

## Requirements

- .NET Core 3.1, .NET 5.0, .NET 6.0, and .NEt 7.0 on Windows and Linux
- .NET Framework 4.6.1 and higher on Windows

## Instrumented libraries and frameworks

See [instrumented-libraries.md](instrumented-libraries.md)
to know for which libraries and frameworks are instrumented.

## Get started

Make sure you set up the [Splunk OpenTelemtry Collector](https://github.com/signalfx/splunk-otel-collector)
to receive telemetry data.

### Installation

### Automated download and installation for Windows

Run in PowerShell as administrator:

```powershell
# signalfx-dotnet-tracing github repository API
$api = "https://api.github.com/repos/signalfx/signalfx-dotnet-tracing/releases/latest"

# determine OS architecture
$os_bits = (Get-CimInstance Win32_OperatingSystem).OSArchitecture
$os_arch = (&{If($os_bits -eq "64-bit") {"x64"} Else {"x86"}})

# File pattern to search for
$pattern = "signalfx-dotnet-tracing-*-$os_arch.msi"

# Find latest MSI to download
$download = (Invoke-WebRequest $api | ConvertFrom-Json).assets | Where-Object { $_.name -like $pattern } | Select-Object -Property browser_download_url,name

# Download installer MSI to Temp
$msi = Join-Path $env:temp $download.name
Invoke-WebRequest -Uri $download.browser_download_url -OutFile $msi

# Install downloaded MSI
Start-Process msiexec.exe -Wait -ArgumentList "/I $msi /quiet"

# Cleanup
Remove-Item $msi

```

### Manual installation

You can find the latest installation packages on the
[Releases](https://github.com/signalfx/signalfx-dotnet-tracing/releases/latest)
page.

| File         | Operating system    | Architecture | Install command | Notes |
| ---           | ---                 | ---          | ---          | ---   |
| `x86_64.rpm`  | Red Hat-based Linux distributions | x64 | `rpm -ivh signalfx-dotnet-tracing.rpm` | RPM package |
| `x64.msi`     | Windows 64-bit | x64 |  `msiexec /i signalfx-dotnet-tracing-x64.msi /quiet` | |
| `x86.msi`     | Windows 32-bit | x86 | `msiexec /i signalfx-dotnet-tracing-x86.msi /quiet` | |
| `tar.gz`      | Linux distributions using [glibc](https://www.gnu.org/software/libc) | x64 | `tar -xf signalfx-dotnet-tracing.tar.gz -C /opt/signalfx` | Currently, all [officially supported Linux distribtions](https://docs.microsoft.com/dotnet/core/install/linux) except Alpine use glibc |
| `amd64.deb`   | Debian-based Linux distributions | x64 | `dpkg -i signalfx-dotnet-tracing.deb` | DEB package |
| `musl.tar.gz` | Linux distributions using [musl](https://wiki.musl-libc.org/projects-using-musl.html) | x64 | `tar -xf signalfx-dotnet-tracing-musl.tar.gz -C /opt/signalfx` | Alpine Linux uses musl |

On Linux, after the installation, you can optionally create the log directory:

```bash
/opt/signalfx/createLogPath.sh
```

## Instrument a .NET application on Windows

Before running the application, set the following environment variables:

```env
COR_ENABLE_PROFILING=1
COR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
```

Example in PowerShell:

```powershell
$Env:COR_ENABLE_PROFILING = "1"                                   # Enable the .NET Framework Profiler
$Env:COR_PROFILER = "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"      # Select the .NET Framework Profiler
$Env:CORECLR_ENABLE_PROFILING = "1"                               # Enable the .NET (Core) Profiler
$Env:CORECLR_PROFILER = "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"  # Select the .NET (Core) Profiler
# Now the auto-instrumentation is configured in this shell session.
# You can set additional settings and run your application, for example:
$Env:SIGNALFX_SERVICE_NAME = "my-service-name"                    # Set the service name
$Env:SIGNALFX_ENV = "production"                                  # Set the environment name
dotnet run                                                        # Run your application                                                     
```

## Instrument a .NET application on Linux

Before running the application, set the following environment variables:

```env
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={B4C89B0F-9908-4F73-9F59-0D77C5A06874}
CORECLR_PROFILER_PATH=/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so
SIGNALFX_DOTNET_TRACER_HOME=/opt/signalfx
```

Example in Bash:

```bash
export CORECLR_ENABLE_PROFILING="1"                                                 # Enable the .NET (Core) Profiler
export CORECLR_PROFILER="{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"                    # Select the .NET (Core) Profiler
export CORECLR_PROFILER_PATH="/opt/signalfx/SignalFx.Tracing.ClrProfiler.Native.so" # Select the .NET (Core) Profiler file path
export SIGNALFX_DOTNET_TRACER_HOME="/opt/signalfx"                                  # Select the SignalFx Instrumentation for .NET home folder
# Now the auto-instrumentation is configured in this shell session.
# You can set additional settings and run your application, for example:
export SIGNALFX_SERVICE_NAME="my-service-name"  # Set the service name
export SIGNALFX_ENV="production"                # Set the environment name
dotnet run                                      # Run your application 
```

## Instrument a Windows Service running a .NET application

See [windows-service-instrumentation.md](windows-service-instrumentation.md).

## Instrument an ASP.NET application deployed on IIS

See [iis-instrumentation.md](iis-instrumentation.md).

## Azure instrumentation guide

See [azure-instrumentation.md](azure-instrumentation.md).

## Advanced configuration

See [advanced-config.md](advanced-config.md).

## Manual instrumentation

See [manual-instrumentation.md](manual-instrumentation.md).

## Correlating traces with logs

See [correlating-traces-with-logs.md](correlating-traces-with-logs.md).

## Metrics

See [metrics.md](metrics.md)

## AlwaysOn Profiling

See [always-on-profiling.md](always-on-profiling.md)

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
