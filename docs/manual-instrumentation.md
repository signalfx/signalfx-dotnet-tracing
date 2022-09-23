# Manually instrument a .NET application

You can build upon the provided tracing functionality by modifying and adding
to automatically generated traces. The OpenTelemetry Tracing library for .NET
provides and registers an [OpenTracing-compatible](https://github.com/opentracing/opentracing-csharp)
global tracer you can use.

OpenTracing version 0.12.1 is supported and the provided tracer offers a
complete implementation of the OpenTracing API.

The auto-instrumentation provides a base you can build on by adding your own
custom instrumentation. By using both instrumentation approaches, you'll be
able to present a more detailed representation of the logic and functionality
of your application, clients, and framework.

1. Add the OpenTracing dependency to your project:

    ```xml
    <PackageReference Include="OpenTracing" Version="0.12.1" />
    ```

1. Obtain the `OpenTracing.Util.GlobalTracer` instance and create spans that
automatically become child spans of any existing spans in the same context:

    ```csharp
    using OpenTracing;
    using OpenTracing.Util;

    namespace MyProject
    {
        public class MyClass
        {
            public static async void MyMethod()
            {
                // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
                var tracer = GlobalTracer.Instance;

                // Create an active span that will be automatically parented by any existing span in this context
                using (IScope scope = tracer.BuildSpan("MyTracedFunctionality").StartActive(finishSpanOnDispose: true))
                {
                    var span = scope.Span;
                    span.SetTag("MyImportantTag", "MyImportantValue");
                    span.Log("My Important Log Statement");

                    var ret = await MyAppFunctionality();

                    span.SetTag("FunctionalityReturned", ret.ToString());
                }
            }
        }
    }
    ```

Further reading:

- [OpenTracing documentation](https://github.com/opentracing/opentracing-csharp)
- [OpenTracing terminology](https://github.com/opentracing/specification/blob/master/specification.md)
