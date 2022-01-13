
> :construction: The thread sampler feature is experimental.

# About the .NET thread sampling

The SignalFx Instrumentation for .NET includes a continuous thread sampler
that can be enabled with a configuration setting. This sampler periodically captures
the call stack state for all .NET threads and sends these
to the Splunk Observability Cloud. You can then view a flamegraph of application
call stacks and inspect individual code-level call stacks for relevant traces.

## How does the thread sampler work?

The profiler leverages the [.NET profiling](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/profiling/)
to perform periodic call stack sampling. For every sampling period,
the runtime is suspended
then the samples for all managed thread are saved into the buffer
and the runtime is resumed.

The separate managed-thread is processing data from the buffer
and sents it to the Collector.

To make the process more efficient sampler is utilizing two independent buffers
used to store samples anternately.

Stack trace data is embedded as a string inside of an OTLP logs payload. The
[Splunk OpenTelemetry Connector](https://github.com/signalfx/splunk-otel-collector)
will detect this profiling data inside of OTLP logs and will help it along
its ingest path.

# Requirements

* .NET Core 3.0 or .NET 5.0 or higher (`ICorProfilerInfo10` available in runtime).
* [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
version 0.33.1 or higher.
_Sending profiling data directly to ingest is not supported at this time_.
* Profiler is enabled at startup (disabled by default, see the Configuration section)

# Enable the profiler

To enable the profiler, set the `SIGNALFX_THREAD_SAMPLING_ENABLED` environment variable
to `true` to your .NET process.

# Configuration settings

Please check [description](internal-config.md) for following environment variables

* `SIGNALFX_LOGS_ENDPOINT_URL`,
* `SIGNALFX_THREAD_SAMPLING_ENABLED`,
* `SIGNALFX_THREAD_SAMPLING_PERIOD`.

> We strongly recommend using defaults for these settings.

# Escape hatch

The profiler limits its own behavior when both both buffers
used to store sampled data are full.

This scenario might happen when the thread to process data is not able
to send data to collector in the given period of time.

Thread sampler will resume when any of buffers will be empty.

# FAQ / Troubleshooting

## How do I know if it's working?

At startup, the agent will log the string "Thread sampling initialized" at `INF`.
You can grep for this in your logs to see something like this:

```text
2022-01-13 13:30:02.601 +01:00 [INF] Thread sampling initialized.  { MachineName: ".", Process: "[11524 dotnet]", AppDomain: "[1 Samples.Profiling]", AssemblyLoadContext: "\"Default\" System.Runtime.Loader.DefaultAssemblyLoadContext #1", TracerVersion: "0.2.0.0" }
```

## How can I see the profiler configuration?

The agent logs the profiling configuration at `INF` during startup. You can grep
for the string `TRACER CONFIGURATION` to see configuration.

## What about this escape hatch?

If the escape hatch becomes active, it will log with
`Skipping a thread sample period, buffers are full.`
(you can grep for this in the logs).
You may also look for `"** THIS WILL RESULT IN LOSS OF PROFILING DATA **"`
as a big hint that things are not well.

If you see such logs, please check the configuration and communication layer
between your process and the Collector.

## What if I'm on an unsupported .NET version?

If you want to use the profiler and see this in your logs, you must upgrade
your .NET version to .NET Core 3.0 or .NET 5.0 or higher.
Any of .NET Framework versions is not supported.

## Why is the OTLP/logs exporter complaining?

Collector configuration issues may prevent logs from being exported and profiling
data from showing in Splunk Observability Cloud.

Check for the following common issues:

* Look at the values of the agent's configuration,
especially `SIGNALFX_LOGS_ENDPOINT_URL`. Hint: they are logged at startup (see above).
* Verify that a collector is actually running at that endpoint and that the
application host/container can resolve any hostnames
and actually connect to the given OTLP port (default: 4318)
* Make sure you are running the [Splunk OpenTelemetry Connector](https://github.com/signalfx/splunk-otel-collector)
and that the version is 0.33.1 or greater.
Other collector distributions may not be able to route
the log data containing profiles correctly.
* Make sure that the collector is configured correctly to handle profiling data.
By default, the [Splunk OpenTelemetry Connector](https://github.com/signalfx/splunk-otel-collector)
handles this, but a custom configuration might have overridden some settings.
Make sure that an OTLP HTTP _receiver_ is configured in the collector
and that an exporter is configured for `splunk_hec` export.
Ensure that the `token` and `endpoint` fields are [correctly configured](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/receiver/splunkhecreceiver#configuration).
Lastly, double check that the logs _pipeline_ is configured to use
the OTLP HTTP receiver and `splunk_hec` exporter.

## Can I tell the sampler to ignore some threads?

There is no such functionality. All managed threads are captured by the profiler.
