// <copyright file="ProfilerHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Diagnostics;

namespace Datadog.Trace.TestHelpers
{
    public class ProfilerHelper
    {
        public static Process StartProcessWithProfiler(
            string executable,
            EnvironmentHelper environmentHelper,
            MockTracerAgent agent,
            string arguments = null,
            bool redirectStandardInput = false,
            int aspNetCorePort = 5000,
            int? logsCollectorPort = null,
            string processToProfile = null,
            string externalRulesFile = null)
        {
            if (environmentHelper == null)
            {
                throw new ArgumentNullException(nameof(environmentHelper));
            }

            // clear all relevant environment variables to start with a clean slate
            EnvironmentHelper.ClearProfilerEnvironmentVariables();

            var startInfo = new ProcessStartInfo(executable, $"{arguments ?? string.Empty}");

            environmentHelper.SetEnvironmentVariables(
                agent,
                aspNetCorePort,
                logsCollectorPort,
                startInfo.Environment,
                processToProfile,
                externalRulesFile);

            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = redirectStandardInput;

            return Process.Start(startInfo);
        }
    }
}
