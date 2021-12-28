// <copyright file="AsyncLocalScopeManager.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Threading;
using Datadog.Trace.ClrProfiler;

namespace Datadog.Trace
{
    internal class AsyncLocalScopeManager : ScopeManagerBase
    {
        private readonly bool _pushScopeToNative;

        private readonly AsyncLocal<Scope> _activeScope = new();

        public AsyncLocalScopeManager(bool pushScopeToNative)
        {
            _pushScopeToNative = pushScopeToNative;
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
                if (_pushScopeToNative)
                {
                    // nop
                    if (value == null)
                    {
                        NativeMethods.SignalFxSetNativeContext(traceIdHigh: 0, traceIdLow: 0, spanId: 0);
                    }
                    else
                    {
                        NativeMethods.SignalFxSetNativeContext(value.Span.TraceId.Higher, value.Span.TraceId.Lower, value.Span.SpanId);
                    }
                }
            }
        }
    }
}
