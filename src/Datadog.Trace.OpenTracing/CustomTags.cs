namespace SignalFx.Tracing.OpenTracing
{
    /// <summary>
    /// This set of tags is used by the OpenTracing compatible tracer to set custom fields.
    /// </summary>
    public static class CustomTags
    {
        /// <summary>
        /// This tag sets the service name
        /// </summary>
        public const string ServiceName = "service.name";

        /// <summary>
        /// This tag sets resource name
        /// </summary>
        public const string ResourceName = "resource.name";

        /// <summary>
        /// This tag sets the span type
        /// </summary>
        public const string SpanType = "span.type";
    }
}
