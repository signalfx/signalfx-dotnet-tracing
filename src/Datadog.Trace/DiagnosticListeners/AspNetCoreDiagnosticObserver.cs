// Modified by SignalFx
#if NETSTANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using SignalFx.Tracing.Abstractions;
using SignalFx.Tracing.ExtensionMethods;
using SignalFx.Tracing.Headers;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Propagation;
using SignalFx.Tracing.Util;
using SignalFx.Tracing.Vendors.Serilog.Events;

namespace SignalFx.Tracing.DiagnosticListeners
{
    /// <summary>
    /// Instruments ASP.NET Core.
    /// <para/>
    /// Unfortunately, ASP.NET Core only uses one <see cref="System.Diagnostics.DiagnosticListener"/> instance
    /// for everything so we also only create one observer to ensure best performance.
    /// <para/>
    /// Hosting events: https://github.com/dotnet/aspnetcore/blob/master/src/Hosting/Hosting/src/Internal/HostingApplicationDiagnostics.cs
    /// </summary>
    internal sealed class AspNetCoreDiagnosticObserver : DiagnosticObserver
    {
        public const string IntegrationName = "AspNetCore";

        private const string DiagnosticListenerName = "Microsoft.AspNetCore";
        private const string ComponentName = "aspnet_core";
        private const string HttpRequestInOperationName = "aspnet_core.request";
        private const string NoHostSpecified = "UNKNOWN_HOST";

        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.For<AspNetCoreDiagnosticObserver>();

        private static readonly PropertyFetcher HttpRequestInStartHttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher HttpRequestInStopHttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher UnhandledExceptionHttpContextFetcher = new PropertyFetcher("HttpContext");
        private static readonly PropertyFetcher UnhandledExceptionExceptionFetcher = new PropertyFetcher("Exception");
        private static readonly PropertyFetcher BeforeActionHttpContextFetcher = new PropertyFetcher("httpContext");
        private static readonly PropertyFetcher BeforeActionActionDescriptorFetcher = new PropertyFetcher("actionDescriptor");

        private readonly ISignalFxTracer _tracer;
        private readonly AspNetCoreDiagnosticOptions _options;
        private readonly bool _isLogLevelDebugEnabled = Log.IsEnabled(LogEventLevel.Debug);

