
# About AlwaysOn memory profiling for .NET

The SignalFx Instrumentation for .NET includes a memory profiler for Splunk
APM that can be enabled with a setting. The profiler samples allocations,
captures the call stack state for the .NET thread that triggered the allocation,
and sends the telemetry to Splunk Observability Cloud.

Use the memory allocation data, together with the stack traces and .NET runtime metrics,
to investigate memory leaks and unusual consumption patterns in AlwaysOn Profiling.

## How does the memory profiler work?

The profiler leverages [.NET profiling](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
to perform allocation sampling.
For every sampled allocation, allocation amount together with stack
trace of the thread that triggered the allocation, and associated
span context, are saved into buffer.

The managed thread shared with [CPU Profiler](../always-on-profiling.md)
processes the data from the buffer and sends it to the OpenTelemetry Collector.

Stack trace data is embedded as a string inside of the OTLP logs payload. The
[Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
detects profiling data inside OTLP logs and forwards it to
the Splunk APM.

# Requirements

* .NET 5.0 or higher (`ICorProfilerInfo12` available in runtime).
* [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
version 0.34.0 or higher.
_Sending profiling data directly to ingest is not supported at this time_.

# Enable the profiler

To enable the profiler, set the `SIGNALFX_PROFILER_MEMORY_ENABLED` environment variable
to `true` for your .NET process.

# Configuration settings

Make sure you're following the documentation for the following environment variables:

* [`SIGNALFX_PROFILER_MEMORY_ENABLED`](../internal/internal-config.md#internal-settings)
* [`SIGNALFX_PROFILER_LOGS_ENDPOINT`](../advanced-config.md#alwayson-profiling-settings)

> _NOTE_: `SIGNALFX_PROFILER_LOGS_ENDPOINT` affects both CPU and Memory profiling.

# Escape hatch

The profiler limits its own behavior when buffer
used to store allocation samples is full.

Current maximum size of the buffer is 200 KiB.

This scenario might happen when the data processing thread is not able
to send data to collector in the given period of time.

# Troubleshooting the .NET profiler

## How do I know if it's working?

At the startup, the SignalFx Instrumentation for .NET will log the string
`AlwaysOnProfiler::MemoryProfiling started` at `info` log level.

You can grep for this in the native logs for the instrumentation
to see something like this:

```text
10/12/22 12:10:31.962 PM [12096|22036] [info] AlwaysOnProfiler::MemoryProfiling started.
```

If you enable the memory profiler on an unsupported runtime version
an entry in the managed logs for the instrumentation similar to this appears:

```text
2022-10-12 12:37:18.640 +02:00 [WRN] Memory profiling enabled but not supported.
```

## How can I see the profiler configuration?

The SignalFx Instrumentation for .NET logs the profiling configuration
at `INF` log level during the startup. You can grep for the string `TRACER CONFIGURATION`
to see the configuration.

Ensure `memory_profiling_enabled` is set to `true`.
Ensure `logs_endpoint_url` points to the deployed [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector).

## What does the escape hatch do?

The escape hatch automatically discards captured allocation data
if the ingest limit has been reached.

If the escape hatch activates, it logs the following message:

`Discarding captured allocation sample. Allocation buffer is full.`

If you see these log messages, check the configuration and communication layer
between your process and the Collector.

## What if I'm on an unsupported .NET version?

If you want to use the profiler and see this in your logs, you must upgrade
your .NET version to .NET 5.0 or higher.
None of the .NET Framework versions is supported.

## Why is the OTLP/logs exporter complaining?

Collector configuration issues may prevent logs from being exported and profiling
data from showing in Splunk Observability Cloud.

Check for the following common issues:

* Look at the values of the SignalFx Instrumentation for .NET's configuration,
especially `SIGNALFX_PROFILER_LOGS_ENDPOINT`. They are logged at startup.
* Verify that a collector is actually running at that endpoint and that the
application host/container can resolve any hostnames
and connect to the given OTLP port (default: 4318).
* Make sure you are running the [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
and that the version is 0.34.0 or higher.
Other collector distributions might not be able to correctly route
the log data containing profiles.
* Make sure that the collector is configured correctly to handle profiling data.
By default, the [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
handles this, but a custom configuration might override some settings.
Make sure that an OTLP HTTP _receiver_ is configured in the collector
and that an exporter is configured for `splunk_hec` export.
Ensure that the `token` and `endpoint` fields are [correctly configured](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/splunkhecreceiver#configuration).
Lastly, double check that the logs _pipeline_ is configured to use
the OTLP HTTP receiver and `splunk_hec` exporter.
