// <copyright file="ConfigurationKeys.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.ContinuousProfiler
{
    internal static class ConfigurationKeys
    {
        public const string ProfilingEnabled = "SIGNALFX_PROFILING_ENABLED";
        public const string CodeHotspotsEnabled = "SIGNALFX_PROFILING_CODEHOTSPOTS_ENABLED";
    }
}
