// Modified by SignalFx
using System;
using System.Diagnostics;
using Datadog.Trace.TestHelpers;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public sealed class IisFixture : IDisposable
    {
        private Process _iisExpress;

        public MockZipkinCollector Agent { get; private set; }

        public int HttpPort { get; private set; }

        public void TryStartIis(TestHelper helper, bool addClientIp = false)
        {
            lock (this)
            {
                if (_iisExpress == null)
                {
                    var initialAgentPort = TcpPortProvider.GetOpenPort();
                    Agent = new MockZipkinCollector(initialAgentPort);

                    HttpPort = TcpPortProvider.GetOpenPort();

                    _iisExpress = helper.StartIISExpress(Agent.Port, HttpPort, addClientIp);
                }
            }
        }

        public void Dispose()
        {
            Agent?.Dispose();

            lock (this)
            {
                if (_iisExpress != null)
                {
                    try
                    {
                        if (!_iisExpress.HasExited)
                        {
                            // sending "Q" to standard input does not work because
                            // iisexpress is scanning console key press, so just kill it.
                            // maybe try this in the future:
                            // https://github.com/roryprimrose/Headless/blob/master/Headless.IntegrationTests/IisExpress.cs
                            _iisExpress.Kill();
                            _iisExpress.WaitForExit(8000);
                        }
                    }
                    catch
                    {
                        // in some circumstances the HasExited property throws, this means the process probably hasn't even started correctly
                    }

                    _iisExpress.Dispose();
                }
            }
        }
    }
}
