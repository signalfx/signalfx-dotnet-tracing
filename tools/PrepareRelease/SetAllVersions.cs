// Modified by SignalFx
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Datadog.Core.Tools;

namespace PrepareRelease
{
    public static class SetAllVersions
    {
        public static void Run()
        {
            Console.WriteLine($"Updating version instances to {VersionString()}");

            // README.md
            SynchronizeVersion(
                "README.md",
                text => Regex.Replace(text, $"ARG TRACER_VERSION={VersionPattern()}", $"ARG TRACER_VERSION={VersionString()}"));

            // Dockerfile updates
            SynchronizeVersion(
                "customer-samples/ConsoleApp/Alpine3.9.dockerfile",
                text => Regex.Replace(text, $"ARG TRACER_VERSION={VersionPattern()}", $"ARG TRACER_VERSION={VersionString()}"));

            SynchronizeVersion(
                "customer-samples/ConsoleApp/Alpine3.10.dockerfile",
                text => Regex.Replace(text, $"ARG TRACER_VERSION={VersionPattern()}", $"ARG TRACER_VERSION={VersionString()}"));

            SynchronizeVersion(
                "customer-samples/ConsoleApp/Debian.dockerfile",
                text => Regex.Replace(text, $"ARG TRACER_VERSION={VersionPattern()}", $"ARG TRACER_VERSION={VersionString()}"));

            SynchronizeVersion(
                "reproductions/AutomapperTest/Dockerfile",
                text => Regex.Replace(text, $"ARG TRACER_VERSION={VersionPattern()}", $"ARG TRACER_VERSION={VersionString()}"));

            // Managed project / NuGet package updates
            SynchronizeVersion(
                "src/Datadog.Trace/Datadog.Trace.csproj",
                NugetVersionReplace);

            SynchronizeVersion(
                "src/Datadog.Trace/Tracer.cs",
                PartialAssemblyNameReplace);

            SynchronizeVersion(
                "src/Datadog.Trace.AspNet/Datadog.Trace.AspNet.csproj",
                NugetVersionReplace);

            SynchronizeVersion(
                "src/Datadog.Trace.ClrProfiler.Managed/Datadog.Trace.ClrProfiler.Managed.csproj",
                NugetVersionReplace);

            SynchronizeVersion(
                "src/Datadog.Trace.ClrProfiler.Managed.Core/Datadog.Trace.ClrProfiler.Managed.Core.csproj",
                NugetVersionReplace);

            SynchronizeVersion(
                "src/Datadog.Trace.ClrProfiler.Managed.Loader/Datadog.Trace.ClrProfiler.Managed.Loader.csproj",
                NugetVersionReplace);

            SynchronizeVersion(
                "src/Datadog.Trace.OpenTracing/Datadog.Trace.OpenTracing.csproj",
                NugetVersionReplace);

            // Fully qualified name updates
            SynchronizeVersion(
                "src/Datadog.Trace.ClrProfiler.Managed.Loader/Startup.cs",
                FullAssemblyNameReplace);

            // Locked AssemblyVersion #.0.0.0 updates
            #pragma warning disable CS0162
            if (TracerVersion.Major > 0)
            {
                SynchronizeVersion(
                    "src/Datadog.Trace.AspNet/AssemblyInfo.cs",
                    text => MajorAssemblyVersionReplace(text, "."));

                SynchronizeVersion(
                    "src/Datadog.Trace.ClrProfiler.Managed.Core/AssemblyInfo.cs",
                    text => MajorAssemblyVersionReplace(text, "."));
            }
            #pragma warning restore CS0162

            // Native profiler updates
            SynchronizeVersion(
                "src/Datadog.Trace.ClrProfiler.Native/CMakeLists.txt",
                text => FullVersionReplace(text, "."));

            SynchronizeVersion(
                "src/Datadog.Trace.ClrProfiler.Native/Resource.rc",
                text =>
                {
                    text = FullVersionReplace(text, ",");
                    text = FullVersionReplace(text, ".");
                    return text;
                });

            SynchronizeVersion(
                "src/Datadog.Trace.ClrProfiler.Native/version.h",
                text => FullVersionReplace(text, "."));

            // Deployment updates
            SynchronizeVersion(
                "integrations.json",
                FullAssemblyNameReplace);

            SynchronizeVersion(
                "docker/package.sh",
                text => Regex.Replace(text, $"VERSION={VersionPattern()}", $"VERSION={VersionString()}"));

            SynchronizeVersion(
                "deploy/Datadog.Trace.ClrProfiler.WindowsInstaller/Datadog.Trace.ClrProfiler.WindowsInstaller.wixproj",
                WixProjReplace);

            // Azure Site Extension updates
            SynchronizeVersion(
                "deploy/Azure.Site.Extension/applicationHost.xdt",
                text => Regex.Replace(text, VersionPattern(), VersionString()));
            SynchronizeVersion(
                "deploy/Azure.Site.Extension/Azure.Site.Extension.nuspec",
                text => Regex.Replace(text, VersionPattern(), VersionString()));
            SynchronizeVersion(
                "deploy/Azure.Site.Extension/install.cmd",
                text => Regex.Replace(text, VersionPattern(), VersionString()));

            // Buildpack updates
            SynchronizeVersion(
                "deployments/cloudfoundry/buildpack-linux/bin/supply",
                text => Regex.Replace(text, $"LATEST_VERSION = \"{VersionPattern()}\"", $"LATEST_VERSION = \"{VersionString()}\""));
            SynchronizeVersion(
                "deployments/cloudfoundry/buildpack-linux/README.md",
                text => Regex.Replace(text, $"\\$ cf set-env SIGNALFX_DOTNET_TRACING_VERSION \"{VersionPattern()}\"", $"$ cf set-env SIGNALFX_DOTNET_TRACING_VERSION \"{VersionString()}\""));
            SynchronizeVersion(
                "deployments/cloudfoundry/buildpack-windows/src/supply/supply.go",
                text => Regex.Replace(text, $"const LatestVersion = \"{VersionPattern()}\"", $"const LatestVersion = \"{VersionString()}\""));
            SynchronizeVersion(
                "deployments/cloudfoundry/buildpack-windows/README.md",
                text => Regex.Replace(text, $"\\$ cf set-env SIGNALFX_DOTNET_TRACING_VERSION \"{VersionPattern()}\"", $"$ cf set-env SIGNALFX_DOTNET_TRACING_VERSION \"{VersionString()}\""));

            Console.WriteLine($"Completed synchronizing versions to {VersionString()}");
        }

