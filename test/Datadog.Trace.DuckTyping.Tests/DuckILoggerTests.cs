#pragma warning disable SA1201 // Elements must appear in the correct order

#if !NETFRAMEWORK

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignalFx.Tracing.DuckTyping;
using Xunit;

namespace SignalFx.Tracing.Tests
{
    public class DuckILoggerTests
    {
        [Fact]
        public void CanCallBeginScope()
        {
            var webHost = typeof(WebHostOptions).Assembly.GetType("Microsoft.AspNetCore.Hosting.Internal.WebHost");
            var hostingApplication = typeof(DuckILoggerTests)
                                    .GetMethod(nameof(CreateHostingApplication))
                                    ?.MakeGenericMethod(webHost)
                                    .Invoke(this, null);

            var proxy = hostingApplication.DuckCast<IHostingApplication>();

            proxy.Should().NotBeNull();

            var logger = proxy.Diagnostics.Logger;
            logger.Should().NotBeNull();
            var disposable = logger.BeginScope(new DatadogLoggingScope());
            disposable.Should().NotBeNull();
        }

        public object CreateHostingApplication<TWebHost>()
        {
            return new HostingApplication(
                application: context => Task.CompletedTask,
                logger: new Logger<TWebHost>(new LoggerFactory()),
                new DiagnosticListener("ILoggerDuckTypingTests"),
                httpContextFactory: new HttpContextFactory(Options.Create(new FormOptions())));
        }

        public class DatadogLoggingScope
        {
        }

        public interface IHostingApplication
        {
            [Duck(Name = "_diagnostics", Kind = DuckKind.Field)]
            IHostingApplicationDiagnostics Diagnostics { get; }
        }

        public interface IHostingApplicationDiagnostics
        {
            [Duck(Name = "_logger", Kind = DuckKind.Field)]
            ILogger Logger { get; }
        }

        public interface ILogger
        {
            [Duck(ExplicitInterfaceTypeName = "*")]
            IDisposable BeginScope<TState>(TState state);
        }
    }
}
#endif
