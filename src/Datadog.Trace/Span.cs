// Modified by SignalFx
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SignalFx.Tracing.Abstractions;
using SignalFx.Tracing.ExtensionMethods;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Vendors.Serilog.Events;

namespace SignalFx.Tracing
{
    /// <summary>
    /// A Span represents a logical unit of work in the system. It may be
    /// related to other spans by parent/children relationships. The span
    /// tracks the duration of an operation as well as associated metadata in
    /// the form of a resource name, a service name, and user defined tags.
    /// </summary>
    public class Span : IDisposable, ISpan
    {
        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Logger = SignalFxLogging.For<Span>();
        private static readonly bool IsLogLevelDebugEnabled = Logger.IsEnabled(LogEventLevel.Debug);

        private readonly object _lock = new object();
        private readonly object _tagsLock = new object();
        private readonly object _metricLock = new object();

        internal Span(SpanContext context, DateTimeOffset? start)
        {
            Context = context;
            Context.Span = this;
            ServiceName = context.ServiceName;
            StartTime = start ?? Context.TraceContext.UtcNow;

            Logger.Debug(
                "Span started: [s_id: {0}, p_id: {1}, t_id: {2}]",
                SpanId,
                Context.ParentId,
                TraceId);
        }

        /// <summary>
        /// Gets or sets operation name
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Gets or sets the resource name
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets the type of request this span represents (ex: web, db).
        /// Not to be confused with span kind.
        /// </summary>
        /// <seealso cref="SpanTypes"/>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this span represents an error
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string ServiceName
        {
            get => Context.ServiceName;
            set => Context.ServiceName = value;
        }

        /// <summary>
        /// Gets the trace's unique identifier.
        /// </summary>
        public ulong TraceId => Context.TraceId;

        /// <summary>
        /// Gets the span's unique identifier.
        /// </summary>
        public ulong SpanId => Context.SpanId;

        internal SpanContext Context { get; }

        internal DateTimeOffset StartTime { get; }

        internal TimeSpan Duration { get; private set; }

        internal Dictionary<string, string> Tags { get; private set; }

        internal Dictionary<string, double> Metrics { get; private set; }

        internal ConcurrentDictionary<DateTimeOffset, Dictionary<string, string>> Logs { get; } = new ConcurrentDictionary<DateTimeOffset, Dictionary<string, string>>();

        internal bool IsFinished { get; private set; }

        internal bool IsRootSpan => Context?.TraceContext?.RootSpan == this;

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"TraceId: {Context.TraceId}");
            sb.AppendLine($"ParentId: {Context.ParentId}");
            sb.AppendLine($"SpanId: {Context.SpanId}");
            sb.AppendLine($"ServiceName: {ServiceName}");
            sb.AppendLine($"OperationName: {OperationName}");
            sb.AppendLine($"Resource: {ResourceName}");
            sb.AppendLine($"Type: {Type}");
            sb.AppendLine($"Start: {StartTime}");
            sb.AppendLine($"Duration: {Duration}");
            sb.AppendLine($"Error: {Error}");
            sb.AppendLine("Meta:");

            if (Tags?.Count > 0)
            {
                // lock because we're iterating the collection, not reading a single value
                lock (_tagsLock)
                {
                    foreach (var kv in Tags)
                    {
                        sb.Append($"\t{kv.Key}:{kv.Value}");
                    }
                }
            }

            sb.AppendLine("Logs:");
            foreach (var e in Logs)
            {
                sb.Append($"\t{e.Key}:\n");
                foreach (var kv in e.Value)
                {
                    sb.Append($"\t\t{kv.Key}:{kv.Value}\n");
                }
            }

            sb.AppendLine("Metrics:");

