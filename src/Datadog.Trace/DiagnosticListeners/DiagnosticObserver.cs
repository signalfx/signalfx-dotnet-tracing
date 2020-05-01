// Modified by SignalFx
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Datadog.Trace.Logging;

namespace Datadog.Trace.DiagnosticListeners
{
    internal abstract class DiagnosticObserver : IObserver<KeyValuePair<string, object>>
    {
        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.For<DiagnosticObserver>();

        protected DiagnosticObserver(IDatadogTracer tracer)
        {
            Tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));

            ListenerNames = new List<string>();
            ListenerNames.Add(ListenerName);
            ListenerNames.AddRange(tracer.Settings.AdditionalDiagnosticListeners);
        }

        protected IDatadogTracer Tracer { get; }

        protected List<string> ListenerNames { get; }

        /// <summary>
        /// Gets the name of the <see cref="DiagnosticListener"/> that should be instrumented.
        /// </summary>
        /// <value>The name of the <see cref="DiagnosticListener"/> that should be instrumented.</value>
        protected abstract string ListenerName { get; }

        public virtual bool IsSubscriberEnabled()
        {
            return true;
        }

        public virtual IDisposable SubscribeIfMatch(DiagnosticListener diagnosticListener)
        {
            if (ListenerNames.Any(diagnosticListener.Name.Contains))
            {
                return diagnosticListener.Subscribe(this, IsEventEnabled);
            }
            else
            {
                Log.Debug($"{diagnosticListener.Name} not subscribing to {this}.");
            }

            return null;
        }

        void IObserver<KeyValuePair<string, object>>.OnCompleted()
        {
        }

        void IObserver<KeyValuePair<string, object>>.OnError(Exception error)
        {
        }

        void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                OnNext(value.Key, value.Value);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Event Exception: {0}", value.Key);
            }
        }

        protected virtual bool IsEventEnabled(string eventName)
        {
            return true;
        }

        protected abstract void OnNext(string eventName, object arg);
    }
}
