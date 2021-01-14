// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Linq;
using SignalFx.Tracing;
using SignalFx.Tracing.Abstractions;

namespace Datadog.Trace.TestHelpers
{
    public class TestSpan : ISpan
    {
        public string ResourceName { get; set; }

        public string Type { get; set; }

        public bool Error { get; set; }

        public Dictionary<DateTimeOffset, Dictionary<string, string>> Logs { get; } = new Dictionary<DateTimeOffset, Dictionary<string, string>>();

        private Dictionary<string, string> Tags { get; } = new Dictionary<string, string>();

        ISpan ISpan.SetTag(string key, string value)
        {
            SetTagInternal(key, value);

            return this;
        }

        ISpan ISpan.Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields)
        {
            Dictionary<string, string> loggedFields = fields.ToDictionary(x => x.Key, x => x.Value.ToString());
            Logs[timestamp] = loggedFields;
            return this;
        }

        public string GetTag(string key)
            => Tags.TryGetValue(key, out var tagValue)
                   ? tagValue
                   : null;

        public void SetException(Exception exception)
        {
            Error = true;

            SetTagInternal(SignalFx.Tracing.Tags.ErrorMsg, exception.Message);
            SetTagInternal(SignalFx.Tracing.Tags.ErrorStack, exception.StackTrace);
            SetTagInternal(SignalFx.Tracing.Tags.ErrorKind, exception.GetType().ToString());
        }

        private void SetTagInternal(string key, string value)
        {
            if (value == null)
            {
                Tags.Remove(key);
            }
            else
            {
                Tags[key] = value;
            }
        }
    }
}
