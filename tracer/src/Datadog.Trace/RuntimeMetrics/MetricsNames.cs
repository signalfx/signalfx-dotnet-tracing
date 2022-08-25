// <copyright file="MetricsNames.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

namespace Datadog.Trace.RuntimeMetrics
{
    internal static class MetricsNames
    {
        private const string MetricPrefix = "process.runtime.dotnet.";
        public const string ExceptionsCount = $"{MetricPrefix}exceptions.count";

        public const string ContentionTime = "runtime.dotnet.threads.contention_time";
        public const string ContentionCount = $"{MetricPrefix}monitor.lock_contention.count";

        public const string ThreadPoolWorkersCount = $"{MetricPrefix}thread_pool.threads.count";

        public const string ThreadsCount = "runtime.dotnet.threads.count";

        public const string CommittedMemory = "runtime.dotnet.mem.committed";

        public const string CpuUserTime = "runtime.dotnet.cpu.user";
        public const string CpuSystemTime = "runtime.dotnet.cpu.system";
        public const string CpuPercentage = "runtime.dotnet.cpu.percent";

        public const string AspNetCoreCurrentRequests = "runtime.dotnet.aspnetcore.requests.current";
        public const string AspNetCoreFailedRequests = "runtime.dotnet.aspnetcore.requests.failed";
        public const string AspNetCoreTotalRequests = "runtime.dotnet.aspnetcore.requests.total";
        public const string AspNetCoreRequestQueueLength = "runtime.dotnet.aspnetcore.requests.queue_length";

        public const string AspNetCoreCurrentConnections = "runtime.dotnet.aspnetcore.connections.current";
        public const string AspNetCoreConnectionQueueLength = "runtime.dotnet.aspnetcore.connections.queue_length";
        public const string AspNetCoreTotalConnections = "runtime.dotnet.aspnetcore.connections.total";

        internal static class Gc
        {
            public const string CollectionsCount = $"{MetricPrefix}gc.collections.count";
            public const string HeapSize = $"{MetricPrefix}gc.heap.size";
            public const string AllocatedBytes = $"{MetricPrefix}gc.allocations.size";
            public const string HeapCommittedMemory = $"{MetricPrefix}gc.committed_memory.size";
        }
    }
}
