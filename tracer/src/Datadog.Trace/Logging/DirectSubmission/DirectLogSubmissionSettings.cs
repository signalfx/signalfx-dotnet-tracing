// <copyright file="DirectLogSubmissionSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#nullable enable

using Datadog.Trace.Configuration;

namespace Datadog.Trace.Logging.DirectSubmission
{
    /// <summary>
    /// Contains settings for Direct Log Submission.
    /// </summary>
    internal class DirectLogSubmissionSettings
    {
        public DirectLogSubmissionSettings(IConfigurationSource? source)
        {
            LogsInjectionEnabled = source?.GetBool(ConfigurationKeys.LogsInjectionEnabled);
        }

        /// <summary>
        /// Gets or sets whether logs injection has been explicitly enabled or disabled
        /// </summary>
        internal bool? LogsInjectionEnabled { get; set; }
    }
}
