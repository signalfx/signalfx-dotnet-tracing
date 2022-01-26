# Azure instrumentation guide

## App Service

1. Choose your app in Azure App Service.

2. Go to **Development Tools > Extensions**.

3. Find and install the **SignalFx .NET Tracing** extension.

4. Go to **Settings > Configuration**.

5. Click **New application setting** to add the following settings:

   | Name | Value | Description |
   | - | - | - |
   | `SIGNALFX_SERVICE_NAME` | `my-service-name` | Name of the instrumented service. |
   | `SIGNALFX_ENV` | `production` | Deployment environment of the instrumented service. |
   | `SIGNALFX_ACCESS_TOKEN` | `[splunk-access-token]` | Access token. See [here](https://docs.splunk.com/Observability/admin/authentication-tokens/org-tokens.html) to learn how to obtain one. |
   | `SIGNALFX_ENDPOINT_URL` |  `https://ingest.[splunk-realm].signalfx.com/v2/trace` | In the endpoint URL, `splunk-realm` is the [O11y realm](https://dev.splunk.com/observability/docs/realms_in_endpoints). For example, `us0`. |

6. Restart the application in App Service.

> **Tip:** To reduce latency and benefit from OTel Collector features,
> you can set the endpoint URL setting to a Collector instance running
> in Azure VM over an Azure private network.

## WebJobs (Experimental)

To instrument a WebJob, follow these steps:
1. Choose your application in Azure App Service.
2. Go to **Development Tools > Extensions**.
3. Find and install the **SignalFx .NET Tracing** extension.
4. Go to **Settings > Configuration**.
5. Click **New application setting** to add the following settings, replacing `[extension-version]` with the version of the .NET instrumentation (for example,`v0.2.0`):
   | Name | Value | Description |
   | - | - | - |
   | `COR_ENABLE_PROFILING` | `1` | Enables .NET Framework instrumentation. |
   | `COR_PROFILER` | `{B4C89B0F-9908-4F73-9F59-0D77C5A06874}` | |
   | `COR_PROFILER_PATH` | `C:\home\signalfx\tracing\[extension-version]\win-x64\SignalFx.Tracing.ClrProfiler.Native.dll` | |
   | `COR_PROFILER_PATH_32` |  `C:\home\signalfx\tracing\[extension-version]\win-x86\SignalFx.Tracing.ClrProfiler.Native.dll` | |
   | `COR_PROFILER_PATH_64` |  `C:\home\signalfx\tracing\[extension-version]\win-x64\SignalFx.Tracing.ClrProfiler.Native.dll` | |
   | `CORECLR_ENABLE_PROFILING` | `1` | Enables .NET Core instrumentation. |
   | `CORECLR_PROFILER` | `{B4C89B0F-9908-4F73-9F59-0D77C5A06874}` | |
   | `CORECLR_PROFILER_PATH_32` | `C:\home\signalfx\tracing\[extension-version]\win-x86\SignalFx.Tracing.ClrProfiler.Native.dll` | |
   | `CORECLR_PROFILER_PATH_64` | `C:\home\signalfx\tracing\[extension-version]\win-x64\SignalFx.Tracing.ClrProfiler.Native.dll` | |
   | `SIGNALFX_DOTNET_TRACER_HOME` | `C:\home\signalfx\tracing\[extension-version]` | SignalFX extension install location. |
   | `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` | `SnapshotUploader.exe;workerforwarder.exe` | Azure internal services to exclude. |
   | `SIGNALFX_TRACE_LOG_PATH` | `C:\home\LogFiles\signalfx\tracing\[extension-version]\dotnet-profiler.log` | Path for log files. |
   | `SIGNALFX_AZURE_APP_SERVICES` | `0` | Must be set to `0` to enable background services instrumentation. |
   | `SIGNALFX_ACCESS_TOKEN` | `[splunk-access-token]` | Access token. See [here](https://docs.splunk.com/Observability/admin/authentication-tokens/org-tokens.html) to learn how to obtain one. |

> *NOTE*: You must disable `SIGNALFX_AZURE_APP_SERVICES` when instrumenting WebJobs. Keep a separate App Service for the WebJob.