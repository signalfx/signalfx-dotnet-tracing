// Modified by Splunk Inc.

using System.Collections.Generic;

namespace Datadog.Trace.AlwaysOnProfiler
{
    internal interface IThreadSampleExporter
    {
        void ExportThreadSamples(List<ThreadSample> threadSamples);
    }
}
