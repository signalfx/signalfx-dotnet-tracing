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
| `SIGNALFX_SERVICE_NAME` | The name of the application or service. |  |
| `SIGNALFX_VERSION` | The version of the application. When set, it populates the `version` tag on spans. |  |

## Global management settings

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_AZURE_APP_SERVICES` | Set to indicate that the profiler is running in the context of Azure App Services. | `false` |
| `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` | Sets the filename of executables the profiler cannot attach to. Supports multiple comma-separated values, for example: `MyApp.exe,dotnet.exe` |  |
| `SIGNALFX_PROFILER_PROCESSES` | Sets the filename of executables the profiler can attach to. Supports multiple comma-separated values, for example: `MyApp.exe,dotnet.exe` |  |
| `SIGNALFX_TRACE_CONFIG_FILE` | Path of the JSON configuration file. |  |
| `SIGNALFX_TRACE_ENABLED` | Enable to activate the tracer. | `true` |

## Global instrumentation settings

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_DISABLED_INTEGRATIONS` | Comma-separated list of disabled library instrumentations. The available integration ID values can be found [here](instrumented-libraries.md). |  |
| `SIGNALFX_RECORDED_VALUE_MAX_LENGTH` | The maximum length an attribute value can have. Values longer than this are truncated. Values are discarded entirely when set to 0, and ignored when set to a negative value. | `12000` |
| `SIGNALFX_TAGS` | Comma-separated list of key-value pairs that specify global span tags. For example: `"key1:val1,key2:val2"` |  |
| `SIGNALFX_TRACE_{0}_ENABLED` | Configuration pattern for enabling or disabling a specific [library instrumentation](instrumented-libraries.md). For example, in order to disable Kafka instrumentation, set `SIGNALFX_TRACE_Kafka_ENABLED=false` | `true` |

## Exporter settings

Use following settings to configure where the telemetry data is being exported.

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_ACCESS_TOKEN` | Your Splunk Observabilty Cloud access token for your organization. It enables sending traces directly to the Splunk Observabilty Cloud ingest endpoint. |  |
| `SIGNALFX_ENDPOINT_URL` | The URL to where trace exporters send traces. | `http://localhost:9411/api/v2/spans` |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED` | Enable to activate sending partial traces to the agent. | `false` |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS` | The minimum number of closed spans in a trace before it's partially flushed. `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED` has to be enabled for this to take effect. | `500` |

### Exporting directly to Splunk Observabilty Cloud

In order to export telemetry directly to Splunk Observability Cloud,
configure the following settings:

| Setting | Value | Notes |
|-|-|-|
| `SIGNALFX_ACCESS_TOKEN` | *organization access tokens* | See [here](https://docs.splunk.com/Observability/admin/authentication-tokens/org-tokens.html) to learn how to obtain one. |
| `SIGNALFX_ENDPOINT_URL` | `https://ingest.<REALM>.signalfx.com/v2/trace` | Replace `<REALM>` with your realm name. To find your realm, open Splunk Observability Cloud, click Settings, and click on your user name. |

## Trace propagation settings

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_PROPAGATORS` | Comma-separated list of the propagators for the tracer. Available propagators are: `Datadog`, `B3`, `W3C`. The Tracer will try to execute extraction in the given order. | `B3` |

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
| `SIGNALFX_TRACE_HTTP_CLIENT_EXCLUDED_URL_SUBSTRINGS` | Sets URLs that are skipped by the tracer. |  |
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
