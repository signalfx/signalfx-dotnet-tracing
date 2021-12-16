# Internal configuration

This section contains list of internal and not supported configuration settings.
These settings should be never used by the users.

## New settings

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_EXPORTER` | The exporter to be used. The Tracer uses it to encode and dispatch traces. Available values are: `DatadogAgent`, `Zipkin`. | `Zipkin` |
| `SIGNALFX_CONVENTION` | Sets the semantic and trace id conventions for the tracer. Available values are: `Datadog` (64bit trace id), `OpenTelemetry` (128 bit trace id). | `OpenTelemetry` |

## Unsupported upstream settings

| Environment variable | Description | Default |
|-|-|-|
| `SIGNALFX_AGENT_HOST` | The host name of the targeted SatsD server. |  |
| `SIGNALFX_TRACE_AGENT_URL` | Alias for `SIGNALFX_ENDPOINT_URL`. The URL to where trace exporters send traces. | `http://localhost:9411/api/v2/spans` |
| `SIGNALFX_TRACE_AGENT_HOST` | The Agent host where the Tracer can send traces | `localhost` |
| `SIGNALFX_TRACE_AGENT_PORT` | The Agent port where the Tracer can send traces | `9411` |
| `SIGNALFX_TRACE_PIPE_NAME` | The named pipe where the Tracer can send traces. |  |
| `SIGNALFX_TRACE_PIPE_TIMEOUT_MS` | The timeout in milliseconds for named pipes communication. | `100` |
| `SIGNALFX_TRACE_BUFFER_SIZE` | The size in bytes of the trace buffer. | `1024 * 1024 * 10 (10MB)` |
| `SIGNALFX_TRACE_BATCH_INTERVAL` | The batch interval in milliseconds for the serialization queue. | `100` |
| `SIGNALFX_DOGSTATSD_PORT` | The port of the targeted StatsD server. | `8125` |
| `SIGNALFX_DOGSTATSD_PIPE_NAME` | The named pipe that DogStatsD binds to. |  |
| `SIGNALFX_APM_RECEIVER_PORT` | The port for Trace Agent binding. | `8126` |
| `SIGNALFX_TRACE_ANALYTICS_ENABLED` | Enable to activate default Analytics. | `false` |
| `SIGNALFX_TRACE_SERVICE_MAPPING` | Comma-separated map of services to rename. | `"key1:val1,key2:val2"` |
| `SIGNALFX_TRACE_METRICS_ENABLED` | Enable to activate internal metrics sent to DogStatsD. | `false` |
| `SIGNALFX_RUNTIME_METRICS_ENABLED` | Enable to activate internal runtime metrics sent to DogStatsD. | `false` |
| `SIGNALFX_TRACE_AGENT_PATH` | The Trace Agent path for when a standalone instance needs to be started. |  |
| `SIGNALFX_TRACE_AGENT_ARGS` | Comma-separated list of arguments to be passed to the Trace Agent process. |  |
| `SIGNALFX_DOGSTATSD_PATH` | The DogStatsD path for when a standalone instance needs to be started. |  |
| `SIGNALFX_DOGSTATSD_ARGS` | Comma-separated list of arguments to be passed to the DogStatsD pricess. |  |
| `SIGNALFX_API_KEY` | The API key used by the Agent. |  |
| `SIGNALFX_TRACE_TRANSPORT` | Overrides the transport to use for communicating with the trace agent. Available values are: `datagod-tcp`, `datadog-named-pipes`. | `null` |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS` | The minimum number of closed spans in a trace before it's partially flushed. `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED` has to be enabled for this to take effect. | `500` |
