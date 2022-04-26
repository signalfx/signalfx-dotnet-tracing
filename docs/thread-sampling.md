
# About the .NET thread sampler

> :construction: &nbsp;Status: Experimental - exported telemetry and
> configuration settings may change.

The SignalFx Instrumentation for .NET includes a continuous thread sampler
that can be enabled with a configuration setting. This sampler periodically captures
the call stack state for all .NET threads and sends these
to the Splunk Observability Cloud as logs. You can then view a flame graph of application
call stacks and inspect individual code-level call stacks for relevant traces.

## How does the thread sampler work?

The profiler leverages [.NET profiling](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
to perform periodic call stack sampling. For every sampling period,
the runtime is suspended
and the samples for all managed thread are saved into the buffer,
then the runtime resumes.

The separate managed-thread is processing data from the buffer
and sends it to the OpenTelemetry Collector.

To make the process more efficient, the sampler uses two independent buffers
to store samples alternatively.

Stack trace data is embedded as a string inside of an OTLP logs payload. The
[Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
detects profiling data inside OTLP logs and forwards it to
Splunk APM.

# Requirements

* .NET Core 3.1 or .NET 5.0 or higher (`ICorProfilerInfo10` available in runtime).
* [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
version 0.34.0 or higher.
_Sending profiling data directly to ingest is not supported at this time_.

# Enable the profiler

To enable the profiler, set the `SIGNALFX_PROFILER_ENABLED` environment variable
to `true` for your .NET process.

# Configuration settings

Please check [description](internal-config.md) for the following environment variables

* `SIGNALFX_PROFILER_LOGS_ENDPOINT`,
* `SIGNALFX_PROFILER_ENABLED`,
* `SIGNALFX_PROFILER_CALL_STACK_INTERVAL`.

> We strongly recommend using defaults for `SIGNALFX_PROFILER_CALL_STACK_INTERVAL`.

# Escape hatch

The profiler limits its own behavior when both buffers
used to store sampled data are full.

This scenario might happen when the data processing thread is not able
to send data to collector in the given period of time.

Thread sampler will resume when any of the buffers are empty.

# Troubleshooting the .NET profiler

## How do I know if it's working?

At the startup, the SignalFx Instrumentation for .NET will log the string
"Thread sampling initialized" at `INF`. You can grep for this in
the logs to see something like this:

```text
2022-01-13 13:30:02.601 +01:00 [INF] Thread sampling initialized.  { MachineName: ".", Process: "[11524 dotnet]", AppDomain: "[1 Samples.Profiling]", AssemblyLoadContext: "\"Default\" System.Runtime.Loader.DefaultAssemblyLoadContext #1", TracerVersion: "0.2.0.0" }
```

## How can I see the profiler configuration?

The SignalFx Instrumentation for .NET logs the profiling configuration
at `INF` during the startup. You can grep for the string `TRACER CONFIGURATION`
to see the configuration.

## What does the escape hatch do?

The escape hatch automatically discards profiling data
if the ingest limit has been reached.

If the escape hatch activates, it logs the following message:

`Skipping a thread sample period, buffers are full.`

You can also look for `"** THIS WILL RESULT IN LOSS OF PROFILING DATA **"`.

If you see these log messages, check the configuration and communication layer
between your process and the Collector.

## What if I'm on an unsupported .NET version?

If you want to use the profiler and see this in your logs, you must upgrade
your .NET version to .NET Core 3.1 or .NET 5.0 or higher.
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
handles this, but a custom configuration might have overridden some settings.
Make sure that an OTLP HTTP _receiver_ is configured in the collector
and that an exporter is configured for `splunk_hec` export.
Ensure that the `token` and `endpoint` fields are [correctly configured](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/splunkhecreceiver#configuration).
Lastly, double check that the logs _pipeline_ is configured to use
the OTLP HTTP receiver and `splunk_hec` exporter.

## Can I tell the sampler to ignore some threads?

There is no such functionality. All managed threads are captured by the profiler.
