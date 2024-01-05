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
| `SIGNALFX_PROFILER_MEMORY_ENABLED` | Enable to activate memory profiling. | `false` |
| `SIGNALFX_PROFILER_MAX_MEMORY_SAMPLES_PER_MINUTE` | Configuratoin key for the maximum number of memory samples gathered per minute. | `200`
| `SIGNALFX_PROFILER_EXPORT_INTERVAL` | Profiling exporter interval in milliseconds. It defines how often the profiling data is sent to the collector. If the CPU profiling is enabled this value will automatically be set to match `SIGNALFX_PROFILER_CALL_STACK_INTERVAL`. | `10000` |

## Unsupported upstream settings

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_AGENT_HOST` | The Agent host where the tracer can send traces. |  |
| `SIGNALFX_APM_RECEIVER_PORT` | The port for Trace Agent binding. | `8126` |
| `SIGNALFX_DOGSTATSD_ARGS` | Comma-separated list of arguments to be passed to the DogStatsD process. |  |
| `SIGNALFX_DOGSTATSD_PATH` | The DogStatsD path for when a standalone instance needs to be started. |  |
| `SIGNALFX_DOGSTATSD_PIPE_NAME` | The named pipe that DogStatsD binds to. |  |
| `SIGNALFX_DOGSTATSD_PORT` | The port of the targeted StatsD server. | `8125` |
| `SIGNALFX_INTERNAL_TRACE_VERSION_COMPATIBILITY` | Enables the compatibility with other versions of tracer. | `false` |
| `SIGNALFX_INTERNAL_PROFILING_LIBDDPROF_ENABLED` | Activates the native pprof generation. | `false` |
| `SIGNALFX_PROFILING_WALLTIME_ENABLED` | Activates wall time profiling | `false` |
| `SIGNALFX_TRACE_RATE_LIMIT` | The number of traces allowed to be submitted per second. | `100` |
| `SIGNALFX_PROXY_HTTPS` | TConfiguration key to set a proxy server for https requests. |  |
| `SIGNALFX_PROXY_NO_PROXY` | Configuration key to set a list of hosts that should bypass the proxy. The list is space-separated|  |
| `SIGNALFX_TRACE_{0}_ANALYTICS_ENABLED` | Enable to activate analytics for specific integration. | `false` |
| `SIGNALFX_TRACE_{0}_ANALYTICS_SAMPLE_RATE` | Set sample rate for analytics in specific integration. |  |
| `SIGNALFX_TRACE_AGENT_ARGS` | Comma-separated list of arguments to be passed to the Trace Agent process. |  |
| `SIGNALFX_TRACE_AGENT_HOSTNAME` | The Agent host where the tracer can send traces | `localhost` |
| `SIGNALFX_TRACE_AGENT_PATH` | The Trace Agent path for when a standalone instance needs to be started. |  |
| `SIGNALFX_TRACE_AGENT_PORT` | The Agent port where the tracer can send traces | `9411` |
| `SIGNALFX_TRACE_ANALYTICS_ENABLED` | Enable to activate default Analytics. | `false` |
| `SIGNALFX_TRACE_BATCH_INTERVAL` | The batch interval in milliseconds for the serialization queue. | `100` |
| `SIGNALFX_TRACE_LOG_PATH` | (Deprecated) The path of the profiler log file. | Linux: `/var/log/signalfx/dotnet/dotnet-profiler.log`<br>Windows: `%ProgramData%\SignalFx .NET Tracing\logs\dotnet-profiler.log` |
| `SIGNALFX_TRACE_HEADER_TAG_NORMALIZATION_FIX_ENABLED` | Enables a fix around header tags normalization. We used to normalize periods even if a tag was provided for a header, whereas we should not. | `true` |
| `SIGNALFX_TRACE_PIPE_NAME` | The named pipe where the tracer can send traces. |  |
| `SIGNALFX_TRACE_PIPE_TIMEOUT_MS` | The timeout in milliseconds for named pipes communication. | `100` |
| `SIGNALFX_TRACE_SAMPLE_RATE` | The global rate for the sampler. By default, all traces are sampled. |  |
| `SIGNALFX_TRACE_WRITE_INSTRUMENTATION_TO_DISK` | Used to enable saving instrumentation data to disc for Instrumentation Verification Library to use (not supported by SignalFx). | `false` |
| `SIGNALFX_TRACE_SAMPLING_RULES` | Comma-separated list of sampling rules that enable custom sampling rules based on regular expressions. Rule are matched by order of specification. Only the first match is used. The item "sample_rate" must be in decimal format. Both `service` and `name` accept regular expressions. | `'[{"sample_rate":0.5, "service":"cart.*"}],[{"sample_rate":0.2, "name":"http.request"}]'` |
| `SIGNALFX_TRACE_SERVICE_MAPPING` | Comma-separated map of services to rename. | `"key1:val1,key2:val2"` |
| `SIGNALFX_TRACE_TRANSPORT` | Overrides the transport to use for communicating with the trace agent. Available values are: `datagod-tcp`, `datadog-named-pipes`. |  |
| `SIGNALFX_AAS_ENABLE_CUSTOM_TRACING` | Used to force the loader to start the tracer agent (in case automatic instrumentation is disabled). Used in contexts where the user cannot manage agent processes, such as Azure App Services. |  |
| `SIGNALFX_AAS_ENABLE_CUSTOM_METRICS` | Used to force the loader to start dogstatsd (in case automatic instrumentation is disabled). Used in contexts where the user cannot manage agent processes, such as Azure App Services. |  |
| `SIGNALFX_SITE` | Sets the default destination site. |  |
| `SIGNALFX_LOG_LEVEL` | Sets the log level for serverless. |  |
| `_SIGNALFX_EXTENSION_PATH` | Sets the lambda extension path. |  |
| `SIGNALFX_TRACE_X_DATADOG_TAGS_MAX_LENGTH` | Configuration key for the maximum length of an outgoing propagation header's value ("x-datadog-tags") | `512`  |
| `SIGNALFX_DEBUGGER_POLL_INTERVAL` | Sets the debugger poll interval (in seconds). | `1`  |
| `SIGNALFX_DEBUGGER_SNAPSHOT_URL` | Sets the URL used to query our backend directly for the list of active probes. This can only be used if SIGNALFX_API_KEY is also available. | |
| `SIGNALFX_DEBUGGER_PROBE_FILE` | Sets the probe configuration file full path. Loads the probe configuration from a local file on disk. Useful for local development and testing. | |
| `SIGNALFX_DEBUGGER_MAX_DEPTH_TO_SERIALIZE` | Sets the max object depth to serialize for probe snapshots. | `1`  |
| `SIGNALFX_DEBUGGER_MAX_TIME_TO_SERIALIZE` | Sets the maximum duration (in milliseconds) to run serialization for probe snapshots. | `150`  |
| `SIGNALFX_DEBUGGER_UPLOAD_BATCH_SIZE` | Sets the maximum upload batch size. | `100`  |
| `SIGNALFX_DEBUGGER_DIAGNOSTICS_INTERVAL` | Sets the interval (in seconds) between sending probe statuses. | `3600`  |
| `SIGNALFX_DEBUGGER_UPLOAD_FLUSH_INTERVAL` | Sets the interval (in milliseconds) between flushing statuses. | `0`  |
| `SIGNALFX_INTERNAL_DEBUGGER_ENABLED` | Enables the Live Debugger | `false`  |
| `SIGNALFX_INTERNAL_DEBUGGER_INSTRUMENT_ALL` | Determine whether to enter "instrument all" mode where the Debugger instrumentation is applied to every jit compiled method. Only useful for testing purposes. | `false` |

## Unpublished settings

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_METRICS_EXPORTER` | Metrics exporter to be used. It is used to encode and dispatch metrics. Available values are: `SignalFx`, `StatsD`. | `SignalFx` |
| `SIGNALFX_PROFILING_CPU_ENABLED` | Enables CPU profiling. | `false` |
| `SIGNALFX_PROFILING_CODEHOTSPOTS_ENABLED` | Enables profiling HotSpots feature. | `false` |
| `SIGNALFX_TRACE_ACTIVITY_LISTENER_ENABLED` | Enables experimental support for activity listener. | `false` |
| `SIGNALFX_TRACE_ANNOTATIONS_ENABLED` | The Tracer will automatically instrument methods that are decorated with a recognized trace attribute. | `true` |
| `SIGNALFX_TRACE_AZURE_FUNCTIONS_ENABLED` | Set to instrument within Azure functions. | `false` |
| `SIGNALFX_TRACE_EXPAND_ROUTE_TEMPLATES_ENABLED` | Set to expand route parameters in ASP.NET and ASP.NET Core resource names. | `false` |
| `SIGNALFX_TRACE_GRPC_TAGS` | Comma-separated list of key-value pairs automatically applied as GRPC metadata values as tags on traces. For example: `"key1:val1,key2:val2"` |  |
| `SIGNALFX_TRACE_METHODS` | Semicolon-separated list of methods to be automatically traced. |  |
