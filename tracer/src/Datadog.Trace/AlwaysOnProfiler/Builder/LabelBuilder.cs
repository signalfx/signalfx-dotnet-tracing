// Modified by Splunk Inc.

using Datadog.Tracer.Pprof.Proto.Profile.V1;

namespace Datadog.Trace.AlwaysOnProfiler.Builder
{
    internal class LabelBuilder
    {
        private readonly Label _label = new();

        public LabelBuilder SetKey(long key)
        {
            _label.Key = key;
            return this;
        }

        public LabelBuilder SetStr(long valueIndex)
        {
            _label.Str = valueIndex;
            return this;
        }

        public LabelBuilder SetNum(long num)
        {
            _label.Num = num;
            return this;
        }

        public Label Build() => _label;
    }
}
