// Modified by SignalFx
using System;
using System.Collections.Generic;

namespace Datadog.Trace.Interfaces
{
    internal interface ISpan
    {
        string ResourceName { get; set; }

        string Type { get; set; }

        ISpan SetTag(string key, string value);

        string GetTag(string key);

        ISpan Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields);

        void SetException(Exception exception);
    }
}
