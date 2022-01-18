# About the .NET runtime metrics

The SignalFx Instrumentation for .NET includes an automatic runtime metrics collection.
This feature can be enabled with configuration setting and is disabled by default. 
When enabled, metrics are periodically captured and sent to Splunk Observability Cloud.

# Enable runtime metrics

To enable runtime metrics, set the `SIGNALFX_RUNTIME_METRICS_ENABLED` environment variable
to `true` for your .NET process. 

# Metrics exporter settings

## Default configuration
Metrics are sent to local [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
that forwards them to Splunk Observability Cloud. For guidance on how to setup required components of metrics pipeline in the collector, see 
[receiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/signalfxreceiver) and [exporter](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/signalfxexporter) documentation.

## Exporting directly to Splunk Observabilty Cloud
If you prefer to send metrics directly to Splunk Observability Cloud, configure the following settings:

| Setting | Value | Notes |
|-|-|-|
| `SIGNALFX_ACCESS_TOKEN` | *organization access tokens* | (__required__) See [here](https://docs.splunk.com/Observability/admin/authentication-tokens/org-tokens.html) to learn how to obtain one. |
| `SIGNALFX_REALM` | *ingest realm* | (__recommended__) Set to send telemetry directly to Splunk Observability Cloud. If configured, metrics are sent to `https://ingest.<SIGNALFX_REALM>.signalfx.com/v2/datapoint`. To find your realm, open Splunk Observability Cloud, click Settings, and click on your user name. __NOTE: affects traces endpoint as well.__ |
| `SIGNALFX_METRICS_ENDPOINT_URL` | *valid endpoint url* | (__optional__) Overrides other configuration settings for metrics ingestion endpoint. |

__IMPORTANT:__ One of `SIGNALFX_REALM` or `SIGNALFX_METRICS_ENDPOINT_URL` has to be configured in addition to `SIGNALFX_ACCESS_TOKEN`.

# Troubleshooting

## How to check if runtime metrics are enabled?

The SignalFx Instrumentation for .NET logs the profiling configuration at `INF` during the startup. 
Verify that `runtime_metrics_enabled` key is set to `true` under `TRACER CONFIGURATION`.

## How to check where are metrics exported?

Check the value of the `metrics_agent_url` key under `TRACER CONFIGURATION`.

## How to verify collector setup?

* Make sure you are running the [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
* Make sure that the collector is configured correctly to handle metrics.
* Make sure that an `signalfx` receiver and `signalfx` exporter are configured in the collector.
Ensure that the `access_token` and `realm` fields are [correctly configured](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/signalfxexporter#metrics-configuration).
Lastly, double check that the metrics pipeline is configured to use
the `signalfx` receiver and `signalfx` exporter.
