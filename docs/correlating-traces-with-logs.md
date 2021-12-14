# Correlating traces with logs

You can link individual log entries with trace IDs and span IDs associated with
corresponding events. If your application uses a supported logger, enable trace
injection to automatically include trace context in your application's logs.

To inject traces in logs, enable log correlation by setting the environment variable
`SIGNALFX_LOGS_INJECTION=true` before running your instrumented application.

If your logger uses JSON, the tracing library automatically handles trace ID
injection.

If your logger uses a raw format, manually configure your logger to include
the trace ID and span ID.

Supported loggers:

- Log4Net
- NLog
- Serilog
- ILogger (Microsoft.Extensions.Logging)

## Supported Loggers

These are the supported logging frameworks.

### Log4Net

- Versions ≥ 1.2.11 (.NET Framework)
- Versions ≥ 2.0.12 (.NET core)

Available layouts:

- JSON format: `SerializedLayout` (from the `log4net.Ext.Json` NuGet package)
- Raw format: `PatternLayout` (requires manual configuration)

### NLog

- Versions ≥ 1.0.0.505 (.NET Framework)
- Versions ≥ 4.5.11 (.NET Core)

Available layouts:

- JSON format: `JsonLayout`
- Raw format: Custom layout (requires manual configuration)

### Serilog

- Versions ≥ 2.0.0 (.NET Framework)
- Versions ≥ 2.0.0 (.NET Core)

Available layouts:

- JSON format: `JsonFormatter`
- JSON format: `CompactJsonFormatter` (from the `Serilog.Formatting.Compact`
  NuGet package)
- Raw format: output template (requires manual configuration)

\* Log transformation rules must be configured as Serilog doesn't support
property names with '`.`'. Find more information about the log processing rules
[here](https://docs.splunk.com/Observability/logs/processors.html#logs-processors).
Configure rules for:

- `service_name` => `service.name`
- `service_version` => `service.version`
- `deployment_environment` => `deployment.environment`

## Fields injected into log context

- `trace_id`
- `span_id`
- `service.name` - [`SIGNALFX_SERVICE_NAME`](advanced-config.md) configuration option
- `service.version` - [`SIGNALFX_VERSION`](advanced-config.md) configuration option
- `deployment.environment` - [`SIGNALFX_ENV`](advanced-config.md) configuration option
