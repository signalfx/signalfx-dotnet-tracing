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

## Settings

Use these environment variables to configure the tracing library:

| Setting | Description | Default |
|-|-|-|
| `SIGNALFX_ACCESS_TOKEN` | The access token for your SignalFx organization. It enables sending traces directly to the SignalFx ingestion endpoint. To do so, the `SIGNALFX_ENDPOINT_URL` must be set with: `https://ingest.<REALM>.signalfx.com/v2/trace`. |  |
| `SIGNALFX_TRACE_CONFIG_FILE` | The file path of a JSON configuration file that will be loaded. |  |
| `SIGNALFX_VERSION` | The application's version that will populate `version` tag on spans. |  |
| `SIGNALFX_TRACE_HEADER_TAGS` | Comma-separated map of header keys to tag name, that will be automatically applied as tags on traces. | `"key1:val1,key2:val2"` |
| `SIGNALFX_MAX_TRACES_PER_SECOND` | The number of traces allowed to be submitted per second. | `100` |
| `SIGNALFX_TRACE_RESPONSE_HEADER_ENABLED` | Enable to add server trace information to HTTP response headers. | `true` |
| `SIGNALFX_TRACE_STARTUP_LOGS` | Enable to activate diagnostic log at startup. | `true` |
| `SIGNALFX_TRACE_SAMPLING_RULES` | Comma separated list of sampling rules that enable custom sampling rules based on regular expressions. The rule is matched in order of specification. The first match in a list is used. The item "sample_rate" is required in decimal format. The item "service" is optional in regular expression format, to match on service name. The item "name" is optional in regular expression format, to match on operation name. | `'[{"sample_rate":0.5, "service":"cart.*"}],[{"sample_rate":0.2, "name":"http.request"}]'` |
| `SIGNALFX_TRACE_SAMPLE_RATE` | The global rate for the sampler. By default, all traces are sampled. |  |
| `SIGNALFX_TRACE_LOGGING_RATE` | The number of seconds between identical log messages for tracer log files. Setting to 0 disables rate limiting. | `60` |
| `SIGNALFX_TRACE_LOG_DIRECTORY` | The directory of the .NET Tracer logs. Overrides the value in `SIGNALFX_TRACE_LOG_PATH` if present. | Linux: `/var/log/signalfx/dotnet/`<br>Windows: `%ProgramData%\SignalFx .NET Tracing\logs\` |
| `SIGNALFX_HTTP_SERVER_ERROR_STATUSES` | The application's server http statuses to set spans as errors by. | `500-599` |
| `SIGNALFX_HTTP_CLIENT_ERROR_STATUSES` | The application's client http statuses to set spans as errors by. | `400-599` |
| `SIGNALFX_TRACE_HTTP_CLIENT_EXCLUDED_URL_SUBSTRINGS` | Sets URLs that are skipped by the tracer. |  |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED` | Enable to activate sending partial traces to the agent. | `false` |
| `SIGNALFX_TRACE_PARTIAL_FLUSH_MIN_SPANS` | The minimum number of closed spans in a trace before it's partially flushed. `SIGNALFX_TRACE_PARTIAL_FLUSH_ENABLED` has to be enabled for this to take effect. | `500` |
| `SIGNALFX_SERVICE_NAME` | Application's default service name. |  |
| `SIGNALFX_ENV` | The value for the `deployment.environment` tag added to every span. |  |
| `SIGNALFX_TRACE_ENABLED` | Enable to activate the tracer. | `true` |
| `SIGNALFX_TRACE_DEBUG` | Enable to activate debugging mode for the tracer. | `false` |
| `SIGNALFX_ENDPOINT_URL` | The URL to where trace exporters send traces. | `http://localhost:9411/api/v2/spans` |
| `SIGNALFX_TAGS` | Comma-separated list of key-value pairs to specify global span tags. For example: `"key1:val1,key2:val2"` |  |
| `SIGNALFX_LOGS_INJECTION` | Enable to inject trace IDs, span IDs, service name and environment into logs. This requires a compatible logger or manual configuration. | `false` |
| `SIGNALFX_STDOUT_LOG_ENABLED` | Enables `stdout` logging. This is disabled by default. | `false` |
| `SIGNALFX_STDOUT_LOG_TEMPLATE` | Configures `stdout` log template. See more about Serilog formatting options [here](https://github.com/serilog/serilog/wiki/Configuration-Basics#output-templates). | `"[{Level:u3}] {Message:lj} {NewLine}{Exception}{NewLine}"` |
| `SIGNALFX_FILE_LOG_ENABLED` | Enable file logging. This is enabled by default. | `true` |
| `SIGNALFX_MAX_LOGFILE_SIZE` | The maximum size for tracer log files, in bytes. | `10 MB` |
| `SIGNALFX_TRACE_LOG_PATH` | (Deprecated) The path of the profiler log file. | Linux: `/var/log/signalfx/dotnet/dotnet-profiler.log`<br>Windows: `%ProgramData%\SignalFx .NET Tracing\logs\dotnet-profiler.log` |
| `SIGNALFX_DIAGNOSTIC_SOURCE_ENABLED` | Enable to generate troubleshooting logs with the `System.Diagnostics.DiagnosticSource` class. | `true` |
| `SIGNALFX_DISABLED_INTEGRATIONS` | Comma-separated list of disabled library instrumentations. The available integration ID values can be found [here](instrumented-libraries.md). |  |
| `SIGNALFX_PROPAGATORS` | Comma separated list of the propagators for the tracer. Available propagators are: `Datadog`, `B3`, `W3C`. The Tracer will try to execute extraction in the given order. | `B3` |
| `SIGNALFX_PROFILER_PROCESSES` | Sets the filename of executables the profiler can attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with comma, for example: `MyApp.exe,dotnet.exe` |  |
| `SIGNALFX_PROFILER_EXCLUDE_PROCESSES` | Sets the filename of executables the profiler cannot attach to. If not defined (default), the profiler will attach to any process. Supports multiple values separated with comma, for example: `MyApp.exe,dotnet.exe` |  |
| `SIGNALFX_RECORDED_VALUE_MAX_LENGTH` | The maximum length an attribute value can have. Values longer than this are truncated. Values are completely truncated when set to 0, and ignored when set to a negative value. | `12000` |
| `SIGNALFX_INSTRUMENTATION_MONGODB_TAG_COMMANDS` | Enable the tagging of a Mongo command BsonDocument as `db.statement`. | `true` |
| `SIGNALFX_INSTRUMENTATION_REDIS_TAG_COMMANDS` | Enable the tagging of a Redis command as `db.statement`. | `true` |
| `SIGNALFX_TRACE_ROUTE_TEMPLATE_RESOURCE_NAMES_ENABLED` | ASP.NET span and resource names are based on routing configuration if applicable. | `true` |
| `SIGNALFX_INSTRUMENTATION_ELASTICSEARCH_TAG_QUERIES` | Enable the tagging of a Elasticsearch command PostData as `db.statement`. It may introduce overhead for direct streaming users. | `true` |
| `SIGNALFX_TRACE_DELAY_WCF_INSTRUMENTATION_ENABLED` | Enable the updated WCF instrumentation that delays execution until later in the WCF pipeline when the WCF server exception handling is established. | `false` |
| `SIGNALFX_TRACE_KAFKA_CREATE_CONSUMER_SCOPE_ENABLED` | Enable to close consumer scope on method enter, and start a new one on method exit. | `true` |
| `SIGNALFX_TRACE_{0}_ENABLED` | Configuration pattern for enabling/disabling a specific library instrumentation, e.g in order to disable Kafka instrumentation, set `SIGNALFX_TRACE_Kafka_ENABLED=false` | `true` |
| `SIGNALFX_AZURE_APP_SERVICES` | Set to indicate the profiler is running in the context of Azure App Services. | `false` |
