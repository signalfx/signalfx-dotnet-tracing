// <copyright file="EnvironmentVariables.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.
// </copyright>

namespace Datadog.Profiler.IntegrationTests.Helpers
{
    internal class EnvironmentVariables
    {
        public const string ProfilingLogDir = "SIGNALFX_PROFILING_LOG_DIR";
        public const string ProfilingPprofDir = "SIGNALFX_INTERNAL_PROFILING_OUTPUT_DIR";
        public const string ProfilerInstallationFolder = "SIGNALFX_TESTING_PROFILER_FOLDER";
        public const string CodeHotSpotsEnable = "SIGNALFX_PROFILING_CODEHOTSPOTS_ENABLED";
        public const string UseNativeLoader = "SIGNALFX_USE_NATIVE_LOADER";
        public const string CpuProfilerEnabled = "SIGNALFX_PROFILING_CPU_ENABLED";
        public const string ExceptionProfilerEnabled = "SIGNALFX_PROFILING_EXCEPTION_ENABLED";
    }
}
