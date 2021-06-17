// Modified by SignalFx
#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Datadog.Trace.AspNet;
using Datadog.Trace.ClrProfiler.Emit;
using SignalFx.Tracing;
using SignalFx.Tracing.ExtensionMethods;
using SignalFx.Tracing.Logging;
using SignalFx.Tracing.Propagation;
using SignalFx.Tracing.Util;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    /// <summary>
    /// Contains instrumentation wrappers for ASP.NET Web API 5.
    /// </summary>
    public static class AspNetWebApi2Integration
    {
        private const string IntegrationName = "AspNetWebApi2";
        private const string OperationName = "aspnet-webapi.request";
        private const string Major5Minor1 = "5.1";
        private const string Major5 = "5";

        private const string SystemWebHttpAssemblyName = "System.Web.Http";
        private const string HttpControllerTypeName = "System.Web.Http.Controllers.IHttpController";
        private const string HttpControllerContextTypeName = "System.Web.Http.Controllers.HttpControllerContext";

        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(AspNetWebApi2Integration));

        /// <summary>
        /// Calls the underlying ExecuteAsync and traces the request.
        /// </summary>
        /// <param name="apiController">The Api Controller</param>
        /// <param name="controllerContext">The controller context for the call</param>
        /// <param name="boxedCancellationToken">The cancellation token</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>A task with the result</returns>
        [InterceptMethod(
            TargetAssembly = SystemWebHttpAssemblyName,
            TargetType = HttpControllerTypeName,
            TargetSignatureTypes = new[] { ClrNames.HttpResponseMessageTask, HttpControllerContextTypeName, ClrNames.CancellationToken },
            TargetMinimumVersion = Major5Minor1,
            TargetMaximumVersion = Major5)]
        public static object ExecuteAsync(
            object apiController,
            object controllerContext,
            object boxedCancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            if (apiController == null) { throw new ArgumentNullException(nameof(apiController)); }

            var cancellationToken = (CancellationToken)boxedCancellationToken;
            return ExecuteAsyncInternal(apiController, controllerContext, cancellationToken, opCode, mdToken, moduleVersionPtr);
        }

        /// <summary>
        /// Calls the underlying ExecuteAsync and traces the request.
        /// </summary>
        /// <param name="apiController">The Api Controller</param>
        /// <param name="controllerContext">The controller context for the call</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <param name="opCode">The OpCode used in the original method call.</param>
        /// <param name="mdToken">The mdToken of the original method call.</param>
        /// <param name="moduleVersionPtr">A pointer to the module version GUID.</param>
        /// <returns>A task with the result</returns>
        private static async Task<HttpResponseMessage> ExecuteAsyncInternal(
            object apiController,
            object controllerContext,
            CancellationToken cancellationToken,
            int opCode,
            int mdToken,
            long moduleVersionPtr)
        {
            Func<object, object, CancellationToken, Task<HttpResponseMessage>> instrumentedMethod;

            try
            {
                var httpControllerType = apiController.GetInstrumentedInterface(HttpControllerTypeName);

                instrumentedMethod = MethodBuilder<Func<object, object, CancellationToken, Task<HttpResponseMessage>>>
                                    .Start(moduleVersionPtr, mdToken, opCode, nameof(ExecuteAsync))
                                    .WithConcreteType(httpControllerType)
                                    .WithParameters(controllerContext, cancellationToken)
                                    .WithNamespaceAndNameFilters(
                                         ClrNames.HttpResponseMessageTask,
                                         HttpControllerContextTypeName,
                                         ClrNames.CancellationToken)
                                    .Build();
            }
            catch (Exception ex)
            {
                Log.ErrorRetrievingMethod(
                    exception: ex,
                    moduleVersionPointer: moduleVersionPtr,
                    mdToken: mdToken,
                    opCode: opCode,
                    instrumentedType: HttpControllerTypeName,
                    methodName: nameof(ExecuteAsync),
                    instanceType: apiController.GetType().AssemblyQualifiedName);
                throw;
            }

            using (Scope scope = CreateScope(controllerContext))
            {
                try
                {
                    // call the original method, inspecting (but not catching) any unhandled exceptions
                    var responseMessage = await instrumentedMethod(apiController, controllerContext, cancellationToken).ConfigureAwait(false);

                    if (scope != null)
                    {
                        // some fields aren't set till after execution, so populate anything missing
                        UpdateSpan(controllerContext, scope.Span);
                        if (responseMessage.StatusCode == HttpStatusCode.OK)
                        {
                            scope.Span.SetTag(Tags.HttpStatusCode, "200");
                        }
                        else
                        {
                            scope.Span.SetTag(Tags.HttpStatusCode, ((int)responseMessage.StatusCode).ToString(CultureInfo.InvariantCulture));
                            if (!string.IsNullOrWhiteSpace(responseMessage.ReasonPhrase))
                            {
                                scope.Span.SetTag(Tags.HttpStatusText, responseMessage.ReasonPhrase);
                            }
                        }

                        ServerTimingHeader.SetHeaders(scope.Span.Context, responseMessage, (response, name, value) => response.Headers.Add(name, value));
                    }

                    return responseMessage;
                }
                catch (Exception ex)
                {
                    if (scope != null)
                    {
                        // some fields aren't set till after execution, so populate anything missing
                        UpdateSpan(controllerContext, scope.Span);
                    }

                    scope?.Span.SetException(ex);

                    throw;
                }
            }
        }

        private static Scope CreateScope(object controllerContext)
        {
            Scope scope = null;

            try
            {
                if (!Tracer.Instance.Settings.IsIntegrationEnabled(IntegrationName))
                {
                    // integration disabled, don't create a scope, skip this trace
                    return null;
                }

                var tracer = Tracer.Instance;
                var request = controllerContext.GetProperty<HttpRequestMessage>("Request").GetValueOrDefault();
                SpanContext propagatedContext = null;

                if (request != null && tracer.ActiveScope == null)
                {
                    try
                    {
                        // extract propagated http headers
                        var headers = request.Headers.Wrap();
                        propagatedContext = tracer.Propagator.Extract(headers);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error extracting propagated HTTP headers.");
                    }
                }

                scope = tracer.StartActive(OperationName, propagatedContext);
                UpdateSpan(controllerContext, scope.Span);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating scope.");
            }

            return scope;
        }

        private static void UpdateSpan(dynamic controllerContext, Span span)
        {
            try
            {
                var request = controllerContext.Request;
                Uri requestUri = request.RequestUri;

                string host = request.Headers.Host ?? string.Empty;
                string rawUrl = requestUri?.ToString().ToLowerInvariant() ?? string.Empty;
                string method = request.Method.Method?.ToUpperInvariant() ?? "GET";
                string route = null;
                try
                {
                    route = controllerContext.RouteData.Route.RouteTemplate;
                }
                catch
                {
                }

                string resourceName;
                bool newResourceNamesEnabled = Tracer.Instance.Settings.RouteTemplateResourceNamesEnabled;

                if (route != null)
                {
                    resourceName = $"{method} {(newResourceNamesEnabled ? "/" : string.Empty)}{route.ToLowerInvariant()}";
                }
                else if (requestUri != null)
                {
                    var cleanUri = UriHelpers.GetCleanUriPath(requestUri);
                    resourceName = $"{method} {cleanUri.ToLowerInvariant()}";
                }
                else
                {
                    resourceName = $"{method}";
                }

                string controller = string.Empty;
                string action = string.Empty;
                string area = string.Empty;
                try
                {
                    var routeValues = controllerContext.RouteData.Values;
                    if (routeValues != null)
                    {
                        controller = (routeValues.GetValueOrDefault("controller") as string)?.ToLowerInvariant();
                        action = (routeValues.GetValueOrDefault("action") as string)?.ToLowerInvariant();
                        area = (routeValues.GetValueOrDefault("area") as string)?.ToLowerInvariant();
                    }
                }
                catch
                {
                }

                // Replace well-known routing tokens
                var resourceNameBuilder = new StringBuilder(resourceName);
                resourceNameBuilder
                       .Replace("{area}", area)
                       .Replace("{controller}", controller)
                       .Replace("{action}", action);

                IPAddress remoteIp = null;
                if (request != null && Tracer.Instance.Settings.AddClientIpToServerSpans)
                {
                    var httpContextWrapper = request.Properties.GetValueOrDefault("MS_HttpContext") as HttpContextWrapper;
                    if (httpContextWrapper != null)
                    {
                        IPAddress.TryParse(httpContextWrapper.Request.UserHostAddress, out remoteIp);
                    }
                    else
                    {
                        var remoteEndpoint = request.Properties.GetValueOrDefault(RemoteEndpointMessageProperty.Name) as RemoteEndpointMessageProperty;
                        if (remoteEndpoint != null)
                        {
                            IPAddress.TryParse(remoteEndpoint.Address, out remoteIp);
                        }
                    }
                }

                resourceName = resourceNameBuilder.ToString();

                span.DecorateWebServerSpan(
                    resourceName: resourceName,
                    method: method,
                    host: host,
                    httpUrl: rawUrl,
                    remoteIp: remoteIp);

                span.SetTag(Tags.AspNetAction, action);
                span.SetTag(Tags.AspNetController, controller);
                span.SetTag(Tags.AspNetArea, area);
                span.SetTag(Tags.AspNetRoute, route);

                if (newResourceNamesEnabled)
                {
                    // set the resource name in the HttpContext so TracingHttpModule can update root span
                    var httpContext = HttpContext.Current;
                    if (httpContext is not null)
                    {
                        httpContext.Items[SharedConstants.HttpContextPropagatedResourceNameKey] = resourceName;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error populating scope data.");
            }
        }
    }
}

#endif
