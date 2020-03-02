// Modified by SignalFx
#ifndef DD_CLR_PROFILER_ENVIRONMENT_VARIABLES_H_
#define DD_CLR_PROFILER_ENVIRONMENT_VARIABLES_H_

#include "string.h"  // NOLINT

namespace trace {
namespace environment {

// Sets whether the profiler is enabled. Default is true.
// Setting this to false disabled the profiler entirely.
const WSTRING tracing_enabled = "SIGNALFX_TRACING_ENABLED"_W;

// Sets whether debug mode is enabled. Default is false.
const WSTRING debug_enabled = "SIGNALFX_TRACE_DEBUG"_W;

// Sets the paths to integration definition JSON files.
// Supports multiple values separated with semi-colons, for example:
// "C:\Program Files\Datadog .NET Tracer\integrations.json;D:\temp\test_integrations.json"
const WSTRING integrations_path = "SIGNALFX_INTEGRATIONS"_W;

// Sets the path to the profiler's home directory, for example:
// "C:\Program Files\Datadog .NET Tracer\" or "/opt/datadog/"
const WSTRING profiler_home_path = "SIGNALFX_DOTNET_TRACER_HOME"_W;

// Sets the filename of executables the profiler can attach to.
// If not defined (default), the profiler will attach to any process.
// Supports multiple values separated with semi-colons, for example:
// "MyApp.exe;dotnet.exe"
const WSTRING include_process_names = "SIGNALFX_PROFILER_PROCESSES"_W;

// Sets the filename of executables the profiler cannot attach to.
// If not defined (default), the profiler will attach to any process.
// Supports multiple values separated with semi-colons, for example:
// "MyApp.exe;dotnet.exe"
const WSTRING exclude_process_names = "SIGNALFX_PROFILER_EXCLUDE_PROCESSES"_W;

// Sets the Agent's host. Default is localhost.
const WSTRING agent_host = "SIGNALFX_AGENT_HOST"_W;

// Sets the Agent's port. Default is 9080.
const WSTRING agent_port = "SIGNALFX_TRACE_AGENT_PORT"_W;

// Sets the "env" tag for every span.
const WSTRING env = "SIGNALFX_ENV"_W;

// Sets the default service name for every span.
// If not set, Tracer will try to determine service name automatically
// from application name (e.g. entry assembly or IIS application name).
const WSTRING service_name = "SIGNALFX_SERVICE_NAME"_W;

// Sets a list of integrations to disable. All other integrations will remain
// enabled. If not set (default), all integrations are enabled. Supports
// multiple values separated with semi-colons, for example:
// "ElasticsearchNet;AspNetWebApi2"
const WSTRING disabled_integrations = "SIGNALFX_DISABLED_INTEGRATIONS"_W;

// Sets the path for the profiler's log file.
// If not set, default is
// "%ProgramData%"\Datadog .NET Tracer\logs\dotnet-profiler.log" on Windows or
// "/var/log/signalfx/dotnet-profiler.log" on Linux.
const WSTRING log_path = "SIGNALFX_TRACE_LOG_PATH"_W;

// Sets whether to disable all optimizations.
// Default is false on Windows.
// Default is true on Linux to work around a bug in the JIT compiler.
// https://github.com/dotnet/coreclr/issues/24676
// https://github.com/dotnet/coreclr/issues/12468
const WSTRING clr_disable_optimizations = "SIGNALFX_CLR_DISABLE_OPTIMIZATIONS"_W;

// Indicates whether the profiler is running in the context
// of Azure App Services
const WSTRING azure_app_services = "SIGNALFX_AZURE_APP_SERVICES"_W;

// The app_pool_id in the context of azure app services
const WSTRING azure_app_services_app_pool_id = "APP_POOL_ID"_W;

// The DOTNET_CLI_TELEMETRY_PROFILE in the context of azure app services
const WSTRING azure_app_services_cli_telemetry_profile_value =
    "DOTNET_CLI_TELEMETRY_PROFILE"_W;

}  // namespace environment
}  // namespace trace

#endif