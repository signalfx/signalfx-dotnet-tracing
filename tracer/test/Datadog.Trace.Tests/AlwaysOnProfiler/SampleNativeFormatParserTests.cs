// Modified by Splunk Inc.

#if NETCOREAPP3_1_OR_GREATER
using System;
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
    public class SampleNativeFormatParserTests
    {
        static SampleNativeFormatParserTests()
        {
            VerifierSettings.DerivePathInfo(
                (sourceFile, projectDirectory, type, method) =>
                {
                    return new(directory: Path.Combine(projectDirectory, "..", "snapshots"));
                });
        }

        public static IEnumerable<object[]> GetBufferFiles =>
            Enumerable.Range(0, 2).Select(n => new object[] { $"Buffer{n:D6}" });

        public static IEnumerable<object[]> GetAllocationBufferFiles =>
            Enumerable.Range(1, 3).Select(n => new object[] { $"AllocationBuffer{n}" });

        [Theory]
        [MemberData(nameof(GetBufferFiles))]
        public async Task ParseSampleBuffer(string fileName)
        {
            var buf = File.ReadAllBytes($"../../../AlwaysOnProfiler/Buffers/{fileName}.bin");
            var samples = SampleNativeFormatParser.ParseThreadSamples(buf, buf.Length);

            VerifySettings settings = new();
            settings.UseParameters(fileName);
            await Verifier.Verify(samples, settings);
        }

        [Fact]
        public async Task ParseTruncatedBuffer()
        {
            var buf = File.ReadAllBytes($"../../../AlwaysOnProfiler/Buffers/TruncatedBuffer.bin");

            var samples = SampleNativeFormatParser.ParseThreadSamples(buf, buf.Length);

            VerifySettings settings = new();
            settings.UseParameters("TruncatedBuffer");
            await Verifier.Verify(samples, settings);
        }

        [Theory]
        [MemberData(nameof(GetAllocationBufferFiles))]
        public async Task ParseSampleAllocationBuffer(string fileName)
        {
            var buf = File.ReadAllBytes($"../../../AlwaysOnProfiler/Buffers/{fileName}.bin");
            var samples = SampleNativeFormatParser.ParseAllocationSamples(buf, buf.Length);

            VerifySettings settings = new();
            settings.UseParameters(fileName);
            await Verifier.Verify(samples, settings);
        }
    }
}
#endif
