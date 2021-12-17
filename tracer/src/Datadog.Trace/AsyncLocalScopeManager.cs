// <copyright file="AsyncLocalScopeManager.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Datadog.Trace
{
    internal class AsyncLocalScopeManager : ScopeManagerBase
    {
        private static readonly bool PushScopeToNative;
        private readonly AsyncLocal<Scope> _activeScope = new();

        static AsyncLocalScopeManager()
        {
            // FIXME JBLEY share logic or at least constants somewhere
            var enabled = Environment.GetEnvironmentVariable("SIGNALFX_THREAD_SAMPLING_ENABLED");
            if (enabled != null && (enabled.ToLower() == "1" || enabled.ToLower() == "true"))
            {
                PushScopeToNative = true;
            }
            else
            {
                PushScopeToNative = false;
            }
        }

        public override Scope Active
        {
            get
            {
                return _activeScope.Value;
            }

            protected set
            {
                _activeScope.Value = value;
                if (PushScopeToNative)
                {
                    // nop
                    if (value == null)
                    {
                        SignalFx_set_native_context(0, 0, 0);
                    }
                    else
                    {
                        SignalFx_set_native_context(value.Span.TraceId.Higher, value.Span.TraceId.Lower, value.Span.SpanId);
                    }
                }
            }
        }

        [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
#pragma warning disable SA1400 // Access modifier should be declared
        static extern void SignalFx_set_native_context(ulong traceIdHigh, ulong traceIdLow, ulong spanId);
#pragma warning restore SA1400 // Access modifier should be declared
    }
}
