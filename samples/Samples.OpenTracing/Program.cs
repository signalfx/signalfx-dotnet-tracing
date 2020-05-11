// Modified by SignalFx
using OpenTracing;
using OpenTracing.Util;

namespace Samples.OpenTracing
{
    public static class Program
    {
        private static ITracer tracer = GlobalTracer.Instance;

        public static void Main()
        {
            using (IScope scope = tracer.BuildSpan("MySpan").StartActive(finishSpanOnDispose: true))
            {
                var span = scope.Span;
                span.SetTag("MyTag", "MyValue");
                span.Log("My Log Statement");
            }
        }
    }
}
