// Modified by SignalFx
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using SignalFx.Tracing.Configuration;
using SignalFx.Tracing.Logging.LogProviders;

namespace SignalFx.Tracing.Logging
{
    /// <summary>
    /// Subscriber to ScopeManager events that sets/unsets correlation identifier
    /// properties in the application's logging context.
    /// </summary>
    internal class LibLogScopeEventSubscriber : IDisposable
    {
        private readonly IScopeManager _scopeManager;
        private readonly ILogProvider _logProvider;
        private readonly TracerSettings _settings;

        // Each mapped context sets a key-value pair into the logging context
        // Disposing the returned context unsets the key-value pair
        // Keep a stack to retain the history of our correlation identifier properties
        // (the stack is particularly important for Serilog, see below).
        //
        // IMPORTANT: Serilog -- The logging contexts (throughout the entire application)
        //            are maintained in a stack, as opposed to a map, and must be closed
        //            in reverse-order of opening. When operating on the stack-based model,
        //            it is only valid to add the properties once and unset them once.
        private readonly ConcurrentStack<IDisposable> _contextDisposalStack = new ConcurrentStack<IDisposable>();

        private bool _safeToAddToMdc = true;

        // IMPORTANT: For all logging frameworks, do not set any default values for
        //            "trace_id" and "span_id" when initializing the subscriber
        //            because the Tracer may be initialized at a time when it is not safe
        //            to add properties logging context of the underlying logging framework.
        //
        //            Failure to abide by this can cause a SerializationException when
        //            control is passed from one AppDomain to another where the originating
        //            AppDomain used a logging framework that stored logging context properties
        //            inside the System.Runtime.Remoting.Messaging.CallContext structure
        //            but the target AppDomain is unable to de-serialize the object --
        //            this can easily happen if the target AppDomain cannot find/load the
        //            logging framework assemblies.
        public LibLogScopeEventSubscriber(IScopeManager scopeManager, TracerSettings settings)
        {
            _scopeManager = scopeManager;
            _settings = settings;

            _logProvider = LogProvider.CurrentLogProvider ?? LogProvider.ResolveLogProvider();
            if (_logProvider is SerilogLogProvider)
            {
                    // Do not set default values for Serilog because it is unsafe to set
                    // except at the application startup, but this would require auto-instrumentation
                    _scopeManager.SpanOpened += StackOnSpanOpened;
                    _scopeManager.SpanClosed += StackOnSpanClosed;
            }
            else if (_logProvider is NLogLogProvider && UseNLogOptimized())
            {
                // This NLog version can use the value readers optimization.
                _scopeManager.SpanActivated += ActivateMappedContext;
                _scopeManager.TraceEnded += DeactivateMappedContext;
            }
            else
            {
                _scopeManager.SpanActivated += MapOnSpanActivated;
                _scopeManager.TraceEnded += MapOnTraceEnded;
            }
        }

        public void StackOnSpanOpened(object sender, SpanEventArgs spanEventArgs)
        {
            SetCorrelationIdentifierContext(new LogCorrelationFields(spanEventArgs.Span));
        }

        public void StackOnSpanClosed(object sender, SpanEventArgs spanEventArgs)
        {
            RemoveLastCorrelationIdentifierContext();
        }

        public void MapOnSpanActivated(object sender, SpanEventArgs spanEventArgs)
        {
            RemoveAllCorrelationIdentifierContexts();
            SetCorrelationIdentifierContext(new LogCorrelationFields(spanEventArgs.Span));
        }

        public void MapOnTraceEnded(object sender, SpanEventArgs spanEventArgs)
        {
            RemoveAllCorrelationIdentifierContexts();
            SetCorrelationIdentifierContext(new LogCorrelationFields());
        }

        public void Dispose()
        {
            if (_logProvider is SerilogLogProvider)
            {
                _scopeManager.SpanOpened -= StackOnSpanOpened;
                _scopeManager.SpanClosed -= StackOnSpanClosed;
            }
            else if (_logProvider is NLogLogProvider && UseNLogOptimized())
            {
                _scopeManager.SpanActivated -= ActivateMappedContext;
                _scopeManager.TraceEnded -= DeactivateMappedContext;
            }
            else
            {
                _scopeManager.SpanActivated -= MapOnSpanActivated;
                _scopeManager.TraceEnded -= MapOnTraceEnded;
            }

            RemoveAllCorrelationIdentifierContexts();
        }

