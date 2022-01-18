using System.Threading.Tasks;
using OpenTracing;
using OpenTracing.Util;

namespace Samples.OpenTracing
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var tracer = GlobalTracer.Instance;
            
            using var scope = tracer.BuildSpan("MyTracedFunctionality").StartActive(finishSpanOnDispose: true);
            var span = scope.Span;
            span.SetTag("MyImportantTag", "MyImportantValue");
            span.Log("My Important Log Statement");

            var ret = await MyAppFunctionality(tracer);

            span.SetTag("FunctionalityReturned", ret.ToString());
            span.Finish();
        }

        private static async Task<bool> MyAppFunctionality(ITracer tracer)
        {
            using var scope = tracer.BuildSpan("NestedSpan").StartActive(finishSpanOnDispose: true);
            var span = scope.Span;

            await Task.Delay(10);

            span.SetTag("InnerSpanTag", "ImportantValue");
            span.Finish();

            return true;
        }
    }
}