            if (Metrics?.Count > 0)
            {
                // lock because we're iterating the collection, not reading a single value
                lock (_metricLock)
                {
                    foreach (var kv in Metrics)
                    {
                        sb.Append($"\t{kv.Key}:{kv.Value}");
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Add a the specified tag to this span.
        /// </summary>
        /// <param name="key">The tag's key.</param>
        /// <param name="value">The tag's value.</param>
        /// <returns>This span to allow method chaining.</returns>
        public Span SetTag(string key, string value)
        {
            if (IsFinished)
            {
                Logger.Debug("SetTag should not be called after the span was closed");
                return this;
            }

            // some tags have special meaning
            switch (key)
            {
                case Tracing.Tags.SamplingPriority:
                    if (Enum.TryParse(value, out SamplingPriority samplingPriority) &&
                        Enum.IsDefined(typeof(SamplingPriority), samplingPriority))
                    {
                        // allow setting the sampling priority via a tag
                        Context.TraceContext.SamplingPriority = samplingPriority;
                    }

                    break;
#pragma warning disable CS0618 // Type or member is obsolete
                case Tracing.Tags.ForceKeep:
                case Tracing.Tags.ManualKeep:
                    if (value.ToBoolean() ?? false)
                    {
                        // user-friendly tag to set UserKeep priority
                        Context.TraceContext.SamplingPriority = SamplingPriority.UserKeep;
                    }

                    break;
                case Tracing.Tags.ForceDrop:
                case Tracing.Tags.ManualDrop:
                    if (value.ToBoolean() ?? false)
                    {
                        // user-friendly tag to set UserReject priority
                        Context.TraceContext.SamplingPriority = SamplingPriority.UserReject;
                    }

                    break;
#pragma warning restore CS0618 // Type or member is obsolete
                case Tracing.Tags.Analytics:
                    // value is a string and can represent a bool ("true") or a double ("0.5"),
                    // so try to parse both.
                    // note that "1" and "0" can parse as either type,
                    // but they mean the same thing in this case, so it's fine.
                    bool? boolean = value.ToBoolean();

                    if (boolean == true)
                    {
                        // always sample
                        SetMetric(Tracing.Tags.Analytics, 1.0);
                    }
                    else if (boolean == false)
                    {
                        // never sample
                        SetMetric(Tracing.Tags.Analytics, 0.0);
                    }
                    else if (double.TryParse(
                        value,
                        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                        CultureInfo.InvariantCulture,
                        out double analyticsSampleRate))
                    {
                        // use specified sample rate
                        SetMetric(Tracing.Tags.Analytics, analyticsSampleRate);
                    }
                    else
                    {
                        Logger.Warning("Value {0} has incorrect format for tag {1}", value, Tracing.Tags.Analytics);
                    }

                    break;
                default:
                    if (value == null)
                    {
                        if (Tags != null)
                        {
                            // lock when modifying the collection
                            lock (_tagsLock)
                            {
                                // Agent doesn't accept null tag values,
                                // remove them instead
                                Tags.Remove(key);
                            }
                        }
                    }
                    else
                    {
                        // lock when modifying the collection
                        lock (_tagsLock)
                        {
                            if (Tags == null)
                            {
                                // defer instantiation until needed to
                                // avoid unnecessary allocations per span
                                Tags = new Dictionary<string, string>();
                            }

                            // if not a special tag, just add it to the tag bag
                            Tags[key] = value;
                        }
                    }

                    break;
            }

            return this;
        }

        /// <summary>
        /// Add the specified log fields to this span.
        /// </summary>
        /// <param name="fields">The logged fields.</param>
        /// <returns>This span to allow method chaining.</returns>
        public Span Log(IEnumerable<KeyValuePair<string, object>> fields)
        {
            return Log(Context.TraceContext.UtcNow, fields);
        }

        /// <summary>
        /// Add the specified log field objects to this span.
        /// </summary>
        /// <param name="timestamp">The event timestamp.</param>
        /// <param name="fields">The logged fields.</param>
        /// <returns>This span to allow method chaining.</returns>
        public Span Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields)
        {
            if (IsFinished)
            {
                Logger.Debug("Log should not be called after the span was closed");
                return this;
            }

            if (fields == null)
            {
                return this;
            }

            var loggedFields = fields.ToDictionary(x => x.Key, x => x.Value.ToString());
            Logs[timestamp] = loggedFields;
            return this;
        }

        /// <summary>
        /// Add the specified event to this span.
        /// </summary>
        /// <param name="eventName">The key to log as an event field name.</param>
        /// <param name="value">The value to log as an event field value.</param>
        /// <returns>This span to allow method chaining.</returns>
        public Span Log(string eventName, object value)
        {
            return Log(new Dictionary<string, object>() { { eventName, value } });
        }

        /// <summary>
        /// Add the specified event to this span.
        /// </summary>
        /// <param name="value">The event to log as an "event" field.</param>
        /// <returns>This span to allow method chaining.</returns>
        public Span Log(object value)
        {
            return Log(global::SignalFx.Tracing.Logs.Event, value);
        }

        /// <summary>
        /// Add a the specified tag to this span.
        /// </summary>
        /// <param name="key">The tag's key.</param>
        /// <param name="value">The tag's value.</param>
        /// <returns>This span to allow method chaining.</returns>
        ISpan ISpan.SetTag(string key, string value)
            => SetTag(key, value);

        /// <summary>
        /// Add the specified log fields to this span.
        /// </summary>
        /// <param name="timestamp">The event's timestamp.</param>
        /// <param name="fields">The logged fields.</param>
        /// <returns>This span to allow method chaining.</returns>
        ISpan ISpan.Log(DateTimeOffset timestamp, IEnumerable<KeyValuePair<string, object>> fields)
            => Log(timestamp, fields);

        /// <summary>
        /// Record the end time of the span and flushes it to the backend.
        /// After the span has been finished all modifications will be ignored.
        /// </summary>
        public void Finish()
        {
            Finish(Context.TraceContext.UtcNow);
        }

        /// <summary>
        /// Explicitly set the end time of the span and flushes it to the backend.
        /// After the span has been finished all modifications will be ignored.
        /// </summary>
        /// <param name="finishTimestamp">Explicit value for the end time of the Span</param>
        public void Finish(DateTimeOffset finishTimestamp)
        {
            var shouldCloseSpan = false;
            lock (_lock)
            {
                ResourceName ??= OperationName;

                if (!IsFinished)
                {
                    Duration = finishTimestamp - StartTime;
                    if (Duration < TimeSpan.Zero)
                    {
                        Duration = TimeSpan.Zero;
                    }

                    IsFinished = true;
                    shouldCloseSpan = true;
                }
            }

            if (shouldCloseSpan)
            {
                Context.TraceContext.CloseSpan(this);

                if (IsLogLevelDebugEnabled)
                {
                    Logger.Debug(
                        "Span closed: [s_id: {0}, p_id: {1}, t_id: {2}] for (Service: {3}, Resource: {4}, Operation: {5}, Tags: [{6}])",
                        SpanId,
                        Context.ParentId,
                        TraceId,
                        ServiceName,
                        ResourceName,
                        OperationName,
                        Tags == null ? string.Empty : string.Join(",", Tags.Keys));
                }
            }
        }

        /// <summary>
        /// Record the end time of the span and flushes it to the backend.
        /// After the span has been finished all modifications will be ignored.
        /// </summary>
        public void Dispose()
        {
            Finish();
        }

        /// <summary>
        /// Add the StackTrace and other exception metadata to the span
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void SetException(Exception exception)
        {
            Error = true;

            if (exception != null)
            {
                // for AggregateException, use the first inner exception until we can support multiple errors.
                // there will be only one error in most cases, and even if there are more and we lose
                // the other ones, it's still better than the generic "one or more errors occurred" message.
                if (exception is AggregateException aggregateException && aggregateException.InnerExceptions.Count > 0)
                {
                    exception = aggregateException.InnerExceptions[0];
                }

                SetTag(Tracing.Tags.ErrorMsg, exception.Message);
                SetTag(Tracing.Tags.ErrorStack, exception.ToString());
                SetTag(Tracing.Tags.ErrorKind, exception.GetType().ToString());
            }
        }

        /// <summary>
        /// Gets the value (or default/null if the key is not a valid tag) of a tag with the key value passed
        /// </summary>
        /// <param name="key">The tag's key</param>
        /// <returns> The value for the tag with the key specified, or null if the tag does not exist</returns>
        public string GetTag(string key)
        {
            // no need to lock on single reads
            return Tags != null && Tags.TryGetValue(key, out var value) ? value : null;
        }

        internal bool SetExceptionForFilter(Exception exception)
        {
            SetException(exception);
            return false;
        }

        internal double? GetMetric(string key)
        {
            // no need to lock on single reads
            return Metrics != null && Metrics.TryGetValue(key, out double value) ? value : default;
        }

        internal Span SetMetric(string key, double? value)
        {
            if (value == null)
            {
                if (Metrics != null)
                {
                    // lock when modifying the collection
                    lock (_metricLock)
                    {
                        Metrics.Remove(key);
                    }
                }
            }
            else
            {
                // lock when modifying the collection
                lock (_metricLock)
                {
                    if (Metrics == null)
                    {
                        // defer instantiation until needed to
                        // avoid unnecessary allocations per span
                        Metrics = new Dictionary<string, double>();
                    }

                    Metrics[key] = value.Value;
                }
            }

            return this;
        }
    }
}
