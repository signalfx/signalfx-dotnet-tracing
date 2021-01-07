// Modified by SignalFx
using OpenTracing;

namespace SignalFx.Tracing.OpenTracing
{
    internal class OpenTracingScope : IScope
    {
        internal OpenTracingScope(Scope scope)
        {
            Scope = scope;
        }

        public ISpan Span
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
