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
| `SIGNALFX_LOGS_DIRECT_SUBMISSION_INTEGRATIONS` | Configuration key for a list of direct log submission integrations to enable. Only selected integrations are enabled for direct log submission. Supports multiple values separated with semi-colons. Valid values are `ILogger`, `Serilog`, `Log4Net`, `NLog` | `` |
| `SIGNALFX_LOGS_DIRECT_SUBMISSION_HOST` | Configuration key for the name of the originating host for direct logs submission. | `` |
| `SIGNALFX_LOGS_DIRECT_SUBMISSION_SOURCE` | Configuration key for the originating source for direct logs submission. | `csharp` |
| `SIGNALFX_LOGS_DIRECT_SUBMISSION_TAGS` | Configuration key for a list of tags to be applied globally to all logs directly submitted. Supports multiple key key-value pairs which are comma-separated, and for which the key and value are colon-separated. For example Key1:Value1, Key2:Value2 | `` |
| `SIGNALFX_LOGS_DIRECT_SUBMISSION_URL` | Configuration key for the url to send logs to.  | `https://http-intake.logs.datadoghq.com:443` |
| `SIGNALFX_LOGS_DIRECT_SUBMISSION_MINIMUM_LEVEL` | Configuration key for the minimum level logs should have to be sent to the intake. Should be one of `Verbose`,`Debug`,`Information`,`Warning`,`Error`,`Fatal` | `Information` |
| `SIGNALFX_LOGS_DIRECT_SUBMISSION_MAX_BATCH_SIZE` | Configuration key for the maximum number of logs to send at one time  | `1000` |
| `SIGNALFX_LOGS_DIRECT_SUBMISSION_MAX_QUEUE_SIZE` | Configuration key for the maximum number of logs to hold in internal queue at any one time | `100000` |
| `SIGNALFX_LOGS_DIRECT_SUBMISSION_BATCH_PERIOD_SECONDS` | Configuration key for the time in seconds to wait between checking for batches | `2` |

