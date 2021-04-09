namespace SignalFx.Tracing.Configuration
{
    /// <summary>
    /// Enumeration for the available propagator types.
    /// </summary>
    public enum PropagatorType
    {
        /// <summary>
        /// The default propagator.
        /// Default is <c>B3</c>.
        /// </summary>
        Default,

        /// <summary>
        /// The W3C propagator.
        /// </summary>
        W3C,

        /// <summary>
        /// The B3 propagator
        /// </summary>
        B3
    }
}
