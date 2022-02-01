// Modified by Splunk Inc.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Datadog.Trace.Tests.ThreadSampling
{
    public class AsyncLocalScopeManagerTests
    {
        [Fact(Timeout = 500_000)]
        public void PushAsyncContextToNative()
        {
            // Have the whole test running in a dedicated thread so Tasks get to
            // run in the thread pool, causing an async thread change. This is
            // not required on dev boxes but makes the test more robust for machines
            // with a smaller number of cores.
            // Running the test in a dedicated thread requires any exception throw
            // in the test, eg.: an assert, to be flowed to the original test thread.
            Exception testException = null;
            var testThread = new Thread(() =>
            {
                try
                {
                    // The actual code of the test will execute in different threads
                    // because of that the thread must wait for its completion.
                    TestThread().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
            });

            testThread.Start();
            testThread.Join();

            // Check if there wasn't any assertion/exception on the test thread.
            testException.Should().BeNull();

            async Task TestThread()
            {
                // For the tests use a delegate that keeps track of current context
                // in the test thread.
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
                    var blockingThreadTask = Task.Run(() => Thread.Sleep(200));
                    await Task.Delay(100);
                    await blockingThreadTask;
                    currentManagedThreadId = Thread.CurrentThread.ManagedThreadId;
                }

                // Context must have migrated to the new thread.
                AssertProfilingContextMatchesScope(activeScope, currentManagedThreadId);

                // Context must have been cleaned up from old thread.
                tid2ProfilingCtx.Should().NotContainKey(initialManagedThreadId);

                activeScope.Close();

                tid2ProfilingCtx.Should().NotContainKey(currentManagedThreadId);

                void AssertProfilingContextMatchesScope(Scope scope, int managedThreadId)
                {
                    var scopeCtx = scope.Span.Context;
                    var profilingCtx = tid2ProfilingCtx[managedThreadId];

                    using (new AssertionScope())
                    {
                        scopeCtx.TraceId.Higher.Should().Be(profilingCtx.TraceIdHigher);
                        scopeCtx.TraceId.Lower.Should().Be(profilingCtx.TraceIdLower);
                        scopeCtx.SpanId.Should().Be(profilingCtx.SpanId);
                    }
                }
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
