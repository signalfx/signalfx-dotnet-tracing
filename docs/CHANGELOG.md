<!-- markdownlint-disable-file MD024 -->

# Changelog

All notable changes to this repository are documented in this file.

The format is based on the [Splunk GDI specification](https://github.com/signalfx/gdi-specification/blob/v1.0.0/specification/repository.md),
and this repository adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### General

### Breaking changes

### Bugfixes

### Enhancements

## [Release 1.1.0](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v1.1.0)

### General

- Add `profiling.data.total.frame.count` attribute when exporting profiling data.

## [Release 1.0.1](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v1.0.1)

### General

- Deprecate `process.cpu.utilization` metric.

## [Release 1.0.0](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v1.0.0)

### Breaking changes

- Replace `SIGNALFX_RUNTIME_METRICS_ENABLED` setting with `SIGNALFX_METRICS_{0}_ENABLED`,
that allows to enable specific metrics groups (See [advanced-config](advanced-config.md)).
- Rename `SIGNALFX_TRACE_METRICS_ENABLED` to `SIGNALFX_METRICS_Traces_ENABLED` to
preserve consistent naming convention.
- Rename `SIGNALFX_TRACE_GLOBAL_TAGS` to `SIGNALFX_GLOBAL_TAGS`.

### Bugfixes

- Fix a deadlock in AlwaysOn Profiling.

### Enhancements

- Add support for .NET 7.0.
- Add `SIGNALFX_TRACE_BUFFER_SIZE` setting to configure trace exporter buffer size.
- Add `SIGNALFX_METRICS_{0}_ENABLED` to enable specific metrics group.
- Change metric structure of the runtime metrics exported to align with the OpenTelemetry
implementation from the [Runtime](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/blob/bc947a00c3f859cc436f050e81172fc1f8bc09d7/src/OpenTelemetry.Instrumentation.Runtime) package.
- Change metric structure of the process metrics exported to align with the OpenTelemetry
implementation from the [Process](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/926386e68c9066e8032853e8309abdf4088d8dca/src/OpenTelemetry.Instrumentation.Process) package.

## [Release 0.2.9](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.9)

### Bugfixes

- Fix sampled value in `ServerTiming` header.

### Enhancements

Adds support to include ASP.NET Core query string in the tag `http.url`.
By default **enabled**. See configuration variables:

- `SIGNALFX_HTTP_SERVER_TAG_QUERY_STRING` - Enable or disable query string inclusion.
- `SIGNALFX_TRACE_OBFUSCATION_QUERY_STRING_REGEXP` - Query string obfuscation regex.
- `SIGNALFX_TRACE_OBFUSCATION_QUERY_STRING_REGEXP_TIMEOUT` - Obfuscation regex
runtime timeout.

## [Release 0.2.8](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.8)

### Bugfixes

- [ASP.NET Web API 2 Instrumentation] Fix the case when the spans was
  incorrectly marked as errors when servers return 404 status code.

---

## [Release 0.2.7](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.7)

### Bugfixes

- Fix span context propagation for [AlwaysOn Profiling](always-on-profiling.md)

---

## [Release 0.2.6](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.6)

### Bugfixes

- Fix context propagation on .NET Framework.

---

## [Release 0.2.5](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.5)

### General

- Add `pprof` format support for exporting profiling data.

---

## [Release 0.2.4](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.4)

### General

- Add `W3C` as default propagator.

---

## [Release 0.2.3](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.3)

### General

- The release adds experimental support for [AlwaysOn Profiling](always-on-profiling.md)

### Enhancements

- Add `SIGNALFX_PROFILER_LOGS_ENDPOINT` setting to configure the URL to where
logs are exported using [OTLP/HTTP v1 log protocol](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/otlp.md).
- Add `SIGNALFX_PROFILER_ENABLED` setting that enables AlwaysOn Profiling.
- Add `SIGNALFX_PROFILER_CALL_STACK_INTERVAL` setting that overrides default
AlwaysOn Profiling sampling period.

---

## [Release 0.2.2](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.2)

### General

- The release adds experimental support for automatic runtime and tracing metrics.

### Enhancements

- Add `SIGNALFX_RUNTIME_METRICS_ENABLED` setting that enables runtime metrics.
- Add `SIGNALFX_REALM` setting to let the instrumentation bypass
the OpenTelemetry Collector and send telemetry directly to Splunk
Observability Cloud ingest endpoints.
- Add `SIGNALFX_METRICS_ENDPOINT_URL` setting that overrides
the metrics exporter's endpoint.
- Add `SIGNALFX_TRACE_METRICS_ENABLED` setting that enables trace metrics.

---

## [Release 0.2.1](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.1)

### General

- Brings back support for Alpine Linux.

### Bugfixes

- Fixes OpenTracing Scope creation when manual tracing is used.

---

## [Release 0.2.0](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.2.0)

### General

- The release contains significant changes as it is based on the latest
  [.NET Tracer for Datadog APM](https://github.com/DataDog/dd-trace-dotnet)
  with modifications to make it work with the
  [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector)
  and directly with [Splunk Observability Cloud](https://www.splunk.com/en_us/observability.html).
  Some of the changes are breaking. Please contact us if you miss any feature
  from the previous release.

### Breaking changes

- There is no support for .NET Framework older than .NET Framework 4.6.1.
- There is no support for Alpine Linux.
- There are no new releases for [`SignalFx.Tracing`](https://www.nuget.org/packages/SignalFx.Tracing.OpenTracing/)
nor [`SignalFx.Tracing.OpenTracing`](https://www.nuget.org/packages/SignalFx.Tracing.OpenTracing/)
NuGet packages. For manual instrumentation use [`OpenTracing`](https://www.nuget.org/packages/OpenTracing/)
instead. You can find more information in the documentation.
- Rename `SIGNALFX_DOTNET_TRACER_CONFIG_FILE` configuration to `SIGNALFX_TRACE_CONFIG_FILE`.
- Rename `SIGNALFX_PROPAGATOR` configuration to `SIGNALFX_PROPAGATORS`.
- Rename `SIGNALFX_TRACING_ENABLED` configuration to `SIGNALFX_TRACE_ENABLED`
- Rename `SIGNALFX_ASPNET_TEMPLATE_NAMES_ENABLED` configuration to `SIGNALFX_TRACE_ROUTE_TEMPLATE_RESOURCE_NAMES_ENABLED`.
- Remove `SIGNALFX_ADD_CLIENT_IP_TO_SERVER_SPANS` configuration.
  New, fixed behavior is equivalent to flag enabled.
- Remove `SIGNALFX_SYNC_SEND` configuration.
- Remove `SIGNALFX_TRACE_DOMAIN_NEUTRAL_INSTRUMENTATION` configuration.
- Remove `SIGNALFX_APPEND_URL_PATH_TO_NAME` configuration as it was against the
  [OpenTelemetry Semantic conventions for HTTP spans](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/http.md#name).
  Take notice that the URL is available via `http.url` tag.
- Remove `SIGNALFX_USE_WEBSERVER_RESOURCE_AS_OPERATION_NAME` configuration.
  New, fixed behavior is equivalent to flag enabled,
  in order to better align with  [OpenTelemetry Semantic conventions for HTTP spans](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/http.md#name).
- Remove `SIGNALFX_SANITIZE_SQL_STATEMENTS` configuration as all field
  sanitizations are moving to the [Splunk Distribution of OpenTelemetry Collector](https://docs.splunk.com/Observability/gdi/opentelemetry/opentelemetry.html).
- Remove `SIGNALFX_OUTBOUND_HTTP_EXCLUDED_HOSTS` configuration as
  [Splunk OpenTelemetry Collector](https://docs.splunk.com/Observability/gdi/opentelemetry/opentelemetry.html)
  is the recommended place for spans filtering.
  If you need span exclusion for specific URL substrings, it can be configured
  using `SIGNALFX_TRACE_HTTP_CLIENT_EXCLUDED_URL_SUBSTRINGS` environment variable.
- Remove `SIGNALFX_INSTRUMENTATION_ASPNETCORE_DIAGNOSTIC_LISTENERS` configuration
  which is no longer needed. It provided a workaround for an issue in a specific
  version of a library, which broke default instrumentation, and was already fixed.
- Remove `SIGNALFX_SERVICE_NAME_PER_SPAN_ENABLED` configuration as
  [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md)
  requires that the resources (such us as [service](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions#service))
  have to be immutable.
- Remove `SIGNALFX_INTEGRATIONS`. This configuration is not needed.
  The instrumentation called `CallSite` was removed.
- Deprecate `SIGNALFX_TRACE_LOG_PATH`. Please use `SIGNALFX_TRACE_LOG_DIRECTORY`.

### Enhancements

- Add [Aerospike.Client](https://www.nuget.org/packages/Aerospike.Client/)
  library instrumentation.
- Add [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/)
  and [`System.Data.SqlClient`](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient)
  library instrumentation.
- Adopt [OpenTelemetry Trace Semantic Conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/trace/semantic_conventions)
  in most of the instrumentations.
- Add `SIGNALFX_HTTP_SERVER_ERROR_STATUSES` configuration that controls server
  HTTP statuses to set spans as errors.
- Add `SIGNALFX_HTTP_CLIENT_ERROR_STATUSES` configuration that controls client
  HTTP statuses to set spans as errors.
- Add `SIGNALFX_STDOUT_LOG_TEMPLATE` configuration that configures `stdout` template.
- Add `SIGNALFX_TRACE_DELAY_WCF_INSTRUMENTATION_ENABLED` configuration that
  enables the updated WCF instrumentation that delays execution until later in
  the WCF pipeline when the WCF server exception handling is established.
- Add `SIGNALFX_TRACE_HEADER_TAGS` configuration that sets a map of header keys
  to tag names.
- Add `SIGNALFX_TRACE_KAFKA_CREATE_CONSUMER_SCOPE_ENABLED` configuration that
  closes consumer scope on method enter, and starts a new one on method exit.
- Add `SIGNALFX_TRACE_LOG_DIRECTORY` configuration that sets directory for logs
  and overrides the value in `SIGNALFX_TRACE_LOG_PATH` if present.
- Add `SIGNALFX_TRACE_LOGGING_RATE` configuration that sets the number of seconds
  between identical log messages for tracer log files.
- Add `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED` configuration that enables partial
  flush of traces.
- Add `SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS` configuration that sets the minimum
  number of closed spans in a trace before it's partially flushed.
- Add `SIGNALFX_VERSION` configuration that sets the application's version that
  will populate the `version` tag on spans.
- Add `SIGNALFX_TRACE_STARTUP_LOGS` configuration that enables diagnostic log
  at startup.
- Add `SIGNALFX_TRACE_{0}_ENABLED` configuration pattern that enables/disables
  specific integration.
- Add `SIGNALFX_TRACE_HTTP_CLIENT_EXCLUDED_URL_SUBSTRINGS` configuration that
  sets URLs skipped by the tracer.
- Add `SIGNALFX_AZURE_APP_SERVICES` configuration that indicates the profiler
  is running in the context of Azure App Services.
- `SIGNALFX_ENDPOINT_URL` now defaults to `http://localhost:9411/api/v2/spans`.

---

## [Release 0.1.16](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.16)

### Bugfixes

- Fix NLog integration when using ILogger.

---

## [Release 0.1.15](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.15)

### Bugfixes

- Remove informational log from hot path (ASP.NET Core performance bug).

### Enhancements

- Add option to disable CancelKeyPress event subscription (`SIGNALFX_DISABLE_CONSOLE_CTRL_HANDLE`).

---

## [Release 0.1.14](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.14)

### Bugfixes

- Do not add Sever-Timing header on IIS apps using classic pool
  (fixes crash for this case).
- Fix RabbitMq delivery mode tags.

### Enhancements

- Remove "PreRelease" from AZ site extension.

---

## [Release 0.1.13](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.13)

### Enhancements

- Use OpenTelemetry semantic conventions for log correlation.
- Remove spans for Confluent.Kafka Consume calls that didn't receive a message.
- Add configuration setting, `SIGNALFX_OUTBOUND_HTTP_EXCLUDED_HOSTS`,
  that prevents the creation of outbound HTTP spans for certain hosts.
- Added Server-Timing header to ASP.NET on IIS.
- Added RabbitMQ instrumentation.

---

## [Release 0.1.12](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.12)

### Enhancements

- Support for .NET 5.0.
- Added Confluent.Kafka instrumentation for IConsumer.Consume, IProducer.Produce,
  and IProducer.ProduceAsync.
- New instrumentations for SqlCommand: ExecuteXmlReader and ExecuteXmlReaderAsync
  methods.

---

## [Release 0.1.11](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.11)

### Enhancements

- Optimized log injection for NLog 4.6+.
- Optimized B3 context propagation.
- Other optimizations: default sampler and span Id allocation.

---

## Previous Releases

See [releases page](https://github.com/signalfx/signalfx-dotnet-tracing/releases).
