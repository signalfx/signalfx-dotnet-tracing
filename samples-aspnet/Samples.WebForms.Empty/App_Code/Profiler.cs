using System;
using System.Runtime.InteropServices;

namespace Samples.WebForms.Empty
{
    public static class Profiler
    {
        public static bool IsAttached
        {
            get
            {
                try
                {
                    return IsProfilerAttached();
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
            }
        }

        [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
        private static extern bool IsProfilerAttached();
    }
}