        private static string FullVersionReplace(string text, string split)
        {
            return Regex.Replace(text, VersionPattern(split), VersionString(split), RegexOptions.Singleline);
        }

        private static string FullAssemblyNameReplace(string text)
        {
            return Regex.Replace(text, AssemblyString(VersionPattern()), AssemblyString(VersionString()), RegexOptions.Singleline);
        }

        private static string PartialAssemblyNameReplace(string text)
        {
            return Regex.Replace(text, PartialAssemblyString(VersionPattern()), PartialAssemblyString(VersionString()), RegexOptions.Singleline);
        }

        private static string MajorAssemblyVersionReplace(string text, string split)
        {
            return Regex.Replace(text, VersionPattern(fourPartVersion: true), MajorVersionString(split), RegexOptions.Singleline);
        }

        private static string NugetVersionReplace(string text)
        {
            return Regex.Replace(text, $"<Version>{VersionPattern(withPrereleasePostfix: true)}</Version>", $"<Version>{VersionString(withPrereleasePostfix: true)}</Version>", RegexOptions.Singleline);
        }

        private static string NuspecVersionReplace(string text)
        {
            return Regex.Replace(text, $"<version>{VersionPattern(withPrereleasePostfix: true)}</version>", $"<version>{VersionString(withPrereleasePostfix: true)}</version>", RegexOptions.Singleline);
        }

        private static string WixProjReplace(string text)
        {
            text = Regex.Replace(
                text,
                $"<OutputName>signalfx-dotnet-tracing-{VersionPattern(withPrereleasePostfix: true)}-\\$\\(Platform\\)</OutputName>",
                $"<OutputName>signalfx-dotnet-tracing-{VersionString(withPrereleasePostfix: true)}-$(Platform)</OutputName>",
                RegexOptions.Singleline);

            text = Regex.Replace(
                text,
                $"InstallerVersion={VersionPattern()}",
                $"InstallerVersion={VersionString()}",
                RegexOptions.Singleline);

            return text;
        }

        private static void SynchronizeVersion(string path, Func<string, string> transform)
        {
            var solutionDirectory = EnvironmentTools.GetSolutionDirectory();
            var fullPath = Path.Combine(solutionDirectory, path);

            Console.WriteLine($"Updating version instances for {path}");

            if (!File.Exists(fullPath))
            {
                throw new Exception($"File not found to version: {path}");
            }

            var fileContent = File.ReadAllText(fullPath);
            var newFileContent = transform(fileContent);

            File.WriteAllText(fullPath, newFileContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static string MajorVersionString(string split = ".")
        {
            return $"{TracerVersion.Major}{split}0{split}0{split}0";
        }

        private static string VersionString(string split = ".", bool withPrereleasePostfix = false)
        {
            var newVersion = $"{TracerVersion.Major}{split}{TracerVersion.Minor}{split}{TracerVersion.Patch}";

            // this gets around a compiler warning about unreachable code below
            var isPreRelease = TracerVersion.IsPreRelease;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (withPrereleasePostfix && isPreRelease)
            {
                newVersion = newVersion + "-prerelease";
            }

            return newVersion;
        }

        private static string VersionPattern(string split = ".", bool withPrereleasePostfix = false, bool fourPartVersion = false)
        {
            if (split == ".")
            {
                split = @"\.";
            }

            var pattern = $@"\d+{split}\d+{split}\d+";

            if (fourPartVersion)
            {
                pattern = pattern + $@"{split}\d+";
            }

            if (withPrereleasePostfix)
            {
                pattern = pattern + "(\\-prerelease)?";
            }

            return pattern;
        }

        private static string AssemblyString(string versionText)
        {
            var partial = PartialAssemblyString(versionText);
            return $"SignalFx.Tracing.ClrProfiler.Managed, {partial}";
        }

        private static string PartialAssemblyString(string versionText)
        {
            return $"Version={versionText}.0, Culture=neutral, PublicKeyToken=def86d061d0d2eeb";
        }
    }
}
