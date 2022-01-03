# Internal configuration

This section contains list of internal and not supported configuration settings.
These settings should be never used by the users.

## Internal settings

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_CLR_DISABLE_OPTIMIZATIONS` | Set to disable all JIT optimizations. | `false` |
| `SIGNALFX_CLR_ENABLE_INLINING` | Set to `false` to disable JIT inlining. | `true` |
| `SIGNALFX_CLR_ENABLE_NGEN` | Set to `false` to disable NGEN images. | `true` |
| `SIGNALFX_CONVENTION` | Sets the semantic and trace id conventions for the tracer. Available values are: `Datadog` (64bit trace id), `OpenTelemetry` (128 bit trace id). | `OpenTelemetry` |
| `SIGNALFX_DUMP_ILREWRITE_ENABLED` | Allows the profiler to dump the IL original code and modification to the log. | `false` |
| `SIGNALFX_EXPORTER` | The exporter to be used. The Tracer uses it to encode and dispatch traces. Available values are: `DatadogAgent`, `Zipkin`. | `Zipkin` |

## Unsupported upstream settings

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_AGENT_HOST` | The Agent host where the tracer can send traces. |  |
| `SIGNALFX_APM_RECEIVER_PORT` | The port for Trace Agent binding. | `8126` |
| `SIGNALFX_APPSEC_BLOCKING_ENABLED` | Enables the AppSec blocking. | `false` |
| `SIGNALFX_APPSEC_ENABLED` | Enables the AppSec. | `false` |
| `SIGNALFX_APPSEC_EXTRA_HEADERS` | Optional custom headers the user wants to send. |  |
| `SIGNALFX_APPSEC_KEEP_TRACES` | Specifies if the AppSec traces should be explicitly kept or droped. | `true` |
| `SIGNALFX_APPSEC_IPHEADER` | Optional name of the custom header to take into account for the ip address. |  |
| `SIGNALFX_APPSEC_RULES` | Overrides the default rules file provided. Must be a path to a valid JSON rules file. |  |
| `SIGNALFX_CIVISIBILITY_ENABLED` | Enable to activate CI Visibility. | `false` |
| `SIGNALFX_DOGSTATSD_ARGS` | Comma-separated list of arguments to be passed to the DogStatsD process. |  |
| `SIGNALFX_DOGSTATSD_PATH` | The DogStatsD path for when a standalone instance needs to be started. |  |
| `SIGNALFX_DOGSTATSD_PIPE_NAME` | The named pipe that DogStatsD binds to. |  |
| `SIGNALFX_DOGSTATSD_PORT` | The port of the targeted StatsD server. | `8125` |
| `SIGNALFX_INTERNAL_TRACE_VERSION_COMPATIBILITY` | Enables the compatibility with other versions of tracer. | `false` |
| `SIGNALFX_TRACE_{0}_ANALYTICS_ENABLED` | Enable to activate analytics for specific integration. | `false` |
| `SIGNALFX_TRACE_{0}_ANALYTICS_SAMPLE_RATE` | Set sample rate for analytics in specific integration. |  |
| `SIGNALFX_TRACE_AGENT_ARGS` | Comma-separated list of arguments to be passed to the Trace Agent process. |  |
| `SIGNALFX_TRACE_AGENT_HOSTNAME` | The Agent host where the tracer can send traces | `localhost` |
| `SIGNALFX_TRACE_AGENT_PATH` | The Trace Agent path for when a standalone instance needs to be started. |  |
| `SIGNALFX_TRACE_AGENT_PORT` | The Agent port where the tracer can send traces | `9411` |
| `SIGNALFX_TRACE_AGENT_URL` | Alias for `SIGNALFX_ENDPOINT_URL`. The URL to where trace exporters send traces. | `http://localhost:9411/api/v2/spans` |
| `SIGNALFX_TRACE_ANALYTICS_ENABLED` | Enable to activate default Analytics. | `false` |
| `SIGNALFX_TRACE_BATCH_INTERVAL` | The batch interval in milliseconds for the serialization queue. | `100` |
| `SIGNALFX_TRACE_BUFFER_SIZE` | The size in bytes of the trace buffer. | `1024 * 1024 * 10 (10MB)` |
| `SIGNALFX_TRACE_LOG_PATH` | (Deprecated) The path of the profiler log file. | Linux: `/var/log/signalfx/dotnet/dotnet-profiler.log`<br>Windows: `%ProgramData%\SignalFx .NET Tracing\logs\dotnet-profiler.log` |
| `SIGNALFX_TRACE_METRICS_ENABLED` | Enable to activate internal metrics sent to DogStatsD. | `false` |
| `SIGNALFX_TRACE_PIPE_NAME` | The named pipe where the tracer can send traces. |  |
| `SIGNALFX_TRACE_PIPE_TIMEOUT_MS` | The timeout in milliseconds for named pipes communication. | `100` |
| `SIGNALFX_TRACE_SERVICE_MAPPING` | Comma-separated map of services to rename. | `"key1:val1,key2:val2"` |
| `SIGNALFX_TRACE_TRANSPORT` | Overrides the transport to use for communicating with the trace agent. Available values are: `datagod-tcp`, `datadog-named-pipes`. |  |

## Unpublished settings

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_METRICS_ENDPOINT_URL` | The URL to where metric exporters send metrics. | `http://localhost:9943/v2/datapoint` |
| `SIGNALFX_METRICS_EXPORTER` | Metrics exporter to be used. It is used to encode and dispatch metrics. Available values are: `SignalFx`, `StatsD`. | `SignalFx` |
| `SIGNALFX_RUNTIME_METRICS_ENABLED` | Enable to activate internal runtime metrics sent to SignalFx. | `false` |
| `SIGNALFX_THREAD_SAMPLING_ENABLED` | Enable to activate thread sampling. | `false` |
| `SIGNALFX_THREAD_SAMPLING_PERIOD` | Sampling period. It defines how often the threads are stopped in order to fetch all stack traces. This value cannot be lower than `1000` milliseconds. | `1000` |
| `SIGNALFX_TRACE_AZURE_FUNCTIONS_ENABLED` | Set to instrument within Azure functions. | `false` |
