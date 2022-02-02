// Modified by Splunk Inc.

using System;
using System.Collections.Specialized;
using System.Reflection;
using Datadog.Trace.Configuration;
using Datadog.Trace.Configuration.Helpers;
using Datadog.Trace.Logging;
using Moq;
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
        public void SafeReadUri(string settingName, string settingValue, string expected, bool log)
        {
            bool wasLogged = false;

            SetupLogger(settingName, value => wasLogged = value);

            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { settingName, settingValue }
            });

            Uri value = source.SafeReadUri(settingName, new Uri("http://www.temp.org", UriKind.Absolute));

            Assert.Equal(new Uri(expected, UriKind.Absolute), value);
            Assert.Equal(log, wasLogged);
        }

        [Theory]
        [InlineData("SIGNALFX_FOO_VALUE", "1", 1, 0, false)]
        [InlineData("SIGNALFX_FOO_VALUE", "-1", 1, 0, true)]
        [InlineData("SIGNALFX_FOO_VALUE", "abc", 1, 0, false)]
        public void SafeReadInt32(string settingName, string settingValue, int expected, int condition, bool log)
        {
            bool wasLogged = false;

            SetupLogger(settingName, value => wasLogged = value);

            var source = new NameValueConfigurationSource(new NameValueCollection
            {
                { settingName, settingValue }
            });

            // using foo validator setting > condition
            int value = source.SafeReadInt32(settingName, expected, (val) => val > condition);

            Assert.Equal(expected, value);
            Assert.Equal(log, wasLogged);
        }

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
    }
}
