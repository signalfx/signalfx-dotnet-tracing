// Modified by Splunk Inc.

using System.Linq;
using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace.Tagging
{
    internal partial class WcfTags : WebTags
    {
        [Tag(Trace.Tags.InstrumentationName)]
        public string InstrumentationName => "Wcf";
    }
}
