// Modified by SignalFx
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Datadog.Trace.TestHelpers
{
    public class ProfilerHelper
    {
        public static Process StartProcessWithProfiler(
            EnvironmentHelper environmentHelper,
            IEnumerable<string> integrationPaths,
            string arguments = null,
            bool redirectStandardInput = false,
            int traceAgentPort = 9696,
            int aspNetCorePort = 5000,
            Dictionary<string, string> envVars = null)
        {
            if (environmentHelper == null)
            {
                throw new ArgumentNullException(nameof(environmentHelper));
            }

            if (integrationPaths == null)
            {
                throw new ArgumentNullException(nameof(integrationPaths));
            }

            var applicationPath = environmentHelper.GetSampleApplicationPath();
            var executable = environmentHelper.GetSampleExecutionSource();

            // clear all relevant environment variables to start with a clean slate
            EnvironmentHelper.ClearProfilerEnvironmentVariables();

            ProcessStartInfo startInfo;

            if (EnvironmentHelper.IsCoreClr())
            {
                // .NET Core
                startInfo = new ProcessStartInfo(executable, $"{applicationPath} {arguments ?? string.Empty}");
            }
            else
            {
                // .NET Framework
                startInfo = new ProcessStartInfo(executable, $"{arguments ?? string.Empty}");
            }

            environmentHelper.SetEnvironmentVariables(traceAgentPort, aspNetCorePort, executable, startInfo.EnvironmentVariables);

            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = redirectStandardInput;

            if (envVars != null)
            {
                foreach (KeyValuePair<string, string> entry in envVars)
                {
                    startInfo.EnvironmentVariables[entry.Key] = entry.Value;
                }
            }

            return Process.Start(startInfo);
        }
    }
}
