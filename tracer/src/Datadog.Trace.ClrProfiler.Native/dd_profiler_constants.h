#ifndef DD_PROFILER_CONSTANTS_H
#define DD_PROFILER_CONSTANTS_H

#include <string>

#include "environment_variables.h"
#include "logger.h"

namespace trace
{

const shared::WSTRING env_vars_to_display[]{environment::tracing_enabled,
                                    environment::debug_enabled,
                                    environment::profiler_home_path,
                                    environment::include_process_names,
                                    environment::exclude_process_names,
                                    environment::agent_host,
                                    environment::agent_port,
                                    environment::env,
                                    environment::service_name,
                                    environment::service_version,
                                    environment::disabled_integrations,
                                    environment::log_path,
                                    environment::log_directory,
                                    environment::clr_disable_optimizations,
                                    environment::clr_enable_inlining,
                                    environment::clr_enable_ngen,
                                    environment::dump_il_rewrite_enabled,
                                    environment::azure_app_services,
                                    environment::azure_app_services_app_pool_id,
                                    environment::azure_app_services_cli_telemetry_profile_value};

const shared::WSTRING skip_assembly_prefixes[]{
    WStr("Microsoft.AI"),
    WStr("Microsoft.ApplicationInsights"),
    WStr("Microsoft.Build"),
    WStr("Microsoft.CSharp"),
    WStr("Microsoft.Extensions.Caching"),
    WStr("Microsoft.Extensions.Configuration"),
    WStr("Microsoft.Extensions.DependencyInjection"),
    WStr("Microsoft.Extensions.DependencyModel"),
    WStr("Microsoft.Extensions.Diagnostics"),
    WStr("Microsoft.Extensions.FileProviders"),
    WStr("Microsoft.Extensions.FileSystemGlobbing"),
    WStr("Microsoft.Extensions.Hosting"),
    WStr("Microsoft.Extensions.Http"),
    WStr("Microsoft.Extensions.Identity"),
    WStr("Microsoft.Extensions.Localization"),
    WStr("Microsoft.Extensions.ObjectPool"),
    WStr("Microsoft.Extensions.Options"),
    WStr("Microsoft.Extensions.PlatformAbstractions"),
    WStr("Microsoft.Extensions.Primitives"),
    WStr("Microsoft.Extensions.WebEncoders"),
    WStr("Microsoft.Web.Compilation.Snapshots"),
    WStr("System.Core"),
    WStr("System.Console"),
    WStr("System.Collections"),
    WStr("System.ComponentModel"),
    WStr("System.Diagnostics"),
    WStr("System.Drawing"),
    WStr("System.EnterpriseServices"),
    WStr("System.IO"),
    WStr("System.Runtime"),
    WStr("System.Text"),
    WStr("System.Threading"),
    WStr("System.Xml"),
};

const shared::WSTRING skip_traceattribute_assembly_prefixes[]{
    WStr("System."), WStr("Microsoft."), WStr("Datadog.")};

const shared::WSTRING skip_assemblies[]{WStr("mscorlib"),
                                WStr("netstandard"),
                                WStr("System.Configuration"),
                                WStr("Microsoft.AspNetCore.Razor.Language"),
                                WStr("Microsoft.AspNetCore.Mvc.RazorPages"),
                                WStr("Anonymously Hosted DynamicMethods Assembly"),
                                WStr("Datadog.AutoInstrumentation.ManagedLoader"),
                                WStr("ISymWrapper")};

const shared::WSTRING mscorlib_assemblyName = WStr("mscorlib");
const shared::WSTRING system_private_corelib_assemblyName = WStr("System.Private.CoreLib");
const shared::WSTRING datadog_trace_clrprofiler_managed_loader_assemblyName = WStr("SignalFx.Tracing.ClrProfiler.Managed.Loader");

const shared::WSTRING managed_profiler_full_assembly_version =
    WStr("SignalFx.Tracing, Version=1.1.1.0, Culture=neutral, PublicKeyToken=e43a27c2023d388a");

const shared::WSTRING managed_profiler_name = WStr("SignalFx.Tracing");

const shared::WSTRING nonwindows_nativemethods_type = WStr("Datadog.Trace.ClrProfiler.NativeMethods+NonWindows");
const shared::WSTRING windows_nativemethods_type = WStr("Datadog.Trace.ClrProfiler.NativeMethods+Windows");

const shared::WSTRING profiler_nativemethods_type = WStr("Datadog.Trace.ContinuousProfiler.NativeInterop+NativeMethods");
const shared::WSTRING native_loader_nativemethods_type = WStr("Datadog.Trace.NativeLoader+NativeMethods");

const shared::WSTRING debugger_nonwindows_nativemethods_type = WStr("Datadog.Trace.Debugger.PInvoke.DebuggerNativeMethods+NonWindows");
const shared::WSTRING debugger_windows_nativemethods_type = WStr("Datadog.Trace.Debugger.PInvoke.DebuggerNativeMethods+Windows");

const shared::WSTRING calltarget_modification_action = WStr("CallTargetModification");

const shared::WSTRING distributed_tracer_type_name = WStr("Datadog.Trace.ClrProfiler.DistributedTracer");
const shared::WSTRING distributed_tracer_interface_name = WStr("Datadog.Trace.ClrProfiler.IDistributedTracer");
const shared::WSTRING distributed_tracer_target_method_name = WStr("__GetInstanceForProfiler__");

#ifdef _WIN32
const shared::WSTRING native_dll_filename = WStr("SIGNALFX.TRACING.CLRPROFILER.NATIVE.DLL");
#elif MACOS
const shared::WSTRING native_dll_filename = WStr("SignalFx.Tracing.ClrProfiler.Native.dylib");
#else
const shared::WSTRING native_dll_filename = WStr("SignalFx.Tracing.ClrProfiler.Native.so");
#endif

const AssemblyProperty managed_profiler_assembly_property = AssemblyProperty(
    managed_profiler_name,
    new BYTE[160]{0,   36,  0,   0,   4,   128, 0,   0,   148, 0,   0,   0,   6,   2,   0,   0,   0,   36,  0,   0,
                  82,  83,  65,  49,  0,   4,   0,   0,   1,   0,   1,   0,   113, 25,  157, 139, 5,   140, 14,  183,
                  143, 206, 5,   141, 85,  31,  218, 167, 100, 218, 115, 54,  243, 178, 58,  94,  113, 205, 1,   61,
                  202, 244, 182, 105, 61,  229, 163, 152, 162, 242, 205, 220, 5,   72,  75,  181, 86,  34,  3,   77,
                  214, 74,  215, 90,  162, 58,  218, 216, 253, 227, 176, 27,  110, 33,  34,  84,  150, 63,  8,   30,
                  168, 108, 125, 174, 108, 72,  0,   80,  13,  222, 89,  226, 104, 231, 249, 228, 238, 194, 224, 67,
                  123, 102, 42,  57,  219, 122, 95,  191, 59,  10,  120, 157, 167, 170, 1,   81,  183, 182, 51,  111,
                  204, 130, 205, 122, 20,  157, 247, 246, 102, 245, 57,  108, 141, 233, 44,  166, 68,  215, 162, 209},
    160, 32772, 1)
        .WithVersion(1, 1, 1, 0);

} // namespace trace

#endif // DD_PROFILER_CONSTANTS_H
