// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Globalization;
using OpenTracing;
using OpenTracing.Tag;

namespace SignalFx.Tracing.OpenTracing
{
    internal class OpenTracingSpan : ISpan
    {
        internal OpenTracingSpan(Span span)
        {
            Span = span;
            Context = new OpenTracingSpanContext(span.Context);
        }

        public OpenTracingSpanContext Context { get; }

        global::OpenTracing.ISpanContext ISpan.Context => Context;

        internal Span Span { get; }

        internal string OperationName => Span.OperationName;

        internal TimeSpan Duration => Span.Duration;

        public string GetBaggageItem(string key) => null;

        public ISpan Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields)
        {
            Span.Log(timestamp, fields);
            return this;
        }

        public ISpan Log(string eventName)
        {
            var fields = new Dictionary<string, object>() { { Logs.Event, eventName } };
            Span.Log(fields);
            return this;
        }

        public ISpan Log(DateTimeOffset timestamp, string eventName)
        {
            var fields = new Dictionary<string, object>() { { Logs.Event, eventName } };
            Span.Log(timestamp, fields);
            return this;
        }

        public ISpan Log(IEnumerable<KeyValuePair<string, object>> fields)
        {
            Span.Log(fields);
            return this;
        }

        public ISpan SetBaggageItem(string key, string value) => this;

        public ISpan SetOperationName(string operationName)
        {
            Span.OperationName = operationName;
            return this;
        }

        public string GetTag(string key)
        {
            return Span.GetTag(key);
        }

        public ISpan SetTag(string key, bool value)
        {
            return SetTag(key, value.ToString());
        }

        public ISpan SetTag(string key, double value)
        {
            return SetTag(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public ISpan SetTag(string key, int value)
        {
            return SetTag(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public ISpan SetTag(BooleanTag tag, bool value)
        {
            return SetTag(tag.Key, value);
        }

        public ISpan SetTag(IntOrStringTag tag, string value)
        {
            return SetTag(tag.Key, value);
        }

        public ISpan SetTag(IntTag tag, int value)
        {
            return SetTag(tag.Key, value);
        }

        public ISpan SetTag(StringTag tag, string value)
        {
            return SetTag(tag.Key, value);
        }

        public ISpan SetTag(string key, string value)
        {
            // TODO:bertrand do we want this behavior on the Span object too ?

            switch (key)
            {
                case CustomTags.ResourceName:
                    Span.ResourceName = value;
                    return this;
                case CustomTags.SpanType:
                    Span.Type = value;
                    return this;
                case CustomTags.ServiceName:
                    Span.ServiceName = value;
                    return this;
            }

            if (key == global::OpenTracing.Tag.Tags.Error.Key)
            {
                Span.Error = value == bool.TrueString;
                return this;
            }

            Span.SetTag(key, value);
            return this;
        }

        public void Finish()
        {
            Span.Finish();
        }

        public void Finish(DateTimeOffset finishTimestamp)
        {
            Span.Finish(finishTimestamp);
        }
    }
}
