// taken from: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Api/Trace/Status.cs
namespace Datadog.Trace
{
    /// <summary>
    /// Span execution status.
    /// </summary>
    public readonly struct SpanStatus : System.IEquatable<SpanStatus>
    {
        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        public static readonly SpanStatus Ok = new SpanStatus(StatusCode.Ok);

        /// <summary>
        /// The default status.
        /// </summary>
        public static readonly SpanStatus Unset = new SpanStatus(StatusCode.Unset);

        /// <summary>
        /// The operation contains an error.
        /// </summary>
        public static readonly SpanStatus Error = new SpanStatus(StatusCode.Error);

        internal SpanStatus(StatusCode statusCode)
        {
            this.StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the canonical code from this status.
        /// </summary>
        public StatusCode StatusCode { get; }

        /// <summary>
        /// Compare two <see cref="SpanStatus"/> for equality.
        /// </summary>
        /// <param name="status1">First Status to compare.</param>
        /// <param name="status2">Second Status to compare.</param>
        public static bool operator ==(SpanStatus status1, SpanStatus status2) => status1.Equals(status2);

        /// <summary>
        /// Compare two <see cref="SpanStatus"/> for not equality.
        /// </summary>
        /// <param name="status1">First Status to compare.</param>
        /// <param name="status2">Second Status to compare.</param>
        public static bool operator !=(SpanStatus status1, SpanStatus status2) => !status1.Equals(status2);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is SpanStatus))
            {
                return false;
            }

            var that = (SpanStatus)obj;
            return this.StatusCode == that.StatusCode;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.StatusCode.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return nameof(SpanStatus)
                + "{"
                + nameof(this.StatusCode) + "=" + this.StatusCode
                + "}";
        }

        /// <inheritdoc/>
        public bool Equals(SpanStatus other)
        {
            return this.StatusCode == other.StatusCode;
        }
    }
}
