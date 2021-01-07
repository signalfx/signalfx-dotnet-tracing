// Modified by SignalFx
using System;
using System.Collections;
using SignalFx.Tracing.ExtensionMethods;
using SignalFx.Tracing.Logging;

namespace SignalFx.Tracing.PlatformHelpers
{
    /// <summary>
    /// Helper type for Azure App Services.
    /// </summary>
    public class AzureAppServices
    {
        /// <summary>
        /// Configuration key which is used as a flag to tell us whether we are running in the context of Azure App Services.
        /// </summary>
        internal static readonly string AzureAppServicesContextKey = "SIGNALFX_AZURE_APP_SERVICES";

        /// <summary>
        /// Example: 8c56d827-5f07-45ce-8f2b-6c5001db5c6f+apm-dotnet-EastUSwebspace
        /// Format: {subscriptionId}+{planResourceGroup}-{hostedInRegion}
        /// </summary>
        internal static readonly string WebsiteOwnerNameKey = "WEBSITE_OWNER_NAME";

        /// <summary>
        /// This is the name of the resource group the site instance is assigned to.
        /// </summary>
        internal static readonly string ResourceGroupKey = "WEBSITE_RESOURCE_GROUP";

        /// <summary>
        /// This is the unique name of the website instance within azure app services.
        /// </summary>
        internal static readonly string SiteNameKey = "WEBSITE_DEPLOYMENT_ID";

        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.GetLogger(typeof(AzureAppServices));

        static AzureAppServices()
        {
            Metadata = new AzureAppServices(Environment.GetEnvironmentVariables());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureAppServices"/> class.
        /// </summary>
        /// <param name="environmentVariables">Environment variables passed to the service.</param>
        public AzureAppServices(IDictionary environmentVariables)
        {
            IsRelevant = GetVariableIfExists(AzureAppServicesContextKey, environmentVariables)?.ToBoolean() ?? false;
            if (IsRelevant)
            {
                SubscriptionId = GetSubscriptionId(environmentVariables);
                ResourceGroup = GetVariableIfExists(ResourceGroupKey, environmentVariables);
                SiteName = GetVariableIfExists(SiteNameKey, environmentVariables);
                ResourceId = CompileResourceId();
            }
        }

        /// <summary>
        /// Gets or sets the metadata associated to the service.
        /// </summary>
        public static AzureAppServices Metadata { get; set; }

        /// <summary>
        /// Gets a value indicating whether the environment variable <c>SIGNALFX_AZURE_APP_SERVICES</c>
        /// is defined and it is running as an Azure App Services.
        /// </summary>
        public bool IsRelevant { get; }

        /// <summary>
        /// Gets the subscription ID.
        /// </summary>
        public string SubscriptionId { get; }

        /// <summary>
        /// Gets the resource group.
        /// </summary>
        public string ResourceGroup { get; }

        /// <summary>
        /// Gets the site name.
        /// </summary>
        public string SiteName { get; }

        /// <summary>
        /// Gets the resource ID.
        /// </summary>
        public string ResourceId { get; }

        private string CompileResourceId()
        {
            string resourceId = null;

            try
            {
                var success = true;
                if (SubscriptionId == null)
                {
                    success = false;
                    Log.Warning("Could not successfully retrieve the subscription ID from variable: {0}", WebsiteOwnerNameKey);
                }

                if (SiteName == null)
                {
                    success = false;
                    Log.Warning("Could not successfully retrieve the deployment ID from variable: {0}", SiteNameKey);
                }

                if (ResourceGroup == null)
                {
                    success = false;
                    Log.Warning("Could not successfully retrieve the resource group name from variable: {0}", ResourceGroupKey);
                }

                if (success)
                {
                    resourceId = $"/subscriptions/{SubscriptionId}/resourcegroups/{ResourceGroup}/providers/microsoft.web/sites/{SiteName}".ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not successfully setup the resource id for azure app services.");
            }

            return resourceId;
        }

        private string GetSubscriptionId(IDictionary environmentVariables)
        {
            try
            {
                var websiteOwner = GetVariableIfExists(WebsiteOwnerNameKey, environmentVariables);
                if (!string.IsNullOrWhiteSpace(websiteOwner))
                {
                    var plusSplit = websiteOwner.Split('+');
                    if (plusSplit.Length > 0 && !string.IsNullOrWhiteSpace(plusSplit[0]))
                    {
                        return plusSplit[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not successfully retrieve the subscription id for azure app services.");
            }

            return null;
        }

        private string GetVariableIfExists(string key, IDictionary environmentVariables)
        {
            if (environmentVariables.Contains(key))
            {
                return environmentVariables[key]?.ToString();
            }

            return null;
        }
    }
}
