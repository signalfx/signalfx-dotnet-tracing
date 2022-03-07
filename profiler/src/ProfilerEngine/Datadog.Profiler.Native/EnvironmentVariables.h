// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#pragma once

#include "shared/src/native-src/string.h"

class EnvironmentVariables final
{
public:
    inline static const shared::WSTRING ProfilingEnabled            = WStr("SIGNALFX_PROFILING_ENABLED");
    inline static const shared::WSTRING DebugLogEnabled             = WStr("SIGNALFX_TRACE_DEBUG");
    inline static const shared::WSTRING LogPath                     = WStr("SIGNALFX_PROFILING_LOG_PATH");
    inline static const shared::WSTRING LogDirectory                = WStr("SIGNALFX_PROFILING_LOG_DIR");
    inline static const shared::WSTRING OperationalMetricsEnabled   = WStr("SIGNALFX_INTERNAL_OPERATIONAL_METRICS_ENABLED");
    inline static const shared::WSTRING Version                     = WStr("SIGNALFX_VERSION");
    inline static const shared::WSTRING ServiceName                 = WStr("SIGNALFX_SERVICE");
    inline static const shared::WSTRING Environment                 = WStr("SIGNALFX_ENV");
    inline static const shared::WSTRING Site                        = WStr("SIGNALFX_SITE");
    inline static const shared::WSTRING UploadInterval              = WStr("SIGNALFX_PROFILING_UPLOAD_PERIOD");
    inline static const shared::WSTRING AgentUrl                    = WStr("SIGNALFX_TRACE_AGENT_URL");
    inline static const shared::WSTRING AgentHost                   = WStr("SIGNALFX_AGENT_HOST");
    inline static const shared::WSTRING AgentPort                   = WStr("SIGNALFX_TRACE_AGENT_PORT");
    inline static const shared::WSTRING ApiKey                      = WStr("SIGNALFX_API_KEY");
    inline static const shared::WSTRING Hostname                    = WStr("SIGNALFX_HOSTNAME");
    inline static const shared::WSTRING Tags                        = WStr("SIGNALFX_TAGS");
    inline static const shared::WSTRING NativeFramesEnabled         = WStr("SIGNALFX_PROFILING_FRAMES_NATIVE_ENABLED");
    inline static const shared::WSTRING ProfilesOutputDir           = WStr("SIGNALFX_PROFILING_OUTPUT_DIR");
    inline static const shared::WSTRING DevelopmentConfiguration    = WStr("SIGNALFX_INTERNAL_USE_DEVELOPMENT_CONFIGURATION");
    inline static const shared::WSTRING Agentless                   = WStr("SIGNALFX_PROFILING_AGENTLESS");

    // feature flags
    inline static const shared::WSTRING FF_LibddprofEnabled = WStr("SIGNALFX_INTERNAL_PROFILING_LIBDDPROF_ENABLED");
};
