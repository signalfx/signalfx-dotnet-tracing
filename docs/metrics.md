# Metrics

> :construction: &nbsp;Status: Experimental - exported telemetry and
> configuration settings may change.

The SignalFx Instrumentation for .NET includes automatic runtime metrics collection.
When enabled, metrics are periodically captured and sent
to Splunk Observability Cloud.

To enable runtime metrics, set the `SIGNALFX_RUNTIME_METRICS_ENABLED` environment
variable to `true` for your .NET process.

> Runtime metrics are automatically enabled when [AlwaysOn memory profiling](internal/memory-profiling.md)
is enabled. Memory profiling setting overrides metrics setting (runtime metrics
will always be enabled if memory profiling is enabled,
even if `SIGNALFX_METRICS_NetRuntime_ENABLED` is set to `false`).

The following metrics are collected by default after enabling .NET metrics.
To learn about differences between SignalFx metric types, visit [documentation](https://docs.splunk.com/Observability/metrics-and-metadata/metric-types.html#metric-types).

## .NET runtime metrics

Names and metric structure of the metrics exported are aligned with the OpenTelemetry
implementation from the [Runtime](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/bc947a00c3f859cc436f050e81172fc1f8bc09d7/src/OpenTelemetry.Instrumentation.Runtime) package.

| Metric                                                 | Description                                                                                                               | Type              |
|:-------------------------------------------------------|:--------------------------------------------------------------------------------------------------------------------------|:------------------|
| `process.runtime.dotnet.exceptions.count`              | Count of exceptions since the previous observation.                                                                       | Counter           |
| `process.runtime.dotnet.gc.collections.count`          | Number of garbage collections since the process started.                                                                  | CumulativeCounter |
| `process.runtime.dotnet.gc.heap.size`                  | Heap size, as observed during the last garbage collection.                                                                | Gauge             |
| `process.runtime.dotnet.gc.objects.size`               | Count of bytes currently in use by live objects in the GC heap.                                                           | Gauge             |
| `process.runtime.dotnet.gc.allocations.size`           | Count of bytes allocated on the managed GC heap since the process started. (.NET Core only)                               | CumulativeCounter |
| `process.runtime.dotnet.gc.committed_memory.size`      | Amount of committed virtual memory for the managed GC heap, as observed during the last garbage collection. (.NET 6 only) | Gauge             |
| `process.runtime.dotnet.gc.pause.time`                 | Number of milliseconds spent in GC pause. (.NET Core only)(equivalent not yet available on OpenTelemetry side)            | Counter           |
| `process.runtime.dotnet.monitor.lock_contention.count` | Contentions count when trying to acquire a monitor lock since the process started.                                        | CumulativeCounter |
| `process.runtime.dotnet.thread_pool.threads.count`     | Number of thread pool threads, as observed during the last measurement. Only available for .NET Core.                     | Gauge             |

## Process metrics

Names and metric structure of the metrics exported are aligned with the OpenTelemetryimplementation from the [Process](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/926386e68c9066e8032853e8309abdf4088d8dca/src/OpenTelemetry.Instrumentation.Process) package.

| Metric                               | Description                                                                                                                         | Type              |
|:-------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------|:------------------|
| `process.memory.usage`               | The amount of physical memory allocated for this process.                                                                           | Gauge             |
| `process.memory.virtual`             | The amount of committed virtual memory for this process.                                                                            | Gauge             |
| `process.cpu.time`                   | Total CPU seconds broken down by different states(`user`,`system`).                                                                 | CumulativeCounter |
| `process.cpu.utilization`            | Difference in process.cpu.time since the last measurement, divided by the elapsed time and number of CPUs available to the process. | Gauge             |
| `process.threads`                    | Process threads count.                                                                                                              | Gauge             |

## ASP.NET Core metrics

| Metric                                                  | Description                                                                                                                       | Type               |
|:--------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------|:-------------------|
| `signalfx.dotnet.aspnetcore.connections.current`         | The current number of active HTTP connections to the web server. (.NET Core only)                                                 | Gauge              |
| `signalfx.dotnet.aspnetcore.connections.queue_length`    | The current length of the HTTP connection queue. (.NET Core only)                                                                 | Gauge              |
| `signalfx.dotnet.aspnetcore.connections.total`           | The total number of HTTP connections to the web server. (.NET Core only)                                                          | Gauge              |
| `signalfx.dotnet.aspnetcore.requests.current`            | The current number of HTTP requests that have started, but not yet stopped. (.NET Core only)                                      | Gauge              |
| `signalfx.dotnet.aspnetcore.requests.failed`             | The number of failed HTTP requests received by the server. (.NET Core only)                                                       | Gauge              |
| `signalfx.dotnet.aspnetcore.requests.queue_length`       | The current length of the HTTP request queue.                                                                                     | Gauge              |
| `signalfx.dotnet.aspnetcore.requests.total`              | The total number of HTTP requests received by the server. (.NET Core only)                                                        | Gauge              |

## Additional permissions for IIS

The .NET Framework collects gc heap size metrics using performance counters.
To let service accounts and IIS application pool accounts access counter data,
add them to the `Performance Monitoring Users` group.

IIS application pools use special accounts that do not appear in the list
of users. To add IIS application pool accounts to the `Performance Monitoring Users`
group, search for `IIS APPPOOL\<name-of-the-pool>`. For example, the user for
the `DefaultAppPool` pool is `IIS APPPOOL\DefaultAppPool`.

The following example shows how to add an IIS application pool account to
the `Performance Monitoring Users` group from a command prompt with
Administrator permissions:

```batch
net localgroup "Performance Monitor Users" "IIS APPPOOL\DefaultAppPool" /add
```

## Trace metrics

The SignalFx Instrumentation for .NET supports trace metrics collection.

To enable additional metrics related to traces, set the `SIGNALFX_TRACE_METRICS_ENABLED`
environment variable to `true` for your .NET process.

| Metric                                   | Description                                               | Type     |
|:-----------------------------------------|:----------------------------------------------------------|:---------|
| `signalfx.tracer.queue.enqueued_traces`  | The total number of traces pushed into the queue.         | Counter  |
| `signalfx.tracer.queue.dequeued_traces`  | The number of traces pulled from the queue for flushing.  | Counter  |
| `signalfx.tracer.queue.enqueued_spans`   | The total number of spans pushed into the queue.          | Counter  |
| `signalfx.tracer.queue.dequeued_spans`   | The number of spans pulled from the queue for flushing.   | Counter  |
| `signalfx.tracer.queue.dropped_traces`   | The total number of traces dropped due to a full queue.   | Counter  |
| `signalfx.tracer.queue.dropped_spans`    | The total number of spans dropped due to a full queue.    | Counter  |
| `signalfx.tracer.heartbeat`              | The total number of tracers.                              | Gauge    |
