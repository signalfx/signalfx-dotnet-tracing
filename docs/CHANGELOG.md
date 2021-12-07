# Changelog

All notable changes to this repository are documented in this file.

The format is based on the [Splunk GDI specification](https://github.com/signalfx/gdi-specification/blob/v1.0.0/specification/repository.md),
and this repository adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### General

- The release contains significant changes as it is based on the latest
  [.NET Tracer for Datadog APM](https://github.com/DataDog/dd-trace-dotnet)
  with modifications to make it working with the
  [Splunk OpenTelemetry Connector](https://github.com/signalfx/splunk-otel-collector)
  and directly with [Splunk Observabilty Cloud](https://www.splunk.com/en_us/observability.html).
  Some of the changes are breaking. Please contact us if you miss any feature
  from the previous release.

### Breaking changes

- Remove `SIGNALFX_APPEND_URL_PATH_TO_NAME` configuration as it was against the
  [OpenTelemetry Semantic conventions for HTTP spans](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/http.md#name).
  Take notice that the URL is available via `http.url` tag.
- Remove `SIGNALFX_USE_WEBSERVER_RESOURCE_AS_OPERATION_NAME` configuration. New, fixed behavior is equivalent to flag enabled,
  in order to better align with  [OpenTelemetry Semantic conventions for HTTP spans](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/http.md#name).
- Remove `SIGNALFX_SANITIZE_SQL_STATEMENTS` configuration as all field sanitizations are moving to the [Splunk Distribution of OpenTelemetry Collector](https://docs.splunk.com/Observability/gdi/opentelemetry/opentelemetry.html).
- Remove `SIGNALFX_OUTBOUND_HTTP_EXCLUDED_HOSTS` configuration as [Splunk Distribution of OpenTelemetry Collector](https://docs.splunk.com/Observability/gdi/opentelemetry/opentelemetry.html) is the recommended place for spans filtering.
  If you need span exclusion for specific url substrings, it can be configured using `SIGNALFX_TRACE_HTTP_CLIENT_EXCLUDED_URL_SUBSTRINGS` environment variable.
- Remove `SIGNALFX_INSTRUMENTATION_ASPNETCORE_DIAGNOSTIC_LISTENERS` configuration which is no longer needed. It provided a workaround for an issue in a specific version of a library, which broke default instrumentation, and was already fixed.  
- Remove `SIGNALFX_SERVICE_NAME_PER_SPAN_ENABLED` configuration as [the OpenTelemetry specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md)
  requires that the resources (such us as [service](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions#service)) have to be immutable.
- Remove `SIGNALFX_INTEGRATIONS`. This configuration is not needed. The insrtumenation called `CallSite` was removed.

### Enhancements

- Adopt [OpenTelemetry Trace Semantic Conventions](https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/trace/semantic_conventions)
  in most of the instrumentations.

---

## [Release 0.1.15](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.15)

### Bugfixes

- Remove informational log from hot path (ASP.NET Core performance bug).

### Enhancements

- Add option to disable CancelKeyPress event subscription (`SIGNALFX_DISABLE_CONSOLE_CTRL_HANDLE`).

---

## [Release 0.1.14](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.14)

### Bugfixes

- Do not add Sever-Timing header on IIS apps using classic pool (fixes crash for this case).
- Fix RabbitMq delivery mode tags.

### Enhancements

- Remove "PreRelease" from AZ site extension.

---

## [Release 0.1.13](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.13)

### Enhancements

- Use OpenTelemetry semantic conventions for log correlation.
- Remove spans for Confluent.Kafka Consume calls that didn't receive a message.
- Add configuration setting, `SIGNALFX_OUTBOUND_HTTP_EXCLUDED_HOSTS`, that prevents the creation of outbound HTTP spans for certain hosts.
- Added Server-Timing header to ASP.NET on IIS.
- Added RabbitMQ instrumentation.

---

## [Release 0.1.12](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.12)

### Enhancements

- Support for .NET 5.0.
- Added Confluent.Kafka instrumentation for IConsumer.Consume, IProducer.Produce, and IProducer.ProduceAsync.
- New instrumentations for SqlCommand: ExecuteXmlReader and ExecuteXmlReaderAsync methods.

---

## [Release 0.1.11](https://github.com/signalfx/signalfx-dotnet-tracing/releases/tag/v0.1.11)

### Enhancements

- Optimized log injection for NLog 4.6+.
- Optimized B3 context propagation.
- Other optimizations: default sampler and span Id allocation.

---

## Previous Releases

See [releases page](https://github.com/signalfx/signalfx-dotnet-tracing/releases).
