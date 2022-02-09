// Modified by Splunk Inc.

using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
#if NETFRAMEWORK
using System.Reflection;
#endif
using Datadog.Trace.Configuration;
using Datadog.Trace.Configuration.Helpers;
#if NETFRAMEWORK
using Datadog.Trace.Logging;
using Moq;
#endif
using Xunit;

namespace Datadog.Trace.Tests.Configuration
{
    public class SafeReadHelperTests
    {
        [Theory]
        [InlineData("SIGNALFX_FOO_URI", "http://www.temp.org", "http://www.temp.org", false)]
        [InlineData("SIGNALFX_FOO_URI", "www.temp.org", "http://www.temp.org", true)]
        [InlineData("SIGNALFX_FOO_URI", "temp.org", "http://www.temp.org", true)]
        [InlineData("SIGNALFX_FOO_URI", "http//invalid.org", "http://www.temp.org", true)]
        [InlineData("SIGNALFX_FOO_URI", "", "http://www.temp.org", false)]
        [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "Supporting Test Exploit")]
        public void SafeReadUri(string settingName, string settingValue, string expected, bool log)
        {
#if NETFRAMEWORK
            bool wasLogged = false;

            SetupLogger(settingName, value => wasLogged = value);
#endif

            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { settingName, settingValue }
            });

            Uri value = source.SafeReadUri(settingName, new Uri("http://www.temp.org", UriKind.Absolute), out _);

            Assert.Equal(new Uri(expected, UriKind.Absolute), value);

#if NETFRAMEWORK
            Assert.Equal(log, wasLogged);
#endif
        }

        [Theory]
        [InlineData("SIGNALFX_FOO_VALUE", "1", 1, 0, false)]
        [InlineData("SIGNALFX_FOO_VALUE", "-1", 1, 0, true)]
        [InlineData("SIGNALFX_FOO_VALUE", "abc", 1, 0, false)]
        [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "Supporting Test Exploit")]
        public void SafeReadInt32(string settingName, string settingValue, int expected, int condition, bool log)
        {
#if NETFRAMEWORK
            bool wasLogged = false;

            SetupLogger(settingName, value => wasLogged = value);
#endif

            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { settingName, settingValue }
            });

            // using foo validator setting > condition
            int value = source.SafeReadInt32(settingName, expected, (val) => val > condition);

            Assert.Equal(expected, value);

#if NETFRAMEWORK
            Assert.Equal(log, wasLogged);
#endif
        }

#if NETFRAMEWORK
        // This test is exploiting .NET Framework issue, where static readonly variables could be overwritten using reflection.
        // It is assumed that the NET FX test result is reflecting the same result for NET Core.
        private void SetupLogger(string key, Action<bool> wasCalled)
        {
            var log = typeof(SafeReadHelper).GetField("Log", BindingFlags.NonPublic | BindingFlags.Static);
            var mock = new Mock<IDatadogLogger>();

            mock.Setup(x => x.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Callback<Exception, string, int, string>((ex, message, line, file) =>
                {
                    if (message.Contains(key))
                    {
                        wasCalled(true);
                    }
                });

            mock.Setup(x => x.Error(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                .Callback<string, int, string>((message, line, file) =>
                {
                    if (message.Contains(key))
                    {
                        wasCalled(true);
                    }
                });

            log.SetValue(null, mock.Object);
        }
#endif
    }
}
