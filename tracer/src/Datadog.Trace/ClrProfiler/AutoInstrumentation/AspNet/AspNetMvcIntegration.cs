// <copyright file="AspNetMvcIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Routing;
using Datadog.Trace.AspNet;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging;
using Datadog.Trace.Propagation;
using Datadog.Trace.Propagators;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AspNet
{
    /// <summary>
    /// The ASP.NET MVC integration.
    /// </summary>
    internal static class AspNetMvcIntegration
    {
        internal const string HttpContextKey = "__SignalFx.Tracing.ClrProfiler.Integrations.AspNetMvcIntegration";

        private const string OperationName = "aspnet-mvc.request";
        private const string ChildActionOperationName = "aspnet-mvc.request.child-action";

        private const string RouteCollectionRouteTypeName = "System.Web.Mvc.Routing.RouteCollectionRoute";

        private const IntegrationId IntegrationId = Configuration.IntegrationId.AspNetMvc;
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(AspNetMvcIntegration));

        /// <summary>
        /// Creates a scope used to instrument an MVC action and populates some common details.
        /// </summary>
        /// <param name="controllerContext">The System.Web.Mvc.ControllerContext that was passed as an argument to the instrumented method.</param>
        /// <returns>A new scope used to instrument an MVC action.</returns>
        internal static Scope CreateScope(ControllerContextStruct controllerContext)
        {
            Scope scope = null;

            try
            {
                var httpContext = controllerContext.HttpContext;
                if (httpContext == null)
                {
                    return null;
                }

                Span span = null;
                // integration enabled, go create a scope!
                var tracer = Tracer.Instance;
                if (tracer.Settings.IsIntegrationEnabled(IntegrationId))
                {
                    var newResourceNamesEnabled = tracer.Settings.RouteTemplateResourceNamesEnabled;
                    string host = httpContext.Request.Headers.Get("Host");
                    string userAgent = httpContext.Request.Headers.Get(CommonHttpHeaderNames.UserAgent);
                    string httpMethod = httpContext.Request.HttpMethod.ToUpperInvariant();
                    string url = httpContext.Request.Url?.ToString(); // Upstream uses RawUrl, ie. the part of the URL following the domain information.
                    string resourceName = null;

                    RouteData routeData = controllerContext.RouteData;
                    Route route = routeData?.Route as Route;
                    RouteValueDictionary routeValues = routeData?.Values;
                    bool wasAttributeRouted = false;
                    bool isChildAction = controllerContext.ParentActionViewContext.RouteData?.Values["controller"] is not null;

                    if (isChildAction && newResourceNamesEnabled)
                    {
                        // For child actions, we want to stick to what was requested in the http request.
                        // And the child action being a child, then we have already computed the resourcename.
                        resourceName = httpContext.Items[SharedItems.HttpContextPropagatedResourceNameKey] as string;
                    }

                    if (route == null && routeData?.Route.GetType().FullName == RouteCollectionRouteTypeName)
                    {
                        var routeMatches = routeValues?.GetValueOrDefault("MS_DirectRouteMatches") as List<RouteData>;

                        if (routeMatches?.Count > 0)
                        {
                            // route was defined using attribute routing i.e. [Route("/path/{id}")]
                            // get route and routeValues from the RouteData in routeMatches
                            wasAttributeRouted = true;
                            route = routeMatches[0].Route as Route;
                            routeValues = routeMatches[0].Values;
                        }
                    }

                    string routeUrl = route?.Url;
                    string areaName;
                    string controllerName;
                    string actionName;
                    if ((wasAttributeRouted || newResourceNamesEnabled) && string.IsNullOrEmpty(resourceName) && !string.IsNullOrEmpty(routeUrl))
                    {
                        resourceName = AspNetResourceNameHelper.CalculateResourceName(
                            httpMethod: httpMethod,
                            routeTemplate: routeUrl,
                            routeValues,
                            defaults: wasAttributeRouted ? null : route.Defaults,
                            out areaName,
                            out controllerName,
                            out actionName,
                            expandRouteTemplates: newResourceNamesEnabled && tracer.Settings.ExpandRouteTemplatesEnabled);
                    }
                    else
                    {
                        // just grab area/controller/action directly
                        areaName = (routeValues?.GetValueOrDefault("area") as string)?.ToLowerInvariant();
                        controllerName = (routeValues?.GetValueOrDefault("controller") as string)?.ToLowerInvariant();
                        actionName = (routeValues?.GetValueOrDefault("action") as string)?.ToLowerInvariant();
                    }

                    if (string.IsNullOrEmpty(resourceName))
                    {
                        // Keep the legacy resource name, just to have something
                        resourceName = $"{httpMethod} {controllerName}.{actionName}";
                    }

                    SpanContext propagatedContext = null;
                    var tagsFromHeaders = Enumerable.Empty<KeyValuePair<string, string>>();

                    if (tracer.InternalActiveScope == null)
                    {
                        try
                        {
                            // extract propagated http headers
                            var headers = httpContext.Request.Headers.Wrap();
                            var propagator = SpanContextPropagator.Instance;
                            propagatedContext = propagator.Extract(headers);
                            tagsFromHeaders = headers.ExtractHeaderTags(tracer.Settings.HeaderTags, PropagationExtensions.HttpRequestHeadersTagPrefix);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error extracting propagated HTTP headers.");
                        }
                    }

                    var tags = new AspNetTags();
                    scope = tracer.StartActiveInternal(resourceName, propagatedContext, tags: tags);
                    span = scope.Span;

                    span.DecorateWebServerSpan(
                        resourceName: resourceName,
                        method: httpMethod,
                        host: host,
                        httpUrl: url,
                        userAgent: userAgent,
                        tags,
                        tagsFromHeaders,
                        httpContext.Request.UserHostAddress);

                    span.LogicScope = isChildAction ? ChildActionOperationName : OperationName;

                    tags.AspNetRoute = routeUrl;
                    tags.AspNetArea = areaName;
                    tags.AspNetController = controllerName;
                    tags.AspNetAction = actionName;

                    tags.SetAnalyticsSampleRate(IntegrationId, tracer.Settings, enabledWithGlobalSetting: true);

                    if (string.IsNullOrEmpty(httpContext.Items[SharedItems.HttpContextPropagatedResourceNameKey] as string))
                    {
                        // set the resource name in the HttpContext so TracingHttpModule can update root span
                        httpContext.Items[SharedItems.HttpContextPropagatedResourceNameKey] = resourceName;
                    }

                    tracer.TracerManager.Telemetry.IntegrationGeneratedSpan(IntegrationId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return scope;
        }
    }
}
#endif
