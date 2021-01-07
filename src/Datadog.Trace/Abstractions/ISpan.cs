// Modified by SignalFx
using System;
using System.Collections.Generic;

namespace SignalFx.Tracing.Abstractions
{
    internal interface ISpan
    {
        string ResourceName { get; set; }

        string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this span represents an error.
        /// </summary>
        bool Error { get; set; }

        ISpan SetTag(string key, string value);

        string GetTag(string key);

        ISpan Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields);

        void SetException(Exception exception);
    }
}
