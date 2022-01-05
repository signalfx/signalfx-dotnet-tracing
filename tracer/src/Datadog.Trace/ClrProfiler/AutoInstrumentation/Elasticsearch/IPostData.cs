// Modified by Splunk Inc.

using System.IO;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Elasticsearch;

internal interface IPostData
{
    public byte[] WrittenBytes { get; }

    void Write(Stream stream, object valueConnectionSettings);
}
