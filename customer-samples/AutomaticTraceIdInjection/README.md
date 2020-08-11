# Inject traces in logs

To inject traces in logs, enable log correlation by setting the environment variable
``SIGNALFX_LOGS_INJECTION=true`` before running your instrumented application.

For more information about instrumenting a .NET application, see
[Configure the SignalFx Tracing Library for .NET](/README.md#Configure-the-SignalFx-Tracing-Library-for-.NET).

If your logger uses JSON, the tracing library automatically handles trace ID
injection.

If your logger uses a raw format, manually configure your logger to include
the trace ID and span ID. For examples that show you how to do this for each
supported logger, see these configurations:
- [Log4Net](/customer-samples/AutomaticTraceIdInjection/Log4NetExample/log4net.config)
- [NLog45](/customer-samples/AutomaticTraceIdInjection/Log4NetExample/log4net.config)
- [NLog46](/customer-samples/AutomaticTraceIdInjection/NLog46Example/NLog.config)
- [Serilog](/customer-samples/AutomaticTraceIdInjection/SerilogExample/Program.cs)

## Supported Logging Frameworks

These are the supported logging frameworks.

### Log4Net

Version 2.0.8

Layouts configured in the sample:
- JSON format: `SerializedLayout` (from the `log4net.Ext.Json` NuGet package)
- Raw format: `PatternLayout` (requires manual configuration)

### NLog

Versions 4.5.11 and 4.6.7

Layouts configured in the sample:
- JSON format: `JsonLayout`
- Raw format: Custom layout (requires manual configuration)

### Serilog

Versions 2.5.0 and 2.9.0

Layouts configured in the sample:
- JSON format: `JsonFormatter`
- JSON format: `CompactJsonFormatter` (from the `Serilog.Formatting.Compact` NuGet package)
- Raw format: output template (requires manual configuration)
