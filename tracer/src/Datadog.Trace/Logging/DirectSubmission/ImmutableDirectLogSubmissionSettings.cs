// <copyright file="ImmutableDirectLogSubmissionSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.
#nullable enable

using System;
using System.Collections.Generic;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging.DirectSubmission.Sink.PeriodicBatching;

namespace Datadog.Trace.Logging.DirectSubmission
{
    /// <summary>
    /// Contains direct-log-submission-specific settings
    /// </summary>
    internal class ImmutableDirectLogSubmissionSettings
    {
        internal static readonly IntegrationId[] SupportedIntegrations =
        {
            IntegrationId.Serilog,
            IntegrationId.ILogger,
            IntegrationId.Log4Net,
            IntegrationId.NLog,
            IntegrationId.XUnit,
        };

        private readonly bool[] _enabledIntegrations;

        private ImmutableDirectLogSubmissionSettings(
            string host,
            string source,
            string globalTags,
            Uri intakeUrl,
            string apiKey,
            bool isEnabled,
            DirectSubmissionLogLevel minimumLevel,
            bool[] enabledIntegrations,
            List<string> validationErrors,
            List<string> enabledIntegrationNames,
            BatchingSinkOptions batchingOptions)
        {
            Host = host;
            Source = source;
            GlobalTags = globalTags;
            IntakeUrl = intakeUrl;
            ApiKey = apiKey;
            ValidationErrors = validationErrors;
            EnabledIntegrationNames = enabledIntegrationNames;
            MinimumLevel = minimumLevel;
            _enabledIntegrations = enabledIntegrations;
            IsEnabled = isEnabled;
            BatchingOptions = batchingOptions;
        }

        public bool IsEnabled { get; }

        public DirectSubmissionLogLevel MinimumLevel { get; }

        public string Host { get; }

        public string Source { get; }

        public string GlobalTags { get; }

        public Uri IntakeUrl { get; }

        public string ApiKey { get; }

        public List<string> ValidationErrors { get; }

        public List<string> EnabledIntegrationNames { get; }

        public BatchingSinkOptions BatchingOptions { get; }

        public static ImmutableDirectLogSubmissionSettings CreateNullSettings()
        {
            var emptyList = new List<string>(0);
            // not trying to enable log submission, so don't log any errors and create a _null_ implementation
            return new ImmutableDirectLogSubmissionSettings(
                host: string.Empty,
                source: string.Empty,
                globalTags: string.Empty,
                intakeUrl: new Uri("http://localhost"),
                apiKey: string.Empty,
                isEnabled: false,
                minimumLevel: DirectSubmissionLogLevel.Fatal,
                enabledIntegrations: Array.Empty<bool>(),
                validationErrors: emptyList,
                enabledIntegrationNames: emptyList,
                batchingOptions: new BatchingSinkOptions(batchSizeLimit: 1, queueLimit: 1, TimeSpan.MaxValue));
        }

        public bool IsIntegrationEnabled(IntegrationId integrationId)
        {
            return IsEnabled && _enabledIntegrations[(int)integrationId];
        }
    }
}
