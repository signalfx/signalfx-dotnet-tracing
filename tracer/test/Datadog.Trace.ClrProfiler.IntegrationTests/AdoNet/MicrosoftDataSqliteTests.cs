// <copyright file="MicrosoftDataSqliteTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AdoNet
{
    public class MicrosoftDataSqliteTests : TestHelper
    {
        public MicrosoftDataSqliteTests(ITestOutputHelper output)
            : base("Microsoft.Data.Sqlite", output)
        {
            SetServiceVersion("1.0.0");
        }

        [SkippableTheory]
        [MemberData(nameof(PackageVersions.MicrosoftDataSqlite), MemberType = typeof(PackageVersions))]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("Category", "ArmUnsupported")]
        public void SubmitsTraces(string packageVersion)
        {
#if NETCOREAPP3_0
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IsAlpine")) // set in dockerfile
             && !string.IsNullOrEmpty(packageVersion)
             && new Version(packageVersion) >= new Version("6.0.0"))
            {
                Output.WriteLine("Skipping as Microsoft.Data.Sqlite hanqs on Alpine .NET Core 3.0 with 6.0.0 package");
                return;
            }
#endif
            const int expectedSpanCount = 91;
            const string dbType = "sqlite";
            const string expectedOperationName = dbType + ".query";
            const string expectedServiceName = "Samples.Microsoft.Data.Sqlite";

            int agentPort = TcpPortProvider.GetOpenPort();
            using var agent = new MockTracerAgent(agentPort);
            using var process = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion);
            var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);

            Assert.Equal(expectedSpanCount, spans.Count);

            foreach (var span in spans)
            {
                Assert.Equal(expectedOperationName, span.Name);
                Assert.Equal(expectedServiceName, span.Service);
                Assert.Equal(SpanTypes.Sql, span.Type);
                Assert.Equal(dbType, span.Tags[Tags.DbType]);
                Assert.Contains(Tags.Version, (IDictionary<string, string>)span.Tags);
                Assert.Contains(Tags.DbStatement, (IDictionary<string, string>)span.Tags);
            }
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        [Trait("Category", "ArmUnsupported")]
        public void IntegrationDisabled()
        {
            const int totalSpanCount = 21;
            const string expectedOperationName = "mssql.query";

            SetEnvironmentVariable($"SIGNALFX_TRACE_{nameof(IntegrationId.Sqlite)}_ENABLED", "false");

            string packageVersion = PackageVersions.MicrosoftDataSqlite.First()[0] as string;
            int agentPort = TcpPortProvider.GetOpenPort();
            using var agent = new MockTracerAgent(agentPort);
            using var process = RunSampleAndWaitForExit(agent.Port, packageVersion: packageVersion);
            var spans = agent.WaitForSpans(totalSpanCount, returnAllOperations: true);

            Assert.NotEmpty(spans);
            Assert.Empty(spans.Where(s => s.Name.Equals(expectedOperationName)));
        }
    }
}
