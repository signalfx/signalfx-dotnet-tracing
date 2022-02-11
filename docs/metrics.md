# Metrics

> :construction: &nbsp;Status: Experimental - exported telemetry and
> configuration settings may change.

The SignalFx Instrumentation for .NET is able to export following metrics:

- [.NET runtime metrics](#.net-runtime-metrics)
- [Trace metrics](#trace-metrics)

## .NET runtime metrics

The SignalFx Instrumentation for .NET includes automatic runtime metrics collection.
When enabled, metrics are periodically captured and sent
to Splunk Observability Cloud.

To enable runtime metrics, set the `SIGNALFX_RUNTIME_METRICS_ENABLED` environment
variable to `true` for your .NET process.

### Supported runtime metrics

The following metrics are collected by default after enabling .NET metrics.
To learn about differences between SignalFx metric types, visit [documentation](https://docs.splunk.com/Observability/metrics-and-metadata/metric-types.html#metric-types).

| Metric | Description | Type |
| --- | --- | --- |
| `runtime.dotnet.aspnetcore.connections.current` | The current number of active HTTP connections to the web server. (.NET Core only) | Gauge |
| `runtime.dotnet.aspnetcore.connections.queue_length` | The current length of the HTTP connection queue. (.NET Core only) | Gauge |
| `runtime.dotnet.aspnetcore.connections.total` | The total number of HTTP connections to the web server. (.NET Core only) | Gauge |
| `runtime.dotnet.aspnetcore.requests.current` | The current number of HTTP requests that have started, but not yet stopped. (.NET Core only) | Gauge |
| `runtime.dotnet.aspnetcore.requests.failed` | The number of failed HTTP requests received by the server. (.NET Core only) | Gauge |
| `runtime.dotnet.aspnetcore.requests.queue_length` | The current length of the HTTP request queue. | Gauge |
| `runtime.dotnet.aspnetcore.requests.total` | The total number of HTTP requests received by the server. (.NET Core only) | Gauge |
| `runtime.dotnet.cpu.percent` | The percentage of total CPU used by the application. | Gauge |
| `runtime.dotnet.cpu.system` | The number of milliseconds executing outside the kernel. | Gauge |
| `runtime.dotnet.cpu.user` | The number of milliseconds executing in the kernel. | Gauge |
| `runtime.dotnet.exceptions.count` | The number of exceptions. | Gauge |
| `runtime.dotnet.gc.count.gen0` | The number of gen0 garbage collections. | Counter |
| `runtime.dotnet.gc.count.gen1` | The number of gen1 garbage collections. | Counter |
| `runtime.dotnet.gc.count.gen2` | The number of gen2 garbage collections. | Counter |
| `runtime.dotnet.gc.memory_load` | The percentage of the total memory used by the process. The GC changes its behavior when this value gets above 85. (.NET Core only) | Gauge |
| `runtime.dotnet.gc.pause_time` | The amount of time the GC paused the application threads. (.NET Core only) | Gauge |
| `runtime.dotnet.gc.size.gen0` | The size of the gen0 heap. | Gauge |
| `runtime.dotnet.gc.size.gen1` | The size of the gen1 heap. | Gauge |
| `runtime.dotnet.gc.size.gen2` | The size of the gen2 heap. | Gauge |
| `runtime.dotnet.gc.size.loh` | The size of the large object heap.| Gauge |
| `runtime.dotnet.mem.committed` | Memory usage. | Gauge |
| `runtime.dotnet.threads.contention_count` | The number of times a thread stopped to wait on a lock. | Counter |
| `runtime.dotnet.threads.contention_time` | The cumulated time spent by threads waiting on a lock (.NET Core only). | Gauge |
| `runtime.dotnet.threads.count` | The number of threads that were running in the process. | Counter |
| `runtime.dotnet.threads.workers_count` | The number of threads that existed in the threadpool. (.NET Core only) | Gauge |

### Additional permissions for IIS

The .NET Framework collects metrics using performance counters.
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

### Supported trace metrics

| Metric | Description | Type |
| --- | --- | --- |
| `signalfx.tracer.queue.enqueued_traces` | The total number of traces pushed into the queue. | Counter |
| `signalfx.tracer.queue.dequeued_traces` | The number of traces pulled from the queue for flushing. | Counter |
| `signalfx.tracer.queue.enqueued_spans` | The total number of spans pushed into the queue. | Counter |
| `signalfx.tracer.queue.dequeued_spans` | The number of spans pulled from the queue for flushing. | Counter |
| `signalfx.tracer.queue.dropped_traces` | The total number of traces dropped due to a full queue. | Counter |
| `signalfx.tracer.queue.dropped_spans` | The total number of spans dropped due to a full queue. | Counter |
| `signalfx.tracer.heartbeat` | The total number of tracers. | Gauge |
