#!/bin/bash
# Modified by SignalFx
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." >/dev/null && pwd )"

export CORECLR_ENABLE_PROFILING="1"
export CORECLR_PROFILER="{B4C89B0F-9908-4F73-9F59-0D77C5A06874}"
export CORECLR_PROFILER_PATH="${DIR}/src/Datadog.Trace.ClrProfiler.Native/obj/Debug/x64/Datadog.Trace.ClrProfiler.Native.so"
export SIGNALFX_DOTNET_TRACER_HOME="${DIR}"
export SIGNALFX_INTEGRATIONS="${SIGNALFX_DOTNET_TRACER_HOME}/integrations.json"

eval "$@"
