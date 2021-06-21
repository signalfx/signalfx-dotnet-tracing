// Modified by SignalFx
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
using SignalFx.Tracing.Headers;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Propagation;
using SignalFx.Tracing.Util;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal class AspNetAmbientContext : IDisposable
    {
        private static readonly string HttpContextKey = "__Datadog_web_request_ambient_context__";
        private static readonly string TopLevelOperationName = "web.request";
        private static readonly string StartupDiagnosticMethod = "DEBUG";
        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(AspNetAmbientContext));

        private readonly ConcurrentStack<IDisposable> _disposables = new ConcurrentStack<IDisposable>();
        private readonly ConcurrentDictionary<string, Scope> _scopeStorage = new ConcurrentDictionary<string, Scope>();
        private readonly object _httpContext;
        private readonly Scope _rootScope;

        private AspNetAmbientContext(string integrationName, object httpContext)
        {
            try
            {
                Tracer = Tracer.Instance;
                _httpContext = httpContext;

                var request = _httpContext.GetProperty("Request").GetValueOrDefault();
                var response = _httpContext.GetProperty("Response").GetValueOrDefault();

                GetTagValues(
                    request,
                    out string absoluteUri,
                    out string httpMethod,
                    out string host,
                    out string resourceName);

                if (httpMethod == StartupDiagnosticMethod)
                {
                    // An initial diagnostic HttpContext is created on the start of many web applications
                    AbortRegistration = true;
                    return;
                }

                RegisterForDisposalWithPipeline(response, this);

                SpanContext propagatedContext = null;

                if (Tracer.ActiveScope == null)
                {
                    try
                    {
                        // extract propagated http headers
                        var requestHeaders = request.GetProperty<IEnumerable>("Headers").GetValueOrDefault();

                        if (requestHeaders != null)
                        {
                            var headersCollection = new DictionaryHeadersCollection();

                            foreach (object header in requestHeaders)
                            {
                                var key = header.GetProperty<string>("Key").GetValueOrDefault();
                                var values = header.GetProperty<IList<string>>("Value").GetValueOrDefault();

                                if (key != null && values != null)
                                {
                                    headersCollection.Add(key, values);
                                }
                            }

                            propagatedContext = Tracer.Instance.Propagator.Extract(headersCollection);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error extracting propagated HTTP headers.");
                    }
                }

                _rootScope = Tracer.StartActive(TopLevelOperationName, propagatedContext);

                RegisterForDisposal(_rootScope);

                var span = _rootScope.Span;

                IPAddress remoteIp = null;
                if (Tracer.Instance.Settings.AddClientIpToServerSpans)
                {
                    var userHostAddress = request.GetProperty<string>("UserHostAddress").GetValueOrDefault();
                    IPAddress.TryParse(userHostAddress, out remoteIp);
                }

                span.DecorateWebServerSpan(
                    resourceName: resourceName,
                    method: httpMethod,
                    host: host,
                    httpUrl: absoluteUri,
                    remoteIp: remoteIp);

                var statusCode = response.GetProperty<int>("StatusCode");

                if (statusCode.HasValue)
                {
                    span.SetTag(Tags.HttpStatusCode, statusCode.Value.ToString());
                }
            }
            catch (Exception ex)
            {
                // Don't crash client apps
                Log.Error(ex, $"Exception when initializing {nameof(AspNetAmbientContext)}.");
            }
        }

        /// <summary>
        /// Gets the instance of the Tracer for this web request.
        /// Ensure that the same Tracer instance is used throughout an entire request.
        /// </summary>
        internal Tracer Tracer { get; }

        /// <summary>
        /// Gets the root span for this web request.
        /// </summary>
        internal Span RootSpan => _rootScope?.Span;

        /// <summary>
        /// Gets a value indicating whether this context should be registered.
        /// </summary>
        internal bool AbortRegistration { get; }

        public void Dispose()
        {
            try
            {
                var request = _httpContext.GetProperty("Response");
                var statusCodeResult = request.GetProperty<int>("StatusCode");

                if (statusCodeResult.HasValue)
                {
                    SetStatusCode(statusCodeResult.Value);
                }
            }
            catch (Exception ex)
            {
                // No exceptions in dispose
                Log.Error(ex, "Exception when trying to populate data at the end of the request pipeline.");
            }

            while (_disposables.TryPop(out IDisposable registeredDisposable))
            {
                try
                {
                    registeredDisposable?.Dispose();
                }
                catch (Exception ex)
                {
                    // No exceptions in dispose
                    Log.Error(ex, $"Exception when disposing {registeredDisposable?.GetType().FullName ?? "NULL"}.");
                }
            }
        }

        /// <summary>
        /// Responsible for setting up an overarching Scope and then registering with the end of pipeline disposal.
        /// </summary>
        /// <param name="httpContext">Instance of Microsoft.AspNetCore.Http.DefaultHttpContext</param>
        internal static void Initialize(object httpContext)
        {
            var context = new AspNetAmbientContext(TopLevelOperationName, httpContext);

            if (context.AbortRegistration)
            {
                return;
            }

            if (httpContext.TryGetPropertyValue("Items", out IDictionary<object, object> contextItems))
            {
                contextItems[HttpContextKey] = context;
            }
        }

        internal static AspNetAmbientContext RetrieveFromHttpContext(object httpContext)
        {
            AspNetAmbientContext context = null;

            try
            {
                if (httpContext.TryGetPropertyValue("Items", out IDictionary<object, object> contextItems))
                {
                    if (contextItems?.ContainsKey(HttpContextKey) ?? false)
                    {
                        context = contextItems[HttpContextKey] as AspNetAmbientContext;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error accessing {nameof(AspNetAmbientContext)}.");
            }

            return context;
        }

        internal bool TryPersistScope(string key, Scope scope)
        {
            return _scopeStorage.TryAdd(key, scope);
        }

        internal bool TryRetrieveScope(string key, out Scope scope)
        {
            return _scopeStorage.TryGetValue(key, out scope);
        }

        internal void RegisterForDisposal(IDisposable disposable)
        {
            _disposables.Push(disposable);
        }

        internal void SetStatusCode(int statusCode)
        {
            SetTagOnRootSpan(Tags.HttpStatusCode, statusCode.ToString());
        }

        internal void SetTagOnRootSpan(string tag, string value)
        {
            _rootScope?.Span?.SetTag(tag, value);
        }

        internal bool SetExceptionOnRootSpan(Exception ex)
        {
            _rootScope?.Span?.SetException(ex);
            // Return false for use in exception filters
            return false;
        }

        internal void ResetWebServerRootTags(string resourceName, string method)
        {
            if (_rootScope?.Span != null)
            {
                if (!string.IsNullOrWhiteSpace(resourceName))
                {
                    _rootScope.Span.ResourceName = resourceName?.Trim();
                }

                if (!string.IsNullOrWhiteSpace(method))
                {
                    SetTagOnRootSpan(Tags.HttpMethod, method);
                }
            }
        }

        private static void GetTagValues(
            object request,
            out string url,
            out string httpMethod,
            out string host,
            out string resourceName)
        {
            host = request.GetProperty("Host").GetProperty<string>("Value").GetValueOrDefault();

            httpMethod = request.GetProperty<string>("Method").GetValueOrDefault()?.ToUpperInvariant() ?? "UNKNOWN";

            string pathBase = request.GetProperty("PathBase").GetProperty<string>("Value").GetValueOrDefault();

            string path = request.GetProperty("Path").GetProperty<string>("Value").GetValueOrDefault();

            string queryString = request.GetProperty("QueryString").GetProperty<string>("Value").GetValueOrDefault();

            string scheme = request.GetProperty<string>("Scheme").GetValueOrDefault()?.ToUpperInvariant() ?? "http";

            url = $"{pathBase}{path}{queryString}";

            string resourceUrl = UriHelpers.GetCleanUriPath(new Uri($"{scheme}://{host}{url}")).ToLowerInvariant();

            resourceName = $"{httpMethod} {resourceUrl}";
        }

        private static void RegisterForDisposalWithPipeline(object response, IDisposable disposable)
        {
            try
            {
                if (response == null)
                {
                    Log.Error($"HttpContext.Response is null, unable to register {disposable.GetType().FullName}");
                    return;
                }

                var disposalRegisterMethod = response.GetType().GetMethod("RegisterForDispose");
                disposalRegisterMethod.Invoke(response, new object[] { disposable });
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unable to register {disposable.GetType().FullName}");
            }
        }
    }
}
