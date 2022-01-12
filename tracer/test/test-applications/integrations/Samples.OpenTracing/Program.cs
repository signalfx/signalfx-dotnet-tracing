using System.Threading.Tasks;
using OpenTracing.Util;

namespace Samples.OpenTracing
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
            var tracer = GlobalTracer.Instance;

            // Create an active span that will be automatically parented by any existing span in this context
            using var scope = tracer.BuildSpan("MyTracedFunctionality").StartActive(finishSpanOnDispose: true);
            var span = scope.Span;
            span.SetTag("MyImportantTag", "MyImportantValue");
            span.Log("My Important Log Statement");

            var ret = await MyAppFunctionality();

            span.SetTag("FunctionalityReturned", ret.ToString());
            span.Finish();
        }

        private static async Task<bool> MyAppFunctionality()
        {
            await Task.Delay(10);
            return true;
        }
    }
}
