// Modified by SignalFx
using OpenTracing;

namespace Datadog.Trace.OpenTracing
{
    internal class OpenTracingScopeManager : global::OpenTracing.IScopeManager
    {
        internal OpenTracingScopeManager(IScopeManager scopeManager)
        {
            ScopeManager = scopeManager;
        }

        internal IScopeManager ScopeManager { get; }

        public global::OpenTracing.IScope Active
        {
            get => ScopeManager?.Active == null ? null : new OpenTracingScope(ScopeManager.Active);
        }

        public global::OpenTracing.IScope Activate(global::OpenTracing.ISpan span, bool finishSpanOnDispose)
        {
            if (ScopeManager == null)
            {
                return null;
            }

            return new OpenTracingScope(ScopeManager.Activate(((OpenTracingSpan)span).Span, finishSpanOnDispose));
        }
    }
}
