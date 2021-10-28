#!/bin/bash

uname_os() {
    os=$(uname -s | tr '[:upper:]' '[:lower:]')
    case "$os" in
        cygwin_nt*) echo "windows" ;;
        mingw*) echo "windows" ;;
        msys_nt*) echo "windows" ;;
        *) echo "$os" ;;
    esac
}

native_sufix() {
    os=$(uname_os)
    case "$os" in
        windows*) echo "dll" ;;
        linux*) echo "so" ;;
        darwin*) echo "dylib" ;;
        *) echo "OS: ${os} is not supported" ; exit 1 ;;
    esac
}

optional_dir() {
    if [[ $(uname_os) = "windows" ]]; then
        echo "win-x64/"
    fi
}

SUFIX=$(native_sufix)

OPT_DIR=$(optional_dir)

# Enable .NET Framework Profiling API
export COR_ENABLE_PROFILING="1"
export COR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
export COR_PROFILER_PATH="${PWD}/tracer/bin/tracer-home/${OPT_DIR}SignalFx.Instrumentation.ClrProfiler.Native.${SUFIX}"

# Enable .NET Core Profiling API
export CORECLR_ENABLE_PROFILING="1"
export CORECLR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
export CORECLR_PROFILER_PATH="${PWD}/tracer/bin/tracer-home/${OPT_DIR}SignalFx.Instrumentation.ClrProfiler.Native.${SUFIX}"

# Configure SFx .NET Tracer 
export SIGNALFX_DOTNET_TRACER_HOME="${PWD}/tracer/bin/tracer-home"
export SIGNALFX_INTEGRATIONS="${PWD}/tracer/integrations.json"
export SIGNALFX_VERSION="1.0.0"
export SIGNALFX_TRACE_AGENT_URL=${SIGNALFX_TRACE_AGENT_URL:-"http://localhost:9411/api/v2/spans"}
export SIGNALFX_TRACE_DEBUG="1"
export SIGNALFX_EXPORTER="Zipkin"
export SIGNALFX_DUMP_ILREWRITE_ENABLED="0"
export SIGNALFX_CLR_ENABLE_INLINING="1"
export SIGNALFX_CONVENTION="OpenTelemetry"
export SIGNALFX_ENV=${SIGNALFX_ENV:-$(whoami)}
