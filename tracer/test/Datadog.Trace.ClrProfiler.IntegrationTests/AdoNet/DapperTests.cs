// <copyright file="DapperTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AdoNet
{
    [Trait("RequiresDockerDependency", "true")]
    public class DapperTests : TestHelper
    {
        public DapperTests(ITestOutputHelper output)
            : base("Dapper", output)
        {
            SetServiceVersion("1.0.0");
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTraces()
        {
            const int expectedSpanCount = 17;
            const string dbType = "postgresql";
            const string expectedOperationName = dbType + ".query";
            const string expectedServiceName = "Samples.Dapper";

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (RunSampleAndWaitForExit(agent))
            {
                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                Assert.Equal(expectedSpanCount, spans.Count);

                foreach (var span in spans)
                {
                    // Assert Npgsql because the Dapper application uses Postgres for the actual client
                    var result = span.IsNpgsql();
                    Assert.True(result.Success, result.ToString());

                    Assert.Equal(expectedOperationName, span.Name);
                    Assert.Equal(expectedServiceName, span.Service);
                }
            }
        }

        [SkippableFact]
        [Trait("Category", "EndToEnd")]
        public void SubmitsTracesWithNetStandard()
        {
            const int expectedSpanCount = 17;
            const string dbType = "postgresql";
            const string expectedOperationName = dbType + ".query";
            const string expectedServiceName = "Samples.Dapper";

            using (var agent = EnvironmentHelper.GetMockAgent())
            using (RunSampleAndWaitForExit(agent))
            {
                var spans = agent.WaitForSpans(expectedSpanCount, operationName: expectedOperationName);
                Assert.Equal(expectedSpanCount, spans.Count);

                foreach (var span in spans)
                {
                    // Assert Npgsql because the Dapper application uses Postgres for the actual client
                    var result = span.IsNpgsql();
                    Assert.True(result.Success, result.ToString());

                    Assert.Equal(expectedOperationName, span.Name);
                    Assert.Equal(expectedServiceName, span.Service);
                }
            }
        }
    }
}
