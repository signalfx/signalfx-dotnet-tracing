// <copyright file="VerifyHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VerifyTests;

namespace Datadog.Trace.TestHelpers
{
    public static class VerifyHelper
    {
        private static readonly Regex LocalhostRegex = new(@"localhost\:\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex KeepRateRegex = new(@"_dd.tracer_kr: \d\.\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TimeUnixNanoRegex = new(@"TimeUnixNano: \d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex VersionRegex = new(@"StringValue: \d\.\d\.\d\.\d", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NidRegex = new(@"nid=0x\S{4}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TidRegex = new(@"tid=0x\S+ ", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// With <see cref="Verify"/>, parameters are used as part of the filename.
        /// This method produces a "sanitised" version to remove problematic values
        /// </summary>
        /// <param name="path">The path to sanitise</param>
        /// <returns>The sanitised path</returns>
        public static string SanitisePathsForVerify(string path)
        {
            // TODO: Make this more robust
            return path
                  .Replace(@"\", "_")
                  .Replace("/", "_")
                  .Replace("?", "-");
        }

        public static VerifySettings GetSpanVerifierSettings(params object[] parameters)
        {
            var settings = new VerifySettings();

            DerivePathInfoForSnapshotFiles();

            if (parameters.Length > 0)
            {
                settings.UseParameters(parameters);
            }

            VerifierSettings.MemberConverter<MockSpan, Dictionary<string, string>>(x => x.Tags, ScrubStackTraceForErrors);

            settings.ModifySerialization(_ =>
            {
                _.IgnoreMember<MockSpan>(s => s.Duration);
                _.IgnoreMember<MockSpan>(s => s.Start);
            });

            settings.AddScrubber(builder => ReplaceRegex(builder, LocalhostRegex, "localhost:00000"));
            settings.AddScrubber(builder => ReplaceRegex(builder, KeepRateRegex, "_dd.tracer_kr: 1.0"));

            return settings;
        }

        public static VerifySettings GetThreadSamplingVerifierSettings()
        {
            var settings = new VerifySettings();

            DerivePathInfoForSnapshotFiles();

            settings.DisableRequireUniquePrefix();

            settings.AddScrubber(builder => ReplaceRegex(builder, TimeUnixNanoRegex, "TimeUnixNano: FakeTimeUnixNano"));
            settings.AddScrubber(builder => ReplaceRegex(builder, VersionRegex, "StringValue: w.x.y.z"));
            settings.AddScrubber(builder => ReplaceRegex(builder, TidRegex, "tid=0xaaaaaa "));
            settings.AddScrubber(builder => ReplaceRegex(builder, NidRegex, "nid=0x0000"));

            return settings;
        }

        private static void DerivePathInfoForSnapshotFiles()
        {
            VerifierSettings.DerivePathInfo(
                (sourceFile, projectDirectory, type, method) =>
                {
                    return new(directory: Path.Combine(projectDirectory, "..", "snapshots"));
                });
        }

        private static void ReplaceRegex(StringBuilder builder, Regex regex, string replacement)
        {
            var value = builder.ToString();
            var result = regex.Replace(value, replacement);

            if (value.Equals(result, StringComparison.Ordinal))
            {
                return;
            }

            builder.Clear();
            builder.Append(result);
        }

        private static Dictionary<string, string> ScrubStackTraceForErrors(
            MockSpan span, Dictionary<string, string> tags)
        {
            return tags
                  .Select(
                       kvp => kvp.Key switch
                       {
                           Tags.ErrorStack => new KeyValuePair<string, string>(kvp.Key, ScrubStackTrace(kvp.Value)),
                           Tags.SignalFxVersion => new KeyValuePair<string, string>(kvp.Key, "x.y.z"),
                           _ => kvp
                       })
                  .OrderBy(x => x.Key)
                  .ToDictionary(x => x.Key, x => x.Value);
        }

        private static string ScrubStackTrace(string stackTrace)
        {
            // keep the message + the first (scrubbed) location
            var sb = new StringBuilder();
            using StringReader reader = new(Scrubbers.ScrubStackTrace(stackTrace));
            string line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (line.StartsWith("at "))
                {
                    // add the first line of the stack trace
                    sb.Append(line);
                    break;
                }

                sb
                   .Append(line)
                   .Append('\n');
            }

            return sb.ToString();
        }
    }
}
