using System;

namespace Datadog.Trace.DogStatsd
{
    internal class NoOpTimer : IDisposable
    {
        internal static readonly NoOpTimer Instance = new NoOpTimer();

        public void Dispose()
        {
        }
    }
}
