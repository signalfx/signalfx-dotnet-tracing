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
export COR_PROFILER="{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"
export COR_PROFILER_PATH="${PWD}/tracer/bin/tracer-home/${OPT_DIR}SignalFx.Tracing.ClrProfiler.Native.${SUFIX}"

# Enable .NET Core Profiling API
export CORECLR_ENABLE_PROFILING="1"
export CORECLR_PROFILER="{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"
export CORECLR_PROFILER_PATH="${PWD}/tracer/bin/tracer-home/${OPT_DIR}SignalFx.Tracing.ClrProfiler.Native.${SUFIX}"

# Configure SFx .NET Tracer 
export SIGNALFX_DOTNET_TRACER_HOME="${PWD}/tracer/bin/tracer-home"
export SIGNALFX_VERSION="1.0.0"
export SIGNALFX_ENDPOINT_URL=${SIGNALFX_ENDPOINT_URL:-"http://localhost:9411/api/v2/spans"}
export SIGNALFX_METRICS_ENDPOINT_URL=${SIGNALFX_METRICS_ENDPOINT_URL:-"http://localhost:9943/v2/datapoint"}
export SIGNALFX_TRACE_DEBUG="1"
export SIGNALFX_DUMP_ILREWRITE_ENABLED="0"
export SIGNALFX_CLR_ENABLE_INLINING="1"
export SIGNALFX_ENV=${SIGNALFX_ENV:-$(whoami)}
export SIGNALFX_RUNTIME_METRICS_ENABLED="1"
