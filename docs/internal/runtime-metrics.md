# .NET runtime metrics

The SignalFx Instrumentation for .NET includes automatic runtime metrics collection.
This feature can be enabled using configuration settings.
When enabled, metrics are periodically captured and sent to Splunk Observability Cloud.

## Enable runtime metrics

To enable runtime metrics, set the `SIGNALFX_RUNTIME_METRICS_ENABLED` environment variable
to `true` for your .NET process.

## Metrics exporter settings

### Default configuration

Metrics are sent to the local instance of [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector),
which forwards telemetry to Splunk Observability Cloud. For instructions on how to set up required components of the metrics pipeline in the collector, see 
[receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/signalfxreceiver) and [exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/signalfxexporter) documentation.

### Exporting directly to Splunk Observability Cloud

If you prefer to send metrics directly to Splunk Observability Cloud, configure the following settings:

| Setting | Value | Notes |
|-|-|-|
| `SIGNALFX_ACCESS_TOKEN` | Your Observability Cloud token | (__required__) See [here](https://docs.splunk.com/Observability/admin/authentication-tokens/org-tokens.html) to learn how to obtain one. |
| `SIGNALFX_REALM` | Your Observability Cloud realm | Set to send telemetry directly to Splunk Observability Cloud. If configured, metrics are sent to `https://ingest.<SIGNALFX_REALM>.signalfx.com/v2/datapoint`. To find your realm, open Splunk Observability Cloud, click Settings, then click on your username. Setting this variable also modify the default tracing endpoint. |
| `SIGNALFX_METRICS_ENDPOINT_URL` | Metric endpoint | (Optional) Overrides other settings for the metrics ingestion endpoint. |

__IMPORTANT:__ To export data directly to Splunk Observability Cloud, set `SIGNALFX_REALM` or `SIGNALFX_METRICS_ENDPOINT_URL` in addition to `SIGNALFX_ACCESS_TOKEN`.

## Troubleshooting

### Check if runtime metrics are enabled

The SignalFx Instrumentation for .NET logs the profiling configuration using `INF` log messages during startup. 
Verify that the `runtime_metrics_enabled` key is set to `true` under `TRACER CONFIGURATION`.

### Check where the metrics are being exported

Check the value of the `metrics_agent_url` key under `TRACER CONFIGURATION`.

### Verify the Collector setup

* Make sure that the [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector) is running.
* Make sure that a `signalfx` receiver and a `signalfx` exporter are configured in the Collector.
* Make sure that the `access_token` and `realm` fields are [configured](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/signalfxexporter#metrics-configuration).
* Check that the metrics pipeline is configured to use
the `signalfx` receiver and `signalfx` exporter.
