namespace SignalFx.Tracing.Sampling
{
    internal interface IRateLimiter
    {
        bool Allowed(Span span);

        float GetEffectiveRate();
    }
}
