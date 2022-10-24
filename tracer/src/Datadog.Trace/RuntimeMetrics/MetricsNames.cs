// <copyright file="MetricsNames.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

namespace Datadog.Trace.RuntimeMetrics
{
    internal static class MetricsNames
    {
        public const string ExceptionsCount = "process.runtime.dotnet.exceptions.count";

        public const string ContentionTime = "runtime.dotnet.threads.contention_time";
        public const string ContentionCount = "process.runtime.dotnet.monitor.lock_contention.count";

        public const string ThreadPoolWorkersCount = "process.runtime.dotnet.thread_pool.threads.count";

        public const string AspNetCoreCurrentRequests = "runtime.dotnet.aspnetcore.requests.current";

        public const string AspNetCoreFailedRequests = "runtime.dotnet.aspnetcore.requests.failed";
        public const string AspNetCoreTotalRequests = "runtime.dotnet.aspnetcore.requests.total";
        public const string AspNetCoreRequestQueueLength = "runtime.dotnet.aspnetcore.requests.queue_length";

        public const string AspNetCoreCurrentConnections = "runtime.dotnet.aspnetcore.connections.current";
        public const string AspNetCoreConnectionQueueLength = "runtime.dotnet.aspnetcore.connections.queue_length";
        public const string AspNetCoreTotalConnections = "runtime.dotnet.aspnetcore.connections.total";

        internal static class Gc
        {
            public const string CollectionsCount = "process.runtime.dotnet.gc.collections.count";
            public const string HeapSize = "process.runtime.dotnet.gc.heap.size";
            public const string LiveHeapSize = "process.runtime.dotnet.gc.heap.live";
            public const string AllocatedBytes = "process.runtime.dotnet.gc.allocations.size";
            public const string HeapCommittedMemory = "process.runtime.dotnet.gc.committed_memory.size";
            public const string PauseTime = "process.runtime.dotnet.gc.pause.time";
        }

        internal static class Process
        {
            public const string MemoryUsage = "process.memory.usage";
            public const string MemoryVirtual = "process.memory.virtual";
            public const string CpuTime = "process.cpu.time";
            public const string CpuUtilization = "process.cpu.utilization";
            public const string ThreadsCount = "process.threads";
        }
    }
}
