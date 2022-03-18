# Correlating traces with logs

If your application uses a supported logger,
you can configure log correlation to
include trace context in your application's logs.

To inject trace context fields in logs,
enable log correlation by setting the environment variable
`SIGNALFX_LOGS_INJECTION=true` before running your instrumented application.

If your logger uses JSON logging format,
then SignalFx Instrumentation for .NET automatically adds
span context to the logs.

If your logger uses a different format,
you may have to manually configure your
logger to include the trace context fields.

## Fields injected into log context

- `trace_id`
- `span_id`
- `service.name` (`service_name` when using Serilog) -
  [`SIGNALFX_SERVICE_NAME`](advanced-config.md) setting
- `service.version` (`service_version` when using Serilog) -
  [`SIGNALFX_VERSION`](advanced-config.md) setting
- `deployment.environment` (`deployment_environment` when using Serilog) -
  [`SIGNALFX_ENV`](advanced-config.md) setting

## Supported logging libraries

### Log4Net

- Versions: 1.0.0 ≤ 2.*.*

Available layouts:

- JSON format: `SerializedLayout` (from the `log4net.Ext.Json` NuGet package)
- Raw format: `PatternLayout` (requires manual configuration)

Find samples here: [Log4NetExample](../tracer/samples/AutomaticTraceIdInjection/Log4NetExample).

### NLog

- Versions: 1.0.0.505 ≤ 4.*.*

Available layouts:

- JSON format: `JsonLayout`
- Raw format: Custom layout (requires manual configuration)

Find samples here:

- [NLog 4.0.x](../tracer/samples/AutomaticTraceIdInjection/NLog40Example).
- [NLog 4.5.x](../tracer/samples/AutomaticTraceIdInjection/NLog45Example).
- [NLog 4.6.x](../tracer/samples/AutomaticTraceIdInjection/NLog45Example).

### Serilog

- Versions: 1.4.0 ≤ 2.*.*

Regardless of the output layout, your `LoggerConfiguration` must be
enriched from the LogContext to extract the trace context
that is automatically injected.

```csharp
var loggerConfiguration = new LoggerConfiguration()
    .Enrich.FromLogContext() // addition
```

Supported layouts:

- JSON format: `JsonFormatter`
- JSON format: `CompactJsonFormatter` (from the `Serilog.Formatting.Compact`
  NuGet package)
- Raw format: output template (requires manual configuration)

When using the output template you can either use `{Properties}`
to print out all contextual properties.

Alternatively, for more fine-grained control,
you can use the trace context fields explicitly.
The values MUST be surrounded with a quotation mark.
For instance, you can use following output template,
which also transforms the field name
(the log transformation step would not be required):

```csharp
"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] trace_id=\"{trace_id}\" span_id=\"{span_id}\" service.name=\"{service_name}\" service.version=\"{service_version}\" deployment.environment=\"{deployment_environment}\"{NewLine}{Message:lj}{NewLine}{Exception}"
```

The log transformation rules must be configured as Serilog does not support
property names with '`.`'. Find more information about the log processing rules
[here](https://docs.splunk.com/Observability/logs/processors.html#logs-processors).
Configure rules for:

- `service_name` => `service.name`
- `service_version` => `service.version`
- `deployment_environment` => `deployment.environment`

Find samples here: [SerilogExample/Program.cs](../tracer/samples/AutomaticTraceIdInjection/SerilogExample/Program.cs).

### ILogger (Microsoft.Extensions.Logging.Abstractions)

- Versions: 2.0.0 ≤ 6.*.*

Available layouts:

- JSON format: `json` (from the NetEscapades.Extensions.Logging)

Find samples here: [MicrosoftExtensionsExample](../tracer/samples/AutomaticTraceIdInjection/MicrosoftExtensionsExample).
