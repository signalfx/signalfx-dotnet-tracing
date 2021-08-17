using System;

namespace SignalFx.Tracing.DuckTyping.Tests.Methods.ProxiesDefinitions
{
    public abstract class DefaultGenericMethodDuckTypeAbstractClass
    {
        public abstract T GetDefault<T>();

        public abstract Tuple<T1, T2> Wrap<T1, T2>(T1 a, T2 b);
    }
}
