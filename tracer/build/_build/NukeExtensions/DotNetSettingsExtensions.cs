// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;

internal static partial class DotNetSettingsExtensions
{
    public static DotNetBuildSettings SetTargetPlatformAnyCPU(this DotNetBuildSettings settings)
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static DotNetTestSettings SetTargetPlatformAnyCPU(this DotNetTestSettings settings)
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static DotNetPublishSettings SetTargetPlatformAnyCPU(this DotNetPublishSettings settings)
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static T SetTargetPlatformAnyCPU<T>(this T settings)
        where T: MSBuildSettings
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static DotNetBuildSettings SetTargetPlatform(this DotNetBuildSettings settings, MSBuildTargetPlatform platform)
    {
        return platform is null
            ? settings
            : settings.SetProperty("Platform", GetTargetPlatform(platform));
    }
    public static DotNetTestSettings SetTargetPlatform(this DotNetTestSettings settings, MSBuildTargetPlatform platform)
    {
        return platform is null
            ? settings
            : settings.SetProperty("Platform", GetTargetPlatform(platform));
    }

    public static DotNetPublishSettings SetTargetPlatform(this DotNetPublishSettings settings, MSBuildTargetPlatform platform)
    {
        return platform is null
            ? settings
            : settings.SetProperty("Platform", GetTargetPlatform(platform));
    }

    private static string GetTargetPlatform(MSBuildTargetPlatform platform) =>
        platform == MSBuildTargetPlatform.MSIL ? "AnyCPU" : platform.ToString();

    public static T SetNoWarnDotNetCore3<T>(this T settings)
        where T: ToolSettings
    {
        return settings.SetProcessArgumentConfigurator(
            arg => arg.Add("/nowarn:netsdk1138"));
    }
    
    public static T SetPlatform<T>(this T settings, MSBuildTargetPlatform platform)
        where T: NuGetRestoreSettings
    {
        return settings.SetProcessArgumentConfigurator(
            arg => arg.Add($"/p:\"Platform={platform}\""));
    }
    
    public static T SetDDEnvironmentVariables<T>(this T settings, string serviceName)
        where T: ToolSettings
    {
        return settings.SetProcessEnvironmentVariable("SIGNALFX_SERVICE_NAME", serviceName);
    }

    public static T SetLogsDirectory<T>(this T settings, AbsolutePath logsDirectory)
        where T: ToolSettings
    {
        return settings.SetProcessEnvironmentVariable("SIGNALFX_LOG_DIRECTORY", logsDirectory);
    }
    
    public static T SetProcessEnvironmentVariables<T>(this T settings, IEnumerable<KeyValuePair<string, string>> variables)
        where T: ToolSettings
    {
        foreach (var keyValuePair in variables)
        {
            settings = settings.SetProcessEnvironmentVariable(keyValuePair.Key, keyValuePair.Value);
        }

        return settings;
    }

    public static T EnableNoDependencies<T>(this T settings)
        where T: MSBuildSettings
    {
        return settings.SetProperty("BuildProjectReferences", false);
    }

    public static DotNetTestSettings EnableTrxLogOutput(this DotNetTestSettings settings, string resultsDirectory)
    {
        return settings
            .SetLoggers("trx")
            .SetResultsDirectory(resultsDirectory);
    }

    public static DotNetMSBuildSettings EnableTrxLogOutput(this DotNetMSBuildSettings settings, string resultsDirectory)
    {
        return settings
              .SetProperty("VSTestLogger", "trx")
              .SetProperty("VSTestResultsDirectory", resultsDirectory);
    }

    public static DotNetMSBuildSettings SetFilter(this DotNetMSBuildSettings settings, string filter)
    {
        return settings
              .SetProperty("VSTestTestCaseFilter", filter);
    }

    /// <summary>
    /// GitLab installs MSBuild in a non-standard place that causes issues for Nuke trying to resolve it
    /// </summary>
    public static T SetMSBuildPath<T>(this T settings)
        where T : ToolSettings
    {
        var vsRoot = Environment.GetEnvironmentVariable("VSTUDIO_ROOT");

        // Workaround until Nuke supports VS 2022
        string toolPath;
        try
        {
            toolPath = string.IsNullOrEmpty(vsRoot)
                           ? MSBuildToolPathResolver.Resolve()
                           : Path.Combine(vsRoot, "MSBuild", "Current", "Bin", "MSBuild.exe");
        }
        catch
        {

            var editions = new[] { "Enterprise", "Professional", "Community", "BuildTools", "Preview" };
            toolPath = editions
                      .Select(edition => Path.Combine(
                                  EnvironmentInfo.SpecialFolder(SpecialFolders.ProgramFiles)!,
                                  $@"Microsoft Visual Studio\2022\{edition}\MSBuild\Current\Bin\msbuild.exe"))
                      .First(File.Exists);
        }

        return settings.SetProcessToolPath(toolPath);
    }

    /// <summary>
    /// Conditionally set the dotnet.exe location, using the 32-bit dll when targeting x86
    /// </summary>
    public static T SetDotnetPath<T>(this T settings, MSBuildTargetPlatform platform)
        where T : ToolSettings
    {
        if (platform != MSBuildTargetPlatform.x86 && platform != MSBuildTargetPlatform.Win32)
        {
            return settings;
        }


        // assume it's installed where we expect
        var dotnetPath = EnvironmentInfo.GetVariable<string>("DOTNET_EXE_32")
                      ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet", "dotnet.exe");

        if (!File.Exists(dotnetPath))
        {
            throw new Exception($"Error locating 32-bit dotnet process. Expected at '{dotnetPath}'");
        }

        return settings.SetProcessToolPath(dotnetPath);
    }

    /// <summary>
    /// Set filters for tests to ignore
    /// </summary>
    public static T SetIgnoreFilter<T>(this T settings, string[] testsToIgnore)
        where T : DotNetTestSettings
    {
        if (testsToIgnore != null && testsToIgnore.Any())
        {
            var sb = new StringBuilder();
            foreach (var testToIgnore in testsToIgnore)
            {
                sb.Append("FullyQualifiedName!~");
                sb.Append(testToIgnore);
                sb.Append(value: '&');
            }

            sb.Remove(sb.Length - 1, 1);

            settings = settings.SetFilter(sb.ToString());
        }

        return settings;
    }

    public static DotNetTestSettings WithMemoryDumpAfter(this DotNetTestSettings settings, int timeoutInMinutes)
    {
        return settings.SetProcessArgumentConfigurator(
            args =>
                args.Add("--blame-hang")
                    .Add("--blame-hang-dump-type full")
                    .Add($"--blame-hang-timeout {timeoutInMinutes}m")
        );
    }
}
