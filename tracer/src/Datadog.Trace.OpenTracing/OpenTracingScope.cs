// Modified by SignalFx

namespace Datadog.Trace.OpenTracing
{
    internal class OpenTracingScope : global::OpenTracing.IScope
    {
        internal OpenTracingScope(Scope scope)
        {
            Scope = scope;
        }

        public global::OpenTracing.ISpan Span
        {
            get => Scope?.Span == null ? null : new OpenTracingSpan(Scope.Span);
        }

        internal Scope Scope { get; }

        public void Dispose()
        {
            Scope?.Dispose();
        }
    }
}
