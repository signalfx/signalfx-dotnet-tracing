// <copyright file="AsyncLocalScopeManager.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Threading;
using Datadog.Trace.ClrProfiler;
using Datadog.Trace.ContinuousProfiler;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    internal class AsyncLocalScopeManager : IScopeManager, IScopeRawAccess
    {
        private readonly AsyncLocal<Scope> _activeScope;

        public AsyncLocalScopeManager(bool alwaysOnProfilerEnabled = false)
        {
            _activeScope = !alwaysOnProfilerEnabled
                ? CreateScope()
                : new AsyncLocal<Scope>(
                    (AsyncLocalValueChangedArgs<Scope> args) =>
                    {
                        UpdateProfilerContext(args.CurrentValue);
                    });
        }

        public Scope Active
        {
            get => _activeScope.Value;
            private set => _activeScope.Value = value;
        }

        // SetProfilingContext is a internal property to facilitate tests, since the native functions are not available
        // on the unit tests. However, it has a cost, since it introduces indirection, per IL it causes an extra callvirt.
        // This is used by AlwaysOnProfiler, upstream uses this same type to update context see CreateScope().
        // TODO: Move this test to a location that the native API can be accessed, add a test helper on the native side
        // and remove this delegate.
        internal Action<ulong, ulong, ulong, int> SetProfilingContext { get; set; } = NativeMethods.SignalFxSetNativeContext;

        Scope IScopeRawAccess.Active
        {
            get => Active;
            set => Active = value;
        }

        public Scope Activate(Span span, bool finishOnClose)
        {
            var newParent = Active;
            var scope = new Scope(newParent, span, this, finishOnClose);

            Active = scope;
            DistributedTracer.Instance.SetSpanContext(scope.Span.Context);

            return scope;
        }

        public void Close(Scope scope)
        {
            var current = Active;

            if (current == null || current != scope)
            {
                // This is not the current scope for this context, bail out
                return;
            }

            // if the scope that was just closed was the active scope,
            // set its parent as the new active scope
            Active = scope.Parent;

            // scope.Parent is null for distributed traces, so use scope.Span.Context.Parent
            DistributedTracer.Instance.SetSpanContext(scope.Span.Context.Parent as SpanContext);
        }

        private static AsyncLocal<Scope> CreateScope()
        {
            if (Profiler.Instance.ContextTracker.IsEnabled)
            {
                return new AsyncLocal<Scope>(OnScopeChanged);
            }

            return new AsyncLocal<Scope>();
        }

        private static void OnScopeChanged(AsyncLocalValueChangedArgs<Scope> obj)
        {
            if (obj.CurrentValue == null)
            {
                Profiler.Instance.ContextTracker.Reset();
            }
            else
            {
                Profiler.Instance.ContextTracker.Set(obj.CurrentValue.Span.RootSpanId, obj.CurrentValue.Span.SpanId);
            }
        }

        /// <summary>
        /// Value update handler used by SignalFx AlwaysOnProfiler
        /// </summary>
        private void UpdateProfilerContext(Scope scope)
        {
            int managedThreadId = Thread.CurrentThread.ManagedThreadId;
            if (scope == null)
            {
                SetProfilingContext(0, 0, 0, managedThreadId);
            }
            else
            {
                SetProfilingContext(scope.Span.TraceId.Higher, scope.Span.TraceId.Lower, scope.Span.SpanId, managedThreadId);
            }
        }
    }
}
