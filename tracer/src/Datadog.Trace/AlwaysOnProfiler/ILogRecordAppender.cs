using System.Collections.Generic;
using Datadog.Tracer.OpenTelemetry.Proto.Logs.V1;

namespace Datadog.Trace.AlwaysOnProfiler;

internal interface ILogRecordAppender
{
    /// <summary>
    /// Processes samples captured on the native side and
    /// appends results to the provided list.
    /// </summary>
    /// <param name="results">The collection to add records to.</param>
    void AppendTo(List<LogRecord> results);
}
