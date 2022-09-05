# Metrics

> :construction: &nbsp;Status: Experimental - exported telemetry and
> configuration settings may change.

The SignalFx Instrumentation for .NET includes automatic runtime metrics collection.
When enabled, metrics are periodically captured and sent
to Splunk Observability Cloud.

To enable runtime metrics, set the `SIGNALFX_RUNTIME_METRICS_ENABLED` environment
variable to `true` for your .NET process.

The following metrics are collected by default after enabling .NET metrics.
To learn about differences between SignalFx metric types, visit [documentation](https://docs.splunk.com/Observability/metrics-and-metadata/metric-types.html#metric-types).

## .NET runtime metrics

The names and the metric structure of the metrics exported are aligned with OpenTelemetry
[implementation](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/bc947a00c3f859cc436f050e81172fc1f8bc09d7/src/OpenTelemetry.Instrumentation.Runtime).

| Metric                                                  | Description                                                                                                                      | Type               |
|:--------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------|:-------------------|
| `process.runtime.dotnet.exceptions.count`               | The count of exceptions that have been thrown in managed code since the previous observation.                                    | Counter            |
| `process.runtime.dotnet.gc.collections.count`           | The number of garbage collections that have occurred since the process was started.                                              | CumulativeCounter  |
| `process.runtime.dotnet.gc.heap.size`                   | The heap size, as observed during the latest garbage collection.                                                                 | Gauge              |
| `process.runtime.dotnet.gc.allocations.size`            | The count of bytes allocated on the managed GC heap since the process was started. (.NET Core only)                              | CumulativeCounter  |
| `process.runtime.dotnet.gc.committed_memory.size`       | The amount of committed virtual memory for the managed GC heap, as observed during the latest garbage collection. (.NET 6 only)  | Gauge              |
| `process.runtime.dotnet.monitor.lock_contention.count`  | The number of times there was contention when trying to acquire a monitor lock since the process was started.                    | CumulativeCounter  |
| `process.runtime.dotnet.thread_pool.threads.count`      | The number of thread pool threads, as observed during the latest measurement. (.NET Core only)                                   | Gauge              |

## Process metrics

| Metric                                                  | Description                                                                                                                       | Type               |
|:--------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------|:-------------------|
| `runtime.dotnet.cpu.percent`                            | The percentage of total CPU used by the application.                                                                              | Gauge              |
| `runtime.dotnet.cpu.system`                             | The number of milliseconds executing outside the kernel.                                                                          | Gauge              |
| `runtime.dotnet.cpu.user`                               | The number of milliseconds executing in the kernel.                                                                               | Gauge              |
| `runtime.dotnet.threads.count`                          | The number of threads that were running in the process.                                                                           | Counter            |

## ASP.NET Core metrics

| Metric                                                  | Description                                                                                                                       | Type               |
|:--------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------|:-------------------|
| `runtime.dotnet.aspnetcore.connections.current`         | The current number of active HTTP connections to the web server. (.NET Core only)                                                 | Gauge              |
| `runtime.dotnet.aspnetcore.connections.queue_length`    | The current length of the HTTP connection queue. (.NET Core only)                                                                 | Gauge              |
| `runtime.dotnet.aspnetcore.connections.total`           | The total number of HTTP connections to the web server. (.NET Core only)                                                          | Gauge              |
| `runtime.dotnet.aspnetcore.requests.current`            | The current number of HTTP requests that have started, but not yet stopped. (.NET Core only)                                      | Gauge              |
| `runtime.dotnet.aspnetcore.requests.failed`             | The number of failed HTTP requests received by the server. (.NET Core only)                                                       | Gauge              |
| `runtime.dotnet.aspnetcore.requests.queue_length`       | The current length of the HTTP request queue.                                                                                     | Gauge              |
| `runtime.dotnet.aspnetcore.requests.total`              | The total number of HTTP requests received by the server. (.NET Core only)                                                        | Gauge              |

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