        private static bool UseNLogOptimized()
        {
            // This code checks the same requisits from LibLog\5.0.6\LogProviders\NLogLogProvider.cs to see
            // if the code can use an optimized path for NLog.
            var ndlcContextType = Type.GetType("NLog.NestedDiagnosticsLogicalContext, NLog");
            if (ndlcContextType != null)
            {
                var pushObjectMethod = ndlcContextType.GetMethod("PushObject", typeof(object));
                if (pushObjectMethod != null)
                {
                    var mdlcContextType = Type.GetType("NLog.MappedDiagnosticsLogicalContext, NLog");
                    if (mdlcContextType != null)
                    {
                        var setScopedMethod = mdlcContextType.GetMethod("SetScoped", typeof(string), typeof(object));
                        if (setScopedMethod != null)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void ActivateMappedContext(object sender, SpanEventArgs spanEventArgs)
        {
            var span = spanEventArgs.Span;
            OpenMappedContext(
                span.TraceId.ToString(),
                span.SpanId.ToString("x16"),
                span.ServiceName ?? _settings.ServiceName,
                _settings.Environment);
        }

        private void DeactivateMappedContext(object sender, SpanEventArgs spanEventArgs)
        {
            OpenMappedContext(null, null, null, null);
        }

        private void OpenMappedContext(string traceIdStr, string spanIdStr, string service, string environment)
        {
            LogProvider.OpenMappedContext(
                CorrelationIdentifier.TraceIdKey, traceIdStr, destructure: false);

            LogProvider.OpenMappedContext(
                CorrelationIdentifier.SpanIdKey, spanIdStr, destructure: false);

            LogProvider.OpenMappedContext(
                CorrelationIdentifier.ServiceNameKey, service, destructure: false);

            LogProvider.OpenMappedContext(
                CorrelationIdentifier.ServiceEnvironmentKey, environment, destructure: false);
        }

        private void RemoveLastCorrelationIdentifierContext()
        {
            // TODO: Debug logs
            for (var i = 0; i < 4; i++)
            {
                if (_contextDisposalStack.TryPop(out IDisposable ctxDisposable))
                {
                    ctxDisposable.Dispose();
                }
                else
                {
                    // There is nothing left to pop so do nothing.
                    // Though we are in a strange circumstance if we did not balance
                    // the stack properly
                    Debug.Fail($"{nameof(RemoveLastCorrelationIdentifierContext)} call failed. Too few items on the context stack.");
                }
            }
        }

        private void RemoveAllCorrelationIdentifierContexts()
        {
            // TODO: Debug logs
            while (_contextDisposalStack.TryPop(out IDisposable ctxDisposable))
            {
                ctxDisposable.Dispose();
            }
        }

        private void SetCorrelationIdentifierContext(LogCorrelationFields fields)
        {
            if (!_safeToAddToMdc)
            {
                return;
            }

            try
            {
                // TODO: Debug logs
                _contextDisposalStack.Push(
                    LogProvider.OpenMappedContext(
                        CorrelationIdentifier.TraceIdKey, fields.TraceId.ToString(), destructure: false));
                _contextDisposalStack.Push(
                    LogProvider.OpenMappedContext(
                        CorrelationIdentifier.SpanIdKey, fields.SpanId.ToString("x16"), destructure: false));
                _contextDisposalStack.Push(
                    LogProvider.OpenMappedContext(
                        CorrelationIdentifier.ServiceNameKey, fields.Service, destructure: false));
                _contextDisposalStack.Push(
                    LogProvider.OpenMappedContext(
                        CorrelationIdentifier.ServiceEnvironmentKey, fields.Environment, destructure: false));
            }
            catch (Exception)
            {
                _safeToAddToMdc = false;
                RemoveAllCorrelationIdentifierContexts();
            }
        }

        private class LogCorrelationFields
        {
            public LogCorrelationFields(Span span)
            {
                TraceId = span.TraceId;
                SpanId = span.SpanId;
                Service = span.ServiceName;
                Environment = span.GetTag(Tags.Environment) ?? string.Empty;
            }

            public LogCorrelationFields()
            {
                TraceId = TraceId.Zero;
                SpanId = 0;
                Service = string.Empty;
                Environment = string.Empty;
            }

            public TraceId TraceId { get; }

            public ulong SpanId { get; }

            public string Service { get; }

            public string Environment { get; }
        }
    }
}
