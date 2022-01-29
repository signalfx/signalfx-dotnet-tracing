// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Datadog.Trace.Tests.ThreadSampling
{
    public class AsyncLocalScopeManagerTests
    {
        [Fact(Timeout = 5_000)]
        public async Task PushAsyncContextToNative()
        {
            // For the tests use a delegate that keeps track of current context
            var tid2ProfilingCtx = new Dictionary<int, ProfilingContext>();
            var mockSetProfilingContext = (ulong traceIdHigher, ulong traceIdLower, ulong spanId, int managedThreadId) =>
                {
                    if (traceIdHigher == 0 && traceIdLower == 0 && spanId == 0)
                    {
                        tid2ProfilingCtx.Remove(managedThreadId);
                        return;
                    }

                    tid2ProfilingCtx[managedThreadId] = new ProfilingContext
                    {
                        TraceIdHigher = traceIdHigher,
                        TraceIdLower = traceIdLower,
                        SpanId = spanId,
                    };
                };

            var scopeManager = new AsyncLocalScopeManager(pushScopeToNative: true)
            {
                SetProfilingContext = mockSetProfilingContext
            };

            var span = new Span(new SpanContext(TraceId.CreateFromInt(42), 41), DateTimeOffset.UtcNow, null);
            Scope activeScope = scopeManager.Activate(span, false);

            var currentManagedThreadId = Thread.CurrentThread.ManagedThreadId;
            var initialManagedThreadId = currentManagedThreadId;

            AssertProfilingContextMatchesScope(activeScope, currentManagedThreadId);

            while (currentManagedThreadId == initialManagedThreadId)
            {
                await Task.Delay(100);
                currentManagedThreadId = Thread.CurrentThread.ManagedThreadId;
            }

            // Context must have migrated to the new thread.
            AssertProfilingContextMatchesScope(activeScope, currentManagedThreadId);

            // Context must have been cleaned up from old thread.
            Assert.False(tid2ProfilingCtx.ContainsKey(initialManagedThreadId));

            activeScope.Close();

            Assert.False(tid2ProfilingCtx.ContainsKey(currentManagedThreadId));

            void AssertProfilingContextMatchesScope(Scope scope, int managedThreadId)
            {
                var scopeCtx = scope.Span.Context;
                var profilingCtx = tid2ProfilingCtx[managedThreadId];

                Assert.Equal(scopeCtx.TraceId.Higher, profilingCtx.TraceIdHigher);
                Assert.Equal(scopeCtx.TraceId.Lower, profilingCtx.TraceIdLower);
                Assert.Equal(scopeCtx.SpanId, profilingCtx.SpanId);
            }
        }

        private class ProfilingContext
        {
            public ulong TraceIdHigher { get; set; }

            public ulong TraceIdLower { get; set; }

            public ulong SpanId { get; set; }
        }
    }
}
