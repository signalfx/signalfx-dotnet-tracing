# Advanced configuration

## Configuration methods

You can apply configuration settings in the following ways
, with environment variables taking precedence over XML and JSON:

1. Environment variables

    Environment variables are the main way to configure the settings.

2. `web.config` or `app.config` file

    For an application running on .NET Framework, you can use a web configuration
    file  (`web.config`) or an application configuration file (`app.config`) to
    configure the settings.

    Example with `SIGNALFX_SERVICE_NAME` setting:

    ```xml
    <configuration>
    <appSettings>
        <add key="SIGNALFX_SERVICE_NAME" value="my-service-name" />
    </appSettings>
    </configuration>
    ```

3. JSON configuration file

    To use it, set the JSON configuration file path as the value for
    `SIGNALFX_TRACE_CONFIG_FILE` using one of the previous methods.

    Example with `SIGNALFX_SERVICE_NAME` setting:

    ```json
    {
        "SIGNALFX_SERVICE_NAME": "my-service-name"
    }
    ```

## Main settings

The following settings are common to most instrumentation scenarios:

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_ENV` | The value for the `deployment.environment` tag added to every span. |  |
| `SIGNALFX_SERVICE_NAME` | The name of the application or service. | See [here](default-service-name.md)  |
| `SIGNALFX_VERSION` | The version of the application. When set, it populates the `version` tag on spans. |  |

## Global management settings

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_AZURE_APP_SERVICES` | Set to indicate that the profiler is running in the context of Azure App Services. | `false` |
| `SIGNALFX_DOTNET_TRACER_HOME` | Installation location. Must be set manually to `/opt/signalfx` when instrumenting applications on Linux or background services in Azure App Service. |  _Automatically set ONLY by the Windows installer_ |
| `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` | Sets the filename of executables the profiler cannot attach to. Supports multiple semicolon-separated values, for example: `MyApp.exe;dotnet.exe` |  |
| `SIGNALFX_PROFILER_PROCESSES` | Sets the filename of executables the profiler can attach to. Supports multiple semicolon-separated values, for example: `MyApp.exe;dotnet.exe` |  |
| `SIGNALFX_TRACE_CONFIG_FILE` | Path of the JSON configuration file. |  |
| `SIGNALFX_TRACE_ENABLED` | Enable to activate the tracer. | `true` |
| `SIGNALFX_RUNTIME_METRICS_ENABLED` | Enable to activate the runtime metrics. | `false` |
| `SIGNALFX_TRACE_METRICS_ENABLED` | Enable to activate additional trace metrics. | `false` |

## Global instrumentation settings

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_DISABLED_INTEGRATIONS` | Comma-separated list of disabled library instrumentations. The available integration ID values can be found [here](instrumented-libraries.md). |  |
| `SIGNALFX_RECORDED_VALUE_MAX_LENGTH` | The maximum length an attribute value can have. Values longer than this are truncated. Values are discarded entirely when set to 0, and ignored when set to a negative value. | `12000` |
| `SIGNALFX_TRACE_GLOBAL_TAGS` | Comma-separated list of key-value pairs that specify global span tags. For example: `"key1:val1,key2:val2"` |  |
| `SIGNALFX_TRACE_{0}_ENABLED` | Configuration pattern for enabling or disabling a specific [library instrumentation](instrumented-libraries.md). For example, in order to disable Kafka instrumentation, set `SIGNALFX_TRACE_Kafka_ENABLED=false` | `true` |

## Exporter settings

Use following settings to configure where and how the telemetry data is being exported.

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_ACCESS_TOKEN` | Splunk Observability Cloud access token for your organization. It enables sending telemetry directly to the Splunk Observability Cloud ingest endpoint. | |
| `SIGNALFX_REALM` | Your Splunk Observability Cloud realm. To find your realm, open Splunk Observability Cloud, click Settings, then click on your username. | `none` (local collector) |
| `SIGNALFX_ENDPOINT_URL` | The URL to where trace exporters send traces. Overrides `SIGNALFX_REALM` configuration for the traces ingestion endpoint. | `http://localhost:9411/api/v2/spans` |
| `SIGNALFX_METRICS_ENDPOINT_URL` | The URL to where metric exporters send metrics. Overrides `SIGNALFX_REALM` configuration for the metrics ingestion endpoint. | `http://localhost:9943/v2/datapoint` |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED` | Enable to export traces that contain a minimum number of closed spans, as defined by `SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS`. | `false` |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS` | Minimum number of closed spans in a trace before it's exported. The default value is ``500``. Requires the value of the ``SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED`` environment variable to be ``true``. | `500` |
| `SIGNALFX_TRACE_BUFFER_SIZE` | The size of the trace exporter buffer, expressed as the number of traces. | `1000` |