## Unsupported upstream settings

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_AGENT_HOST` | The Agent host where the tracer can send traces. |  |
| `SIGNALFX_APM_RECEIVER_PORT` | The port for Trace Agent binding. | `8126` |
| `SIGNALFX_APPSEC_ENABLED` | Enables the AppSec. | `false` |
| `SIGNALFX_APPSEC_EXTRA_HEADERS` | Optional custom headers the user wants to send. |  |
| `SIGNALFX_APPSEC_IPHEADER` | Optional name of the custom header to take into account for the ip address. |  |
| `SIGNALFX_APPSEC_KEEP_TRACES` | Specifies if the AppSec traces should be explicitly kept or droped. | `true` |
| `SIGNALFX_APPSEC_RULES` | Overrides the default rules file provided. Must be a path to a valid JSON rules file. |  |
| `SIGNALFX_CIVISIBILITY_ENABLED` | Enable to activate CI Visibility. | `false` |
| `SIGNALFX_DOGSTATSD_ARGS` | Comma-separated list of arguments to be passed to the DogStatsD process. |  |
| `SIGNALFX_DOGSTATSD_PATH` | The DogStatsD path for when a standalone instance needs to be started. |  |
| `SIGNALFX_DOGSTATSD_PIPE_NAME` | The named pipe that DogStatsD binds to. |  |
| `SIGNALFX_DOGSTATSD_PORT` | The port of the targeted StatsD server. | `8125` |
| `SIGNALFX_INTERNAL_TRACE_VERSION_COMPATIBILITY` | Enables the compatibility with other versions of tracer. | `false` |
| `SIGNALFX_MAX_TRACES_PER_SECOND` | The number of traces allowed to be submitted per second. | `100` |
| `SIGNALFX_TRACE_{0}_ANALYTICS_ENABLED` | Enable to activate analytics for specific integration. | `false` |
| `SIGNALFX_TRACE_{0}_ANALYTICS_SAMPLE_RATE` | Set sample rate for analytics in specific integration. |  |
| `SIGNALFX_TRACE_AGENT_ARGS` | Comma-separated list of arguments to be passed to the Trace Agent process. |  |
| `SIGNALFX_TRACE_AGENT_HOSTNAME` | The Agent host where the tracer can send traces | `localhost` |
| `SIGNALFX_TRACE_AGENT_PATH` | The Trace Agent path for when a standalone instance needs to be started. |  |
| `SIGNALFX_TRACE_AGENT_PORT` | The Agent port where the tracer can send traces | `9411` |
| `SIGNALFX_TRACE_ANALYTICS_ENABLED` | Enable to activate default Analytics. | `false` |
| `SIGNALFX_TRACE_BATCH_INTERVAL` | The batch interval in milliseconds for the serialization queue. | `100` |
| `SIGNALFX_TRACE_BUFFER_SIZE` | The size in bytes of the trace buffer. | `1024 * 1024 * 10 (10MB)` |
| `SIGNALFX_TRACE_LOG_PATH` | (Deprecated) The path of the profiler log file. | Linux: `/var/log/signalfx/dotnet/dotnet-profiler.log`<br>Windows: `%ProgramData%\SignalFx .NET Tracing\logs\dotnet-profiler.log` |
| `SIGNALFX_TRACE_HEADER_TAG_NORMALIZATION_FIX_ENABLED` | Enables a fix around header tags normalization. We used to normalize periods even if a tag was provided for a header, whereas we should not. | `true` |
| `SIGNALFX_TRACE_PIPE_NAME` | The named pipe where the tracer can send traces. |  |
| `SIGNALFX_TRACE_PIPE_TIMEOUT_MS` | The timeout in milliseconds for named pipes communication. | `100` |
| `SIGNALFX_TRACE_SAMPLE_RATE` | The global rate for the sampler. By default, all traces are sampled. |  |
| `SIGNALFX_TRACE_SAMPLING_RULES` | Comma-separated list of sampling rules that enable custom sampling rules based on regular expressions. Rule are matched by order of specification. Only the first match is used. The item "sample_rate" must be in decimal format. Both `service` and `name` accept regular expressions. | `'[{"sample_rate":0.5, "service":"cart.*"}],[{"sample_rate":0.2, "name":"http.request"}]'` |
| `SIGNALFX_TRACE_SERVICE_MAPPING` | Comma-separated map of services to rename. | `"key1:val1,key2:val2"` |
| `SIGNALFX_TRACE_TRANSPORT` | Overrides the transport to use for communicating with the trace agent. Available values are: `datagod-tcp`, `datadog-named-pipes`. |  |
| `SIGNALFX_AAS_ENABLE_CUSTOM_TRACING` | Used to force the loader to start the tracer agent (in case automatic instrumentation is disabled). Used in contexts where the user cannot manage agent processes, such as Azure App Services. |  |
| `SIGNALFX_AAS_ENABLE_CUSTOM_METRICS` | Used to force the loader to start dogstatsd (in case automatic instrumentation is disabled). Used in contexts where the user cannot manage agent processes, such as Azure App Services. |  |
| `SIGNALFX_INSTRUMENTATION_TELEMETRY_ENABLED` | Used to enable internal telemetry. | `false` |
| `SIGNALFX_TRACE_TELEMETRY_URL` | Sets the telemetry URL where the tracer sends telemetry. |  |
| `SIGNALFX_SITE` | Sets the default destination site. |  |
| `SIGNALFX_LOG_LEVEL` | Sets the log level for serverless. |  |
| `_SIGNALFX_EXTENSION_PATH` | Sets the lambda extension path. |  |

## Unpublished settings

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_PROFILER_LOGS_ENDPOINT` | The URL to where logs are exported using [OTLP/HTTP v1 log protocol](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/otlp.md) | `http://localhost:4318/v1/logs` |
| `SIGNALFX_METRICS_EXPORTER` | Metrics exporter to be used. It is used to encode and dispatch metrics. Available values are: `SignalFx`, `StatsD`. | `SignalFx` |
| `SIGNALFX_PROFILER_ENABLED` | Enable to activate thread sampling. | `false` |
| `SIGNALFX_PROFILER_CALL_STACK_INTERVAL` | Sampling period. It defines how often the threads are stopped in order to fetch all stack traces. This value cannot be lower than `1000` milliseconds. | `10000` |
| `SIGNALFX_TRACE_AZURE_FUNCTIONS_ENABLED` | Set to instrument within Azure functions. | `false` |
| `SIGNALFX_TRACE_EXPAND_ROUTE_TEMPLATES_ENABLED` | Set to expand route parameters in ASP.NET and ASP.NET Core resource names. | `false` |
