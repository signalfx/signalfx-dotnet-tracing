// Modified by SignalFx
using OpenTracing;

namespace SignalFx.Tracing.OpenTracing
{
    internal class OpenTracingScopeManager : global::OpenTracing.IScopeManager
    {
        internal OpenTracingScopeManager(IScopeManager scopeManager)
        {
            ScopeManager = scopeManager;
        }

        public IScope Active
        {
            get => ScopeManager?.Active == null ? null : new OpenTracingScope(ScopeManager.Active);
        }

        internal IScopeManager ScopeManager { get; }

        public IScope Activate(ISpan span, bool finishSpanOnDispose)
        {
            if (ScopeManager == null)
            {
                return null;
            }

            return new OpenTracingScope(ScopeManager.Activate(((OpenTracingSpan)span).Span, finishSpanOnDispose));
        }
    }
}
