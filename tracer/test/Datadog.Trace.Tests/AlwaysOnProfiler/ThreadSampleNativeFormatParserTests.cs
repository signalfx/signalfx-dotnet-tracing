// Modified by Splunk Inc.

#if NETCOREAPP3_1_OR_GREATER
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace.AlwaysOnProfiler;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace Datadog.Trace.Tests.AlwaysOnProfiler
{
    [UsesVerify]
    public class ThreadSampleNativeFormatParserTests
    {
        static ThreadSampleNativeFormatParserTests()
        {
            VerifierSettings.DerivePathInfo(
                (sourceFile, projectDirectory, type, method) =>
                {
                    return new(directory: Path.Combine(projectDirectory, "..", "snapshots"));
                });
        }

        public static IEnumerable<object[]> GetBufferFiles =>
            Enumerable.Range(0, 3).Select(n => new object[] { $"Buffer{n:D6}" });

        [Theory]
        [MemberData(nameof(GetBufferFiles))]
        public async Task ParseSampleBuffer(string fileName)
        {
            var buf = File.ReadAllBytes($"../../../AlwaysOnProfiler/Buffers/{fileName}.bin");
            var parser = new ThreadSampleNativeFormatParser(buf, buf.Length);
            var samples = parser.Parse();

            VerifySettings settings = new();
            settings.UseParameters(fileName);
            await Verifier.Verify(samples, settings);
        }
    }
}
#endif