### Exporting directly to Splunk Observability Cloud

By default, all telemetry is
sent to the local instance of [Splunk OpenTelemetry Collector](https://github.com/signalfx/splunk-otel-collector).

In order to export telemetry directly to Splunk Observability Cloud,
configure the following settings:

| Setting | Value | Notes |
|-|-|-|
| `SIGNALFX_ACCESS_TOKEN` | _organization access tokens_ | See [here](https://docs.splunk.com/Observability/admin/authentication-tokens/org-tokens.html) to learn how to obtain one. |
| `SIGNALFX_REALM` | Your Observability Cloud realm | Set to send telemetry directly to Splunk Observability Cloud. If configured, metrics are sent to `https://ingest.<SIGNALFX_REALM>.signalfx.com/v2/datapoint` and traces are sent to `https://ingest.<SIGNALFX_REALM>.signalfx.com/v2/trace`. |
| `SIGNALFX_ENDPOINT_URL` | Trace ingestion endpoint | Overrides `SIGNALFX_REALM` configuration for the traces ingestion endpoint. |
| `SIGNALFX_METRICS_ENDPOINT_URL` | Metric ingestion endpoint | Overrides `SIGNALFX_REALM` configuration for the traces ingestion endpoint. |

__IMPORTANT:__ To export data directly to Splunk Observability Cloud,
set either the `SIGNALFX_REALM` environment variable
or the `SIGNALFX_ENDPOINT_URL` and `SIGNALFX_METRICS_ENDPOINT_URL` environment variables,
in addition to `SIGNALFX_ACCESS_TOKEN`.

## Trace propagation settings

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_PROPAGATORS` | Comma-separated list of the propagators for the tracer. Available propagators are: `B3`, `W3C`. The Tracer will try to execute extraction in the given order. | `B3,W3C` |

[OpenTelemetry `OTEL_PROPAGATORS`](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/sdk-environment-variables.md#general-sdk-configuration)
to `SIGNALFX_PROPAGATORS` values mapping:

| `OTEL_PROPAGATORS` value | `SIGNALFX_PROPAGATORS` value |
|-|-|
| `b3multi` | `B3` |
| `tracecontext` | `W3C` |

## Library-specific instrumentation settings

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_HTTP_CLIENT_ERROR_STATUSES` | Comma-separated list of HTTP client response statuses for which the spans are set as errors, for example: `300, 400-499`.  | `400-599` |
| `SIGNALFX_HTTP_SERVER_ERROR_STATUSES` | Comma-separated list of HTTP server response statuses for which the spans are set as errors, for example: `300, 400-599`. | `500-599` |
| `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES` | Enable the tagging of an Elasticsearch command PostData as `db.statement`. It might introduce overhead for direct streaming users. | `true` |
| `SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS` | Enable the tagging of a Mongo command BsonDocument as `db.statement`. | `true` |
| `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS` | Enable the tagging of a Redis command as `db.statement`. | `true` |
| `SIGNALFX_LOGS_INJECTION` | Enable to inject trace IDs, span IDs, service name, and environment into logs. See more details [here](correlating-traces-with-logs.md). | `false` |
| `SIGNALFX_TRACE_DELAY_WCF_INSTRUMENTATION_ENABLED` | Enable the updated WCF instrumentation that delays execution until later in the WCF pipeline when the WCF server exception handling is established. | `false` |
| `SIGNALFX_TRACE_HEADER_TAGS` | Comma-separated map of HTTP header keys to tag names, automatically applied as tags on traces. | `"x-my-header:my-tag,header2:tag2"` |
| `SIGNALFX_TRACE_HTTP_CLIENT_EXCLUDED_URL_SUBSTRINGS` | Comma-separated list of URL substrings. Matching URLs are ignored by the tracer. For example, `subdomain,xyz,login,download`. |  |
| `SIGNALFX_TRACE_KAFKA_CREATE_CONSUMER_SCOPE_ENABLED` | Enable to close consumer scope on method enter, and start a new one on method exit. | `true` |
| `SIGNALFX_TRACE_RESPONSE_HEADER_ENABLED` | Enable to add server trace information to HTTP response headers. It enables [Splunk Real User Monitoring (RUM)](https://docs.splunk.com/Observability/rum/intro-to-rum.html) integration when using ASP.NET and ASP.NET Core. | `true` |
| `SIGNALFX_TRACE_ROUTE_TEMPLATE_RESOURCE_NAMES_ENABLED` | ASP.NET span and resource names are based on routing configuration if applicable. | `true` |

## Logging settings

The settings below can be used to configure the internal logging
of SignalFx Instrumentation for .NET.

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_DIAGNOSTIC_SOURCE_ENABLED` | Enable to generate troubleshooting logs with the `System.Diagnostics.DiagnosticSource` class. | `true` |
| `SIGNALFX_FILE_LOG_ENABLED` | Enable file logging. | `true` |
| `SIGNALFX_MAX_LOGFILE_SIZE` | The maximum size for tracer log files, in bytes. | `245760` (10 MB) |
| `SIGNALFX_STDOUT_LOG_ENABLED` | Enables `stdout` logging. | `false` |
| `SIGNALFX_STDOUT_LOG_TEMPLATE` | Configures `stdout` log template. See more about Serilog formatting options [here](https://github.com/serilog/serilog/wiki/Configuration-Basics#output-templates). | `"[{Level:u3}] {Message:lj} {NewLine}{Exception}{NewLine}"` |
| `SIGNALFX_TRACE_DEBUG` | Enable to activate debugging mode for the tracer. | `false` |
| `SIGNALFX_TRACE_LOG_DIRECTORY` | The directory of the .NET Tracer logs. Overrides the value in `SIGNALFX_TRACE_LOG_PATH` if present. | Linux: `/var/log/signalfx/dotnet/`<br>Windows: `%ProgramData%\SignalFx .NET Tracing\logs\` |
| `SIGNALFX_TRACE_LOGGING_RATE` | The number of seconds between identical log messages for tracer log files. Setting to 0 disables rate limiting. | `60` |
| `SIGNALFX_TRACE_STARTUP_LOGS` | Enable to activate diagnostic log at startup. | `true` |

## AlwaysOn Profiling settings

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_PROFILER_LOGS_ENDPOINT` | The URL to where logs are exported using [OTLP/HTTP v1 log protocol](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/otlp.md) | `http://localhost:4318/v1/logs` |
| `SIGNALFX_PROFILER_ENABLED` | Enable to activate thread sampling. | `false` |
| `SIGNALFX_PROFILER_CALL_STACK_INTERVAL` | Sampling period in milliseconds. It defines how often the threads are stopped in order to fetch all stack traces. This value cannot be lower than `1000` milliseconds. | `10000` |

## Including query string settings

This feature is ASP.NET Core only.

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_TRACE_OBFUSCATION_QUERY_STRING_REGEXP` | Specifies a custom regex to obfuscate query strings. | (see default below)[^regex] |
| `SIGNALFX_TRACE_OBFUSCATION_QUERY_STRING_REGEXP_TIMEOUT` | Specifies a timeout in milliseconds to the execution of the query string obfuscation regex. | `200` |
| `SIGNALFX_HTTP_SERVER_TAG_QUERY_STRING` | Enables reporting query string | `true` |

[^regex]: Query string obfuscation default regex:

```txt
((?i)(?:p(?:ass)?w(?:or)?d|pass(?:_?phrase)?|secret|(?:api_?|private_?|public_?|access_?|secret_?)key(?:_?id)?|token|consumer_?(?:id|key|secret)|sign(?:ed|ature)?|auth(?:entication|orization)?)(?:(?:\s|%20)*(?:=|%3D)[^&]+|(?:""|%22)(?:\s|%20)*(?::|%3A)(?:\s|%20)*(?:""|%22)(?:%2[^2]|%[^2]|[^""%])+(?:""|%22))|bearer(?:\s|%20)+[a-z0-9\._\-]|token(?::|%3A)[a-z0-9]{13}|gh[opsu]_[0-9a-zA-Z]{36}|ey[I-L](?:[\w=-]|%3D)+\.ey[I-L](?:[\w=-]|%3D)+(?:\.(?:[\w.+\/=-]|%3D|%2F|%2B)+)?|[\-]{5}BEGIN(?:[a-z\s]|%20)+PRIVATE(?:\s|%20)KEY[\-]{5}[^\-]+[\-]{5}END(?:[a-z\s]|%20)+PRIVATE(?:\s|%20)KEY|ssh-rsa(?:\s|%20)*(?:[a-z0-9\/\.+]|%2F|%5C|%2B){100,})`
```
