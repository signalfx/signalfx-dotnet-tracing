// Modified by Splunk Inc.

using System.Linq;
using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.Tagging
{
    internal class WcfTags : WebTags
    {
        protected static readonly IProperty<string>[] WcfTagsProperties =
            WebTagsProperties.Concat(
                new ReadOnlyProperty<WcfTags, string>(Tags.InstrumentationName, t => t.InstrumentationName));

        public string InstrumentationName => "Wcf";

        protected override IProperty<string>[] GetAdditionalTags() => WcfTagsProperties;
    }
}
