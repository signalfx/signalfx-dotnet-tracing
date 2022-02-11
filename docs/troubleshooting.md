# Troubleshooting

Check if you are not hitting one of the issues listed below.

## Linux instrumentation not working

The proper installation package needs to be selected when deploying to Linux,
eg.: the default Microsoft .NET images are based on Debian and should use
the `deb` package.

If you are not sure what is the Linux distribution being used try the following commands:

```terminal
lsb_release -a
cat /etc/*release
cat /etc/issue*
cat /proc/version
```

## High CPU usage

Check if you have not enabled the auto-instrumentation globally
by setting the environment variables on system or user level.

If it was really intended, then use the
`SIGNALFX_PROFILER_EXCLUDE_PROCESSES` and `SIGNALFX_PROFILER_PROCESSES`
environment variables to include/exclude applications from the tracing auto-instrumentation.
These are ";" delimited lists that control the inclusion/exclusion of processes.

## Investigating other issues

Check that all [settings](advanced-config.md) are properly configured.

If you need to check the environment variables for a process on Windows, use a tool
like [Process Explorer](https://docs.microsoft.com/en-us/sysinternals/downloads/process-explorer).
On Linux, run: `cat /proc/<pid>/environ`
where `<pid>` is the Process ID.

Enable debug logging if none of the suggestions solved your issue.
Follow these steps to enable debug logging for .NET instrumentation:

Set the environment variable `SIGNALFX_TRACE_DEBUG` to `true` before
the instrumented process starts.
By default, the library writes the log files under the below predefined locations.
If needed, change the default location by updating the environment variable
`SIGNALFX_TRACE_LOG_DIRECTORY` to an appropriate path.

On Linux, the default log location is `/var/log/signalfx/dotnet/`. If it does not
exist, run `/opt/signalfx/createLogPath.sh` to create it with the required permissions.

On Windows, the default log location is `%ProgramData%\SignalFx .NET Tracing\logs\`

After obtaining the logs, remember to remove the environment variable
`SIGNALFX_TRACE_DEBUG` to avoid unnecessary overhead.

The SignalFx Instrumentation for .NET logs the profiling configuration
using `INF` log messages during startup. It can be used for troubleshooting.

### Check if runtime metrics are enabled

Verify that the `runtime_metrics_enabled` key is set to `true` under `TRACER CONFIGURATION` log entry.

### Verify the Collector setup for traces

* Make sure that the [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector) is running.
* Make sure that a `zipkin` receiver and a `sapm` exporter are configured in the Collector.
* Make sure that the `access_token` and `endpoint` fields are [configured](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/sapmexporter#configuration).
* Check that the traces pipeline is configured to use
the `zipkin` receiver and `sapm` exporter.

### Verify the Collector setup for metrics

* Make sure that the [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector) is running.
* Make sure that a `signalfx` receiver and a `signalfx` exporter are configured in the Collector.
* Make sure that the `access_token` and `realm` fields are [configured](https://github.com/open-telemetry/opentelemetry-collector-contrib/tree/main/exporter/signalfxexporter#metrics-configuration).
* Check that the metrics pipeline is configured to use
the `signalfx` receiver and `signalfx` exporter.
