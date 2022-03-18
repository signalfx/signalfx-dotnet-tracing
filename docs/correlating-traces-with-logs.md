# Correlating traces with logs

If your application uses a supported logger,
you can configure log correlation to
include trace context in your application's logs.

To inject trace context fields in logs,
enable log correlation by setting the `SIGNALFX_LOGS_INJECTION`
environment variable to `true` before running your instrumented application.

If your logger uses JSON as the logging format,
the SignalFx Instrumentation for .NET automatically adds
span context to the logs.

If your logger uses a different format,
you might have to manually configure your
logger to include the trace context fields.

## Fields injected into log context

- `trace_id`
- `span_id`
- `service.name` -
  [`SIGNALFX_SERVICE_NAME`](advanced-config.md) setting
- `service.version` -
  [`SIGNALFX_VERSION`](advanced-config.md) setting
- `deployment.environment` -
  [`SIGNALFX_ENV`](advanced-config.md) setting

## Supported logging libraries

### Log4Net

- Versions: 1.0.0 ≤ 2.*.*

Supported layouts:

- JSON format: `SerializedLayout` (from the `log4net.Ext.Json` NuGet package)
- Raw format: `PatternLayout`

When using the `SerializedLayout` you can add all contextual properties
by adding the `properties` member as following:

```xml
<layout type='log4net.Layout.SerializedLayout, log4net.Ext.Json'>
  <!-- existing configuration -->
  <member value='properties'/> <!-- addition -->
</layout>
```

You can also add the context fields explicitly. For example:

```xml
<layout type='log4net.Layout.SerializedLayout, log4net.Ext.Json'>
  <!-- existing configuration -->
  <member value='trace_id' />
  <member value='span_id' />
  <member value='service.name' />
  <member value='service.version' />
  <member value='deployment.environment' />
</layout>
```

When using the `PatternLayout`, add the context fields manually. 
Values must be wrapped in quotation marks. For example:

```xml
<layout type="log4net.Layout.PatternLayout">
    <conversionPattern value="%date [%thread] %level %logger {trace_id=&quot;%property{trace_id}&quot;, span_id=&quot;%property{span_id}&quot;, service.name=&quot;%property{service.name}&quot;, service.version=&quot;%property{service.version}&quot;, deployment.environment=&quot;%property{deployment.environment}&quot;} - %message%newline" />
</layout>
```

Find samples here: [Log4NetExample](../tracer/samples/AutomaticTraceIdInjection/Log4NetExample).

### NLog

- Versions: 1.0.0.505 ≤ 4.*.*

Supported layouts:

- JSON format: `JsonLayout`
- Raw format: custom layout

When using `JsonLayout` and `NLog` version is at least 4.4.10,
you can add all contextual properties
by setting the attribute `includeMdlc` to `true`. For example:

```xml
<layout xsi:type="JsonLayout" includeMdlc="true"> <!-- includeMdlc property available in NLog 4.4.10+ -->
  <!-- existing configuration -->
</layout>
```

You can also add the context fields explicitly. For example:

```xml
<layout xsi:type="JsonLayout">
  <!-- existing configuration -->
  <attribute name="trace_id" layout="${mdc:item=trace_id}"/>
  <attribute name="span_id" layout="${mdc:item=span_id}"/>    
  <attribute name="service.name" layout="${mdc:item=service.name}"/>
  <attribute name="service.version" layout="${mdc:item=service.version}"/>
  <attribute name="deployment.environment" layout="${mdc:item=deployment.environment}"/>
</layout>
```

When using the custom layout, add the context fields manually.
Values must be wrapped in quotation marks. For example:

```xml
<target
  // existing configuration
  layout="${longdate}|${uppercase:${level}}|${logger}|{trace_id=&quot;${mdc:item=trace_id}&quot;,span_id=&quot;${mdc:item=span_id}&quot;,service.name=&quot;${mdc:item=service.name}&quot;,service.version=&quot;${mdc:item=service.version}&quot;,deployment.environment=&quot;${mdc:item=deployment.environment}&quot;}|${message}"
/>
```

Find samples here: [NLog46Example](../tracer/samples/AutomaticTraceIdInjection/NLog46Example).

### Serilog

- Versions: 1.4.0 ≤ 2.*.*

Regardless of the output layout, your `LoggerConfiguration` must be
enriched from the `LogContext` to extract the trace context
that is automatically injected. For example:

```csharp
var loggerConfiguration = new LoggerConfiguration()
    .Enrich.FromLogContext() // addition
```

`Serilog` does not support `.` in field names.
Therefore the field names use the `_` character instead.
The following fields are renamed accordingly:

- `service.name` => `service_name`
- `service.version` => `service_version`
- `deployment.environment` => `deployment_environment`

Supported layouts:

- JSON format: `JsonFormatter`
- JSON format: `CompactJsonFormatter` (from the `Serilog.Formatting.Compact`
  NuGet package)
- Raw format: output template (requires manual configuration)

When using the output template you can either use `{Properties}`
to print out all contextual properties or add context fields explicitly.

When adding context fields manually, values must be wrapped in
quotation marks.
For instance, you can use the following output template,
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

Supported layouts:

- JSON format: `json`
  (from the `NetEscapades.Extensions.Logging.RollingFile` NuGet package)

When using `NetEscapades.Extensions.Logging.RollingFile`
you have to enable the `IncludeScopes` option and use the `json` formatter. For example:

```csharp
Host.ConfigureLogging(builder => 
    builder.AddFile(opts =>
    {
        opts.FileName = "logs";
        opts.Extension = "json";
        opts.FormatterName = "json"; // supported formatter
        opts.IncludeScopes = true; // addition
    })
);
```

> Log correlation also works when `ILogger` is wrapping
any of the other supported loggers.

Find samples here: [MicrosoftExtensionsExample](../tracer/samples/AutomaticTraceIdInjection/MicrosoftExtensionsExample).
