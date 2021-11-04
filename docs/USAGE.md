# Usage

**WARNING**: Please notice that no official release has been created for this repo yet, so installation instructions below would require you to build the code manually beforehand.

## Configure the OpenTelemetry Tracing Library for .NET

### Configuration values

Use these environment variables to configure the tracing library:

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_ACCESS_TOKEN` | The access token for your SignalFx organization. It enables sending traces directly to the SignalFx ingestion endpoint. To do so, the `SIGNALFX_TRACE_AGENT_URL` must be set with: `https://ingest.<REALM>.signalfx.com/v2/trace`. |  |
| `SIGNALFX_TRACE_CONFIG_FILE` | The file path of a JSON configuration file that will be loaded. |  |
| `SIGNALFX_VERSION` | The application's version that will populate `version` tag on spans. |  |
| `SIGNALFX_TRACE_ADONET_EXCLUDED_TYPES` | Comma-separated list of AdoNet types that will be excluded from automatic instrumentation. |  |
| `SIGNALFX_AGENT_HOST` | The host name of the targeted SatsD server. |  |
| `SIGNALFX_TRACE_AGENT_PORT` | The Agent port where the Tracer can send traces | `localhost` |
| `SIGNALFX_TRACE_PIPE_NAME` | The named pipe where the Tracer can send traces. |  |
| `SIGNALFX_TRACE_PIPE_TIMEOUT_MS` | The timeout in milliseconds for named pipes communication. | `100` |
| `SIGNALFX_DOGSTATSD_PIPE_NAME` | The named pipe that DogStatsD binds to. |  |
| `SIGNALFX_APM_RECEIVER_PORT` | The port for Trace Agent binding. | `8126` |
| `SIGNALFX_TRACE_ANALYTICS_ENABLED` | Enable to activate default Analytics. | `false` |
| `SIGNALFX_TRACE_HEADER_TAGS` | Comma-separated map of header keys to tag name, that will be automatically applied as tags on traces. | `"key1:val1,key2:val2"` |
| `SIGNALFX_TRACE_SERVICE_MAPPING` | Comma-separated map of services to rename. | `"key1:val1,key2:val2"` |
| `SIGNALFX_TRACE_BUFFER_SIZE` | The size in bytes of the trace buffer. | `1024 * 1024 * 10 (10MB)` |
| `SIGNALFX_TRACE_BATCH_INTERVAL` | The batch interval in milliseconds for the serialization queue. | `100` |
| `SIGNALFX_MAX_TRACES_PER_SECOND` | The number of traces allowed to be submitted per second. | `100` |
| `SIGNALFX_TRACE_RESPONSE_HEADER_ENABLED` | Enable to add server trace information to HTTP response headers. | `true` |
| `SIGNALFX_TRACE_STARTUP_LOGS` | Enable to activate diagnostic log at stratup. | `true` |
| `SIGNALFX_TRACE_SAMPLING_RULES` | Comma separated list of sampling rules taht enabled custom sampling rules based on regular expressions. The rule is matched in order of specification. The first match in a list is used. The item "sample_rate" is required in decimal format. The item "service" is optional in regular expression format, to match on service name. The item "name" is optional in regular expression format, to match on operation name. | `'[{"sample_rate":0.5, "service":"cart.*"}],[{"sample_rate":0.2, "name":"http.request"}]'` |
| `SIGNALFX_TRACE_SAMPLE_RATE` | The global rate for the sampler. |  |
| `SIGNALFX_DOGSTATSD_PORT` | The port of the targeted StatsD server. | `8125` |
| `SIGNALFX_TRACE_METRICS_ENABLED` | Enable to activate internal metrics sent to DogStatsD. | `false` |
| `SIGNALFX_RUNTIME_METRICS_ENABLED` | Enable to activate internal runtime metrics sent to DogStatsD. | `false` |
| `SIGNALFX_TRACE_LOGGING_RATE` | The number of seconds between identical log messages for Tracer log files. Setting to 0 disables rate limiting. | `60` |
| `SIGNALFX_TRACE_LOG_DIRECTORY` | The directory of the .NET Tracer logs. Overrides the value in `SIGNALFX_TRACE_LOG_PATH` if present. | Linux: `/var/log/signalfx/dotnet/`<br>Windows: `%ProgramData%"\SignalFx .NET Tracing\logs\` |
| `SIGNALFX_TRACE_AGENT_PATH` | The Trace Agent path for when a standalone instance needs to be started. |  |
| `SIGNALFX_TRACE_AGENT_ARGS` | Comma-separated list of arguments to be passed to the Trace Agent process. |  |
| `SIGNALFX_DOGSTATSD_PATH` | The DogStatsD path for when a standalone instance needs to be started. |  |
| `SIGNALFX_DOGSTATSD_ARGS` | Comma-separated list of arguments to be passed to the DogStatsD pricess. |  |
| `SIGNALFX_API_KEY` | The API key used by the Agent. |  |
| `SIGNALFX_TRACE_TRANSPORT` | Overrides the transport to use for communicating with the trace agent. Available values are: `datagod-tcp`, `datadog-named-pipes`. | `null` |
| `SIGNALFX_HTTP_SERVER_ERROR_STATUSES` | The application's server http statuses to set spans as errors by. | `500-599` |
| `SIGNALFX_HTTP_CLIENT_ERROR_STATUSES` | The application's client http statuses to set spans as errors by. | `400-499` |
| `SIGNALFX_EXPORTER_JAEGER_AGENT_HOST` | Hostname for the Jaeger agent. | `localhost` |
| `SIGNALFX_EXPORTER_JAEGER_AGENT_PORT` | Port for the Jaeger agent. | `6831` |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED` | Enable to activate sending partial traces to the agent. | `false` |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS` | The minimum number of closed spans in a trace before it's partially flushed. `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED` has to be enabled for this to take effect. | `500` |
| `SIGNALFX_SERVICE` | Application's default service name. |  |
| `SIGNALFX_ENV` | The value for the `deployment.environment` tag added to every span. |  |
| `SIGNALFX_TRACE_ENABLED` | Enable to activate the tracer. | `true` | 
| `SIGNALFX_TRACE_DEBUG` | Enable to activate debugging mode for the tracer. | `false` | 
| `SIGNALFX_TRACE_AGENT_URL`, `SIGNALFX_ENDPOINT_URL` | The URL to where trace exporters (see: `SIGNALFX_EXPORTER`) send traces. | `http://localhost:8126` | 
| `SIGNALFX_TAGS` | Comma-separated list of key-value pairs to specify global span tags. For example: `"key1:val1,key2:val2"` |  |
| `SIGNALFX_LOGS_INJECTION` | Enable to inject trace IDs, span IDs, service name and environment into logs. This requires a compatible logger or manual configuration. | `false` | `SIGNALFX_EXPORTER` | The exporter to be used. The Tracer uses it to encode and dispatch traces. Available values are: `DatadogAgent`, `Zipkin`, `Jeager`. | `DatadogAgent` |
| `SIGNALFX_MAX_LOGFILE_SIZE` | The maximum size for tracer log files, in bytes. | `10 MB` |
| `SIGNALFX_TRACE_LOG_PATH` | The path of the profiler log file. | Linux: `/var/log/signalfx/dotnet/dotnet-profiler.log`<br>Windows: `%ProgramData%"\SignalFx .NET Tracing\logs\dotnet-profiler.log` |
| `SIGNALFX_DIAGNOSTIC_SOURCE_ENABLED` | Enable to generate troubleshooting logs with the `System.Diagnostics.DiagnosticSource` class. | `true` |
| `SIGNALFX_DISABLED_INTEGRATIONS` | The integrations you want to disable, if any, separated by a comma. These are the supported integrations: AspNetMvc, AspNetWebApi2, DbCommand, ElasticsearchNet5, ElasticsearchNet6, GraphQL, HttpMessageHandler, IDbCommand, MongoDb, NpgsqlCommand, OpenTracing, ServiceStackRedis, SqlCommand, StackExchangeRedis, Wcf, WebRequest |  |
| `SIGNALFX_CONVENTION` | Sets the semantic and trace id conventions for the tracer. Available values are: `Datadog` (64bit trace id), `OpenTelemetry` (128 bit trace id). | `Datadog` |
| `SIGNALFX_PROPAGATORS` | Comma separated list of the propagators for the tracer. Available propagators are: `Datadog`, `B3`, `W3C`. The Tracer will try to execute extraction in the given order. | `Datadog` |
| `SIGNALFX_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION` |  Sets whether to intercept method calls when the caller method is inside a domain-neutral assembly. This is recommended when instrumenting IIS applications. | `false` |
| `SIGNALFX_PROFILER_PROCESSES` | Sets the filename of executables the profiler can attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with comma, for example: `MyApp.exe,dotnet.exe` |  |
| `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` | Sets the filename of executables the profiler cannot attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with comma, for example: `MyApp.exe,dotnet.exe` |  |
| `SIGNALFX_RECORDED_VALUE_MAX_LENGTH` | The maximum length an attribute value can have. Values longer than this are truncated. Values are completely truncated when set to 0, and ignored when set to a negative value. | `12000` |

