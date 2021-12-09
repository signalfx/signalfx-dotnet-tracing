using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.IO;

public static class BuildVariables
{
    public static void AddDebuggerEnvironmentVariables(this Dictionary<string, string> envVars, AbsolutePath tracerHomeDirectory)
    {
        envVars.AddTracerEnvironmentVariables(tracerHomeDirectory);
        envVars.Add("SINGNALFX_DEBUGGER_ENABLED", "1");
    }

    public static void AddContinuousProfilerEnvironmentVariables(this Dictionary<string, string> envVars, AbsolutePath tracerHomeDirectory)
    {
        envVars.AddTracerEnvironmentVariables(tracerHomeDirectory);
    }

    public static void AddTracerEnvironmentVariables(this Dictionary<string, string> envVars, AbsolutePath tracerHomeDirectory)
    {
        envVars.Add("COR_ENABLE_PROFILING", "1");
        envVars.Add("COR_PROFILER", "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}");
        envVars.Add("COR_PROFILER_PATH_32", tracerHomeDirectory / "win-x86" / "SignalFx.Tracing.ClrProfiler.Native.dll");
        envVars.Add("COR_PROFILER_PATH_64", tracerHomeDirectory / "win-x64" / "SignalFx.Tracing.ClrProfiler.Native.dll");
        envVars.Add("CORECLR_ENABLE_PROFILING", "1");
        envVars.Add("CORECLR_PROFILER", "{B4C89B0F-9908-4F73-9F59-0D77C5A06874}");
        envVars.Add("DD_DOTNET_TRACER_HOME", tracerHomeDirectory);


        if (EnvironmentInfo.IsWin)
        {
            envVars.Add("CORECLR_PROFILER_PATH_32", tracerHomeDirectory / "win-x86" / "SignalFx.Tracing.ClrProfiler.Native.dll");
            envVars.Add("CORECLR_PROFILER_PATH_64", tracerHomeDirectory / "win-x64" / "SignalFx.Tracing.ClrProfiler.Native.dll");
        }
        else
        {
            envVars.Add("CORECLR_PROFILER_PATH", tracerHomeDirectory / "SignalFx.Tracing.ClrProfiler.Native.so");
        }
    }

    public static void AddExtraEnvVariables(this Dictionary<string, string> envVars, string[] extraEnvVars)
    {
        if (extraEnvVars == null || extraEnvVars.Length == 0)
        {
            return;
        }

        foreach (var envVar in extraEnvVars)
        {
            var kvp = envVar.Split('=');
            envVars[kvp[0]] = kvp[1];
        }
    }
}