        public AspNetCoreDiagnosticObserver(ISignalFxTracer tracer, AspNetCoreDiagnosticOptions options)
            : base(tracer)
        {
            _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected override string ListenerName => DiagnosticListenerName;

        protected override void OnNext(string eventName, object arg)
        {
            switch (eventName)
            {
                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
                    OnHostingHttpRequestInStart(arg);
                    break;

                case "Microsoft.AspNetCore.Mvc.BeforeAction":
                    OnMvcBeforeAction(arg);
                    break;

                case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
                    OnHostingHttpRequestInStop(arg);
                    break;

                case "Microsoft.AspNetCore.Hosting.UnhandledException":
                case "Microsoft.AspNetCore.Diagnostics.UnhandledException":
                    OnHostingUnhandledException(arg);
                    break;
            }
        }

        private static string GetUrl(HttpRequest request)
        {
            if (request.Host.HasValue)
            {
                return $"{request.Scheme}://{request.Host.Value}{request.PathBase.Value}{request.Path.Value}";
            }

            // HTTP 1.0 requests are not required to provide a Host to be valid
            // Since this is just for display, we can provide a string that is
            // not an actual Uri with only the fields that are specified.
            // request.GetDisplayUrl(), used above, will throw an exception
            // if request.Host is null.
            return $"{request.Scheme}://{NoHostSpecified}{request.PathBase.Value}{request.Path.Value}";
        }

        private static SpanContext ExtractPropagatedContext(IPropagator propagator, HttpRequest request)
        {
            try
            {
                // extract propagation details from http headers
                var requestHeaders = request.Headers;

                if (requestHeaders != null)
                {
                    return propagator.Extract(new WrapIHeadersCollection(requestHeaders));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extracting propagated HTTP headers.");
            }

            return null;
        }

        private bool ShouldIgnore(HttpContext httpContext)
        {
            foreach (Func<HttpContext, bool> ignore in _options.IgnorePatterns)
            {
                if (ignore(httpContext))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnHostingHttpRequestInStart(object arg)
        {
            var httpContext = (HttpContext)HttpRequestInStartHttpContextFetcher.Fetch(arg);

            if (ShouldIgnore(httpContext))
            {
                if (_isLogLevelDebugEnabled)
                {
                    Log.Debug("Ignoring request");
                }
            }
            else
            {
                HttpRequest request = httpContext.Request;
                string host = request.Host.Value;
                string httpMethod = request.Method?.ToUpperInvariant() ?? "UNKNOWN";
                string url = GetUrl(request);

                string resourceUrl = UriHelpers.GetCleanUriPath(new Uri(url))
                                               .ToLowerInvariant();

                var propagator = _tracer.Propagator;
                SpanContext propagatedContext = ExtractPropagatedContext(propagator, request);

                Span span = _tracer.StartSpan(HttpRequestInOperationName, propagatedContext)
                                   .SetTag(Tags.InstrumentationName, ComponentName);

                IPAddress remoteIp = null;
                if (Tracing.Tracer.Instance.Settings.AddClientIpToServerSpans)
                {
                    remoteIp = httpContext?.Connection?.RemoteIpAddress;
                }

                span.DecorateWebServerSpan(resourceUrl, httpMethod, host, url, remoteIp);
                span.SetTag(Tags.InstrumentationName, IntegrationName);

                Scope scope = _tracer.ActivateSpan(span);

                _options.OnRequest?.Invoke(scope.Span, httpContext);
            }
        }

        private void OnMvcBeforeAction(object arg)
        {
            var httpContext = (HttpContext)BeforeActionHttpContextFetcher.Fetch(arg);

            if (ShouldIgnore(httpContext))
            {
                if (_isLogLevelDebugEnabled)
                {
                    Log.Debug("Ignoring request");
                }
            }
            else
            {
                Span span = _tracer.ScopeManager.Active?.Span;

                if (span != null)
                {
                    // NOTE: This event is the start of the action pipeline. The action has been selected, the route
                    //       has been selected but no filters have run and model binding hasn't occured.
                    var actionDescriptor = (ActionDescriptor)BeforeActionActionDescriptorFetcher.Fetch(arg);

                    // Try to use the best tag values available.

                    if (!span.Tags.ContainsKey(Tags.HttpMethod))
                    {
                        HttpRequest request = httpContext.Request;
                        string httpMethod = request.Method?.ToUpperInvariant();

                        if (!string.IsNullOrEmpty(httpMethod))
                        {
                            span.Tags.Add(Tags.HttpMethod, httpMethod);
                        }
                    }

                    if (actionDescriptor.RouteValues.TryGetValue("controller", out string controllerName))
                    {
                        span.Tags[Tags.AspNetController] = controllerName;
                    }

                    if (actionDescriptor.RouteValues.TryGetValue("action", out string actionName))
                    {
                        span.Tags[Tags.AspNetAction] = actionName;
                    }

                    string routeTemplate = actionDescriptor.AttributeRouteInfo?.Template;
                    if (!string.IsNullOrEmpty(routeTemplate))
                    {
                        span.OperationName = routeTemplate;
                    }
                    else if (!string.IsNullOrEmpty(controllerName) && !string.IsNullOrEmpty(actionName))
                    {
                        span.OperationName = $"{controllerName}.{actionName}".ToLowerInvariant();
                    }

                    ServerTimingHeader.SetHeaders(span.Context, httpContext.Response.Headers, (headers, name, value) => headers.Add(name, value));
                }
            }
        }

        private void OnHostingHttpRequestInStop(object arg)
        {
            IScope scope = _tracer.ScopeManager.Active;

            if (scope != null)
            {
                var httpContext = (HttpContext)HttpRequestInStopHttpContextFetcher.Fetch(arg);
                scope.Span.SetTag(Tags.HttpStatusCode, httpContext.Response.StatusCode.ToString());

                if (httpContext.Response.StatusCode / 100 == 5)
                {
                    // 5xx codes are server-side errors
                    scope.Span.Error = true;
                }

                scope.Dispose();
            }
        }

        private void OnHostingUnhandledException(object arg)
        {
            ISpan span = _tracer.ScopeManager.Active?.Span;

            if (span != null)
            {
                var exception = (Exception)UnhandledExceptionExceptionFetcher.Fetch(arg);
                var httpContext = (HttpContext)UnhandledExceptionHttpContextFetcher.Fetch(arg);

                span.SetException(exception);
                _options.OnError?.Invoke(span, exception, httpContext);
            }
        }

        private struct WrapIHeadersCollection : IHeadersCollection
        {
            private readonly IHeaderDictionary _headerDictionary;

            public WrapIHeadersCollection(IHeaderDictionary headerDictionary)
            {
                _headerDictionary = headerDictionary;
            }

            public IEnumerable<string> GetValues(string name)
            {
                return _headerDictionary.TryGetValue(name, out var stringValues)
                    ? stringValues
                    : Enumerable.Empty<string>();
            }

            public void Set(string name, string value)
            {
                _headerDictionary[name] = value;
            }

            public void Add(string name, string value)
            {
                _headerDictionary.Add(name, value);
            }

            public void Remove(string name)
            {
                _headerDictionary.Remove(name);
            }
        }
    }
}
#endif