## Ways to configure

There are following ways to apply configuration settings (priority is from first to last):

1. [Environment variables)(#environment-variables)
2. [`web.config` or `app.config` file](#web.config-and-app.config)
3. [JSON configuration file](#json-configuration-file)

### Environment variables

Environment variables are the main way to configure values. A setting configured via an environment variable cannot be overridden.

### web.config and app.config

For an application running on .NET Framework, web configuration file (`web.config`) or application configuration file (`app.config`) can be used to configure settings.

See example with `SIGNALFX_SERVICE` overload.

```xml
<configuration>
  <appSettings>
    <add key="SIGNALFX_SERVICE" value="my-service-name" />
  </appSettings>
</configuration>
```

### JSON configuration file

Use environment variable `SIGNALFX_TRACE_CONFIG_FILE` or `web.config` / `app.config` to set configuration file path . This cannot be set via JSON configuration file.

See example with `SIGNALFX_SERVICE` overload.

```json
{
    "SIGNALFX_SERVICE": "my-service-name"
}
```

## Setup

### Linux

After downloading the library, install the CLR Profiler and its components
via your system's package manager.

1. Download the latest release of the library.
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
1. Configure the required environment variables:
    ```bash
    $ source /opt/signalfx/defaults.env
    ```
2. Set the service name:
    ```bash
    $ export SIGNALFX_SERVICE='MyCoreService'
    ```
3. Set Zipkin exporter:
    ```bash
    $ export SIGNALFX_EXPORTER='Zipkin'
    ```
4. Set OpenTelemetry conventions:
    ```bash
    $ export SIGNALFX_CONVENTION='OpenTelemetry'
    ```
5. Set the endpoint, e.g. OpenTelemetry Collector:
    ```bash
    $ export SIGNALFX_TRACE_AGENT_URL='http://<YourCollector>:9080/v1/trace'
    ```
6. Optionally, enable trace injection in logs:
    ```bash
    $ export SIGNALFX_LOGS_INJECTION=true
    ```
7. Optionally, create the default logging directory:
    ```bash
    $ source /opt/signalfx/createLogPath.sh
    ```
8. Run your application:
    ```bash
    $ dotnet run
    ```

### Windows

**Warning**: Pay close attention to the scope of environment variables. Ensure they
are properly set prior to launching the targeted process. The following steps set the
environment variables at the machine level, with the exception of the variables used
for finer control of which processes will be instrumented, which are set in the current
command session.

1. Install the CLR Profiler using an installer file (`.msi` file) from the latest release.
Choose the installer (x64 or x86) according to the architecture of the operating
system where it will be running.
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
   setx SIGNALFX_SERVICE MyServiceName /m
   ```
4. Set the endpoint, e.g. OpenTelemetry Collector that will forward
the trace data:
   ```batch
   setx SIGNALFX_TRACE_AGENT_URL http://localhost:9080/v1/trace /m
   ```
5. Optionally, enable trace injection in logs:
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

## Troubleshooting

Check if you are not hitting one of the issues listed below.

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

### Investigating other issues

If none of the suggestions above solves your issue, detailed logs are necessary.
Follow the steps below to get the detailed logs from OpenTelemetry AutoInstrumentation for .NET:

Set the environment variable `SIGNALFX_TRACE_DEBUG` to `true` before the instrumented process starts.
By default, the library writes the log files under the below predefined locations.
If needed, change the default location by updating the environment variable `SIGNALFX_TRACE_LOG_PATH` to an appropriate path. 
On Linux, the default log location is `/var/log/signalfx/dotnet/`
On Windows, the default log location is `%ProgramData%\SignalFx .NET Tracing\logs\`
Compress the whole folder to capture the multiple log files and send the compressed folder to us.
After obtaining the logs, remember to remove the environment variable `SIGNALFX_TRACE_DEBUG` to avoid unnecessary overhead.
