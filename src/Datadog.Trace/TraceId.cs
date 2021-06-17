// Modified by SignalFx

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SignalFx.Tracing.Logging;

namespace SignalFx.Tracing
{
    /// <summary>
    /// Class representing 128 bit TraceID (parsed from and to hexadecimal string representation)
    /// </summary>
    public readonly struct TraceId : IEquatable<TraceId>
    {
        private static readonly Vendors.Serilog.ILogger Log = SignalFxLogging.For<TraceId>();
        private readonly string _string;

        private TraceId(ulong higher, ulong lower)
        {
            Higher = higher;
            Lower = lower;
            _string = $"{Higher:x16}{Lower:x16}";
        }

        private TraceId(ulong lower)
        {
            Higher = 0;
            Lower = lower;
            _string = $"{Higher:x16}{Lower:x16}";
        }

        /// <summary>
        /// Gets TraceId with zero id.
        /// </summary>
        public static TraceId Zero => new TraceId(lower: 0);

        /// <summary>
        /// Gets higher 64 bits of 128 bit traceID.
        /// </summary>
        public ulong Higher { get; }

        /// <summary>
        /// Gets lower 64 bits of 128 bit traceID.
        /// </summary>
        public ulong Lower { get; }

        /// <summary>
        /// Indicates if two specified instances of TraceId are not equal.
        /// </summary>
        /// <param name="traceId1">First <see cref="TraceId"/></param>
        /// <param name="traceId2">Second <see cref="TraceId"/></param>
        /// <returns>True if instances are equal, false otherwise.</returns>
        public static bool operator ==(TraceId traceId1, TraceId traceId2)
        {
            return traceId1.Equals(traceId2);
        }

        /// <summary>
        /// Indicates if two specified instances of TraceId are not equal.
        /// </summary>
        /// <param name="traceId1">First <see cref="TraceId"/></param>
        /// <param name="traceId2">Second <see cref="TraceId"/></param>
        /// <returns>True if instances are not equal, false otherwise.</returns>
        public static bool operator !=(TraceId traceId1, TraceId traceId2)
        {
            return !(traceId1 == traceId2);
        }

        /// <summary>
        /// Creates random 128 bit traceId.
        /// </summary>
        /// <returns>Instance of randomly generated <see cref="TraceId"/>.</returns>
        public static TraceId CreateRandom()
        {
            var randomNumberGenerator = RandomNumberGenerator.Current;

            var higher = randomNumberGenerator.Next();
            var lower = randomNumberGenerator.Next();

            return new TraceId(higher, lower);
        }

        /// <summary>
        /// Creates traceId from given 16 or 32 sign string representing traceId in hexadecimal format.
        /// </summary>
        /// <param name="id">16 or 32 sign string ID to be parsed.</param>
        /// <returns>Instance of <see cref="TraceId"/> representing the same traceId as the passed string.</returns>
        public static TraceId CreateFromString(string id)
        {
            try
            {
                switch (id.Length)
                {
                    case 16:
                    {
                        var lower = Convert.ToUInt64(id, fromBase: 16);
                        return new TraceId(higher: 0, lower);
                    }

                    case 32:
                    {
                        var higherAsString = id.Substring(startIndex: 0, length: 16);
                        var lowerAsString = id.Substring(startIndex: 16, length: 16);

                        var higher = Convert.ToUInt64(higherAsString, fromBase: 16);
                        var lower = Convert.ToUInt64(lowerAsString, fromBase: 16);

                        return new TraceId(higher, lower);
                    }

                    default:
                    {
                        return Zero;
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is InvalidOperationException || ex is OverflowException || ex is FormatException)
            {
                Log.Debug($"Could not parse TraceId from string {id}. {ex.Message}");
                return Zero;
            }
        }

        /// <summary>
        /// Creates traceId from given int.
        /// </summary>
        /// <param name="id">Int32 ID to be parsed.</param>
        /// <returns>Instance of <see cref="TraceId"/> representing the same traceId as the passed int.</returns>
        public static TraceId CreateFromInt(int id)
        {
            return new TraceId((ulong)id);
        }

        /// <summary>
        /// Creates traceId from given ulong.
        /// </summary>
        /// <param name="id">Ulong ID to be parsed.</param>
        /// <returns>Instance of <see cref="TraceId"/> representing the same traceId as the passed ulong.</returns>
        public static TraceId CreateFromUlong(ulong id)
        {
            return new TraceId(id);
        }

        /// <summary>
        /// Returns hexadecimal representation of the 128 bit id.
        /// </summary>
        /// <returns>Hex representation of <see cref="TraceId"/> as a string.</returns>
        public override string ToString() => _string;

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is TraceId))
            {
                return false;
            }

            var traceId = (TraceId)obj;

            return Equals(traceId);
        }

        /// <summary>
        /// Indicates if this and another specified instance of TraceId are equal.
        /// </summary>
        /// <param name="other">Trace id to check equality against.</param>
        /// <returns>True if TraceIds are equal, false otherwise.</returns>
        public bool Equals(TraceId other)
        {
            return Lower == other.Lower && Higher == other.Higher;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Higher.GetHashCode();
                hashCode = (hashCode * 397) ^ Lower.GetHashCode();
                return hashCode;
            }
        }
    }
}
