// <copyright file="DirectLogSubmissionSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace.Configuration;
using Datadog.Trace.PlatformHelpers;

namespace Datadog.Trace.Logging.DirectSubmission
{
    /// <summary>
    /// Contains settings for Direct Log Submission.
    /// </summary>
    internal class DirectLogSubmissionSettings
    {
        private const string DefaultSource = "csharp";
        private const DirectSubmissionLogLevel DefaultMinimumLevel = DirectSubmissionLogLevel.Information;
        private const int DefaultBatchSizeLimit = 1000;
        private const int DefaultQueueSizeLimit = 100_000;
        private const int DefaultBatchPeriodSeconds = 2;

        public DirectLogSubmissionSettings()
            : this(source: null)
        {
        }

        public DirectLogSubmissionSettings(IConfigurationSource? source)
        {
            DirectLogSubmissionHost = string.Empty;
            DirectLogSubmissionSource = DefaultSource;

            DirectLogSubmissionUrl = string.Empty;

            DirectLogSubmissionMinimumLevel = DefaultMinimumLevel;

            DirectLogSubmissionGlobalTags = new Dictionary<string, string>();

            DirectLogSubmissionEnabledIntegrations = new HashSet<string>();

            DirectLogSubmissionBatchSizeLimit = DefaultBatchSizeLimit;

            DirectLogSubmissionQueueSizeLimit = DefaultQueueSizeLimit;

            DirectLogSubmissionBatchPeriod = TimeSpan.FromSeconds(DefaultBatchPeriodSeconds);

            SignalFxAccessToken = string.Empty;

            LogsInjectionEnabled = source?.GetBool(ConfigurationKeys.LogsInjectionEnabled);
        }

        /// <summary>
        /// Gets or Sets the integrations enabled for direct log submission
        /// </summary>
        internal HashSet<string> DirectLogSubmissionEnabledIntegrations { get; set; }

        /// <summary>
        /// Gets or Sets the originating host name for direct logs submission
        /// </summary>
        internal string DirectLogSubmissionHost { get; set; }

        /// <summary>
        /// Gets or Sets the originating source for direct logs submission
        /// </summary>
        internal string DirectLogSubmissionSource { get; set; }

        /// <summary>
        /// Gets or sets the global tags, which are applied to all directly submitted logs. If not provided,
        /// <see cref="TracerSettings.GlobalTags"/> are used instead
        /// </summary>
        internal IDictionary<string, string> DirectLogSubmissionGlobalTags { get; set; }

        /// <summary>
        /// Gets or sets the url to send logs to
        /// </summary>
        internal string? DirectLogSubmissionUrl { get; set; }

        /// <summary>
        /// Gets or sets the minimum level logs should have to be sent to the intake.
        /// </summary>
        internal DirectSubmissionLogLevel DirectLogSubmissionMinimumLevel { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of logs to send at one time
        /// </summary>
        internal int DirectLogSubmissionBatchSizeLimit { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of logs to hold in internal queue at any one time
        /// </summary>
        internal int DirectLogSubmissionQueueSizeLimit { get; set; }

        /// <summary>
        /// Gets or sets the time to wait between checking for batches
        /// </summary>
        internal TimeSpan DirectLogSubmissionBatchPeriod { get; set; }

        /// <summary>
        /// Gets or sets the SignalFx Access Token
        /// </summary>
        internal string? SignalFxAccessToken { get; set; }

        /// <summary>
        /// Gets or sets whether logs injection has been explicitly enabled or disabled
        /// </summary>
        internal bool? LogsInjectionEnabled { get; set; }
    }
}
