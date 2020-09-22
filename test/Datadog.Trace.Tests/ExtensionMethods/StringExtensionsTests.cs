// Modified by SignalFx
using System;
using System.Collections;
using System.Linq;
using Datadog.Trace.ExtensionMethods;
using Xunit;

namespace Datadog.Trace.Tests.ExtensionMethods
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("NameSuffix", "Suffix", "Name")]
        [InlineData("Name", "Suffix", "Name")]
        [InlineData("Suffix", "Suffix", "")]
        [InlineData("NameSuffix", "Name", "NameSuffix")]
        [InlineData("Name", "", "Name")]
        [InlineData("Name", null, "Name")]
        [InlineData("", "Name", "")]
        [InlineData("", "", "")]
        public void TrimEnd(string original, string suffix, string expected)
        {
            string actual = original.TrimEnd(suffix, StringComparison.Ordinal);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD=1234", "SELECT * FROM TABLE WHERE FIELD=?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = 1234", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD>=-1234", "SELECT * FROM TABLE WHERE FIELD>=?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD<-1234", "SELECT * FROM TABLE WHERE FIELD<?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD <.1234", "SELECT * FROM TABLE WHERE FIELD <?")]
        [InlineData("SELECT 1.2", "SELECT ?")]
        [InlineData("SELECT -1.2", "SELECT ?")]
        [InlineData("SELECT -1.2e-9", "SELECT ?")]
        [InlineData("SELECT 2E+9", "SELECT ?")]
        [InlineData("SELECT +0.2", "SELECT ?")]
        [InlineData("SELECT .2", "SELECT ?")]
        [InlineData("7", "?")]
        [InlineData(".7", "?")]
        [InlineData("-7", "?")]
        [InlineData("+7", "?")]
        [InlineData("SELECT 0x0af764", "SELECT ?")]
        [InlineData("SELECT 0xdeadbeef", "SELECT ?")]
        [InlineData("SELECT A + B", "SELECT A + B")]
        [InlineData("SELECT -- comment", "SELECT -- comment")]
        [InlineData("SELECT * FROM TABLE123", "SELECT * FROM TABLE123")]
        [InlineData("SELECT FIELD2 FROM TABLE_123 WHERE X<>7", "SELECT FIELD2 FROM TABLE_123 WHERE X<>?")]
        [InlineData("SELECT --83--...--8e+76e3E-1", "SELECT ?")]
        [InlineData("SELECT DEADBEEF", "SELECT DEADBEEF")]
        [InlineData("SELECT 123-45-6789", "SELECT ?")]
        [InlineData("SELECT 1/2/34", "SELECT ?/?/?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = ''", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = 'words and spaces'", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = ' an escaped '' quote mark inside'", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = '\\\\'", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = '\"inside doubles\"'", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = '\"\"'", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = 'a single \" doublequote inside'", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = \"\"", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = \"words and spaces'\"", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = \" an escaped \"\" quote mark inside\"", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = \"\\\\\"", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = \"'inside singles'\"", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = \"''\"", "SELECT * FROM TABLE WHERE FIELD = ?")]
        [InlineData("SELECT * FROM TABLE WHERE FIELD = \"a single ' singlequote inside\"", "SELECT * FROM TABLE WHERE FIELD = ?")]
        public void SqlStatementsAreSanitized(string original, string expected)
        {
            string actual = original.SanitizeSqlStatement();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TruncateExtension()
        {
            var a = string.Concat(Enumerable.Repeat("0", 10000));
            Assert.Equal(1024, a.Truncate(1024).Length);

            var b = "Preserved string";
            Assert.Same(b, b.Truncate(b.Length));

            var c = "Replaced by empty";
            Assert.Same(string.Empty, c.Truncate(0));

            Assert.Throws<ArgumentOutOfRangeException>(() => c.Truncate(-123));
        }
    }
}
