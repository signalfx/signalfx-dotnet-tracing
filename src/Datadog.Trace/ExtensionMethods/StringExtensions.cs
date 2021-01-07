using System;
using System.Text.RegularExpressions;

namespace SignalFx.Tracing.ExtensionMethods
{
    internal static class StringExtensions
    {
        private static string hex = "(0x[0-9a-fA-F]+)";

        private static string numericLiteral = @"((?<![\p{L}\p{C}_0-9])[+.-]*[0-9]+[0-9xEe.+-]*)";

        private static string singleQuoted = "('((?:''|[^'])*)')";

        private static string doubleQuoted = "(\"((?:\"\"|[^\"])*)\")";

        private static string replacement = "?";

        private static string sqlNormalizeString = string.Join("|", new string[] { hex, numericLiteral, singleQuoted, doubleQuoted });

        private static Regex sqlNormalizePattern = new Regex(sqlNormalizeString, RegexOptions.Compiled);

        /// <summary>
        /// Removes the trailing occurrence of a substring from the current string.
        /// </summary>
        /// <param name="value">The original string.</param>
        /// <param name="suffix">The string to remove from the end of <paramref name="value"/>.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how this string and <paramref name="suffix"/> are compared.</param>
        /// <returns>A new string with <paramref name="suffix"/> removed from the end, if found. Otherwise, <paramref name="value"/>.</returns>
        public static string TrimEnd(this string value, string suffix, StringComparison comparisonType)
        {
            if (value == null) { throw new ArgumentNullException(nameof(value)); }

            return !string.IsNullOrEmpty(suffix) && value.EndsWith(suffix, comparisonType)
                       ? value.Substring(0, value.Length - suffix.Length)
                       : value;
        }

        /// <summary>
        /// Converts a <see cref="string"/> into a <see cref="bool"/> by comparing it to commonly used values
        /// such as "True", "yes", or "1". Case-insensitive. Defaults to <c>false</c> if string is not recognized.
        /// </summary>
        /// <param name="value">The string to convert.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is one of the accepted values for <c>true</c>; <c>false</c> otherwise.</returns>
        public static bool? ToBoolean(this string value)
        {
            if (value == null) { throw new ArgumentNullException(nameof(value)); }

            switch (value.ToUpperInvariant())
            {
                case "TRUE":
                case "YES":
                case "T":
                case "1":
                    return true;
                case "FALSE":
                case "NO":
                case "F":
                case "0":
                    return false;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Truncates a <see cref="string"/> to the given maximum length.
        /// </summary>
        /// <param name="value">The string to truncate.</param>
        /// <param name="maxLength">The maximum length of the truncated string.</param>
        /// <exception cref="ArgumentOutOfRangeException">Throw if <paramref name="maxLength"/> is negative.</exception>
        /// <returns>The truncated string or the original string if its length is less than or equal to <paramref name="maxLength"/>.</returns>
        public static string Truncate(this string value, int maxLength)
        {
            if (value == null) { throw new ArgumentNullException(nameof(value)); }

            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        /// <summary>
        /// Sanitize all values from sql statements.
        /// </summary>
        /// <param name="value">The string to sanitize.</param>
        /// <returns>The santized string</returns>
        public static string SanitizeSqlStatement(this string value)
        {
            if (value == null) { throw new ArgumentNullException(nameof(value)); }

            return sqlNormalizePattern.Replace(value, replacement);
        }
    }
}
