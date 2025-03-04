// <copyright file="CIVisibilitySettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.Ci.Configuration
{
    internal class CIVisibilitySettings
    {
        public CIVisibilitySettings(IConfigurationSource source)
        {
            Enabled = false;
            Agentless = false;
            Logs = false;
            ApiKey = string.Empty;
            Site = "localhost";
            AgentlessUrl = string.Empty;

            // By default intake payloads has a 5MB limit
            MaximumAgentlessPayloadSize = 5 * 1024 * 1024;

            ProxyHttps = string.Empty;
            ProxyNoProxy = Array.Empty<string>();

            TracerSettings = new TracerSettings(source);

            // Code coverage
            CodeCoverageEnabled = false;
            CodeCoverageSnkFilePath = string.Empty;
        }

        /// <summary>
        /// Gets a value indicating whether the CI Visibility mode was enabled by configuration
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Gets a value indicating whether the Agentless writer is going to be used.
        /// </summary>
        public bool Agentless { get; }

        /// <summary>
        /// Gets the Agentless url.
        /// </summary>
        public string AgentlessUrl { get; }

        /// <summary>
        /// Gets the Api Key to use in Agentless mode
        /// </summary>
        public string ApiKey { get; }

        /// <summary>
        /// Gets the Datadog site
        /// </summary>
        public string Site { get; }

        /// <summary>
        /// Gets the maximum agentless payload size
        /// </summary>
        public int MaximumAgentlessPayloadSize { get; }

        /// <summary>
        /// Gets the https proxy
        /// </summary>
        public string ProxyHttps { get; }

        /// <summary>
        /// Gets the no proxy list
        /// </summary>
        public string[] ProxyNoProxy { get; }

        /// <summary>
        /// Gets a value indicating whether the Logs submission is going to be used.
        /// </summary>
        public bool Logs { get; }

        /// <summary>
        /// Gets a value indicating whether the Code Coverage is enabled.
        /// </summary>
        public bool CodeCoverageEnabled { get; }

        /// <summary>
        /// Gets the snk filepath to re-signing assemblies after the code coverage modification.
        /// </summary>
        public string CodeCoverageSnkFilePath { get; }

        /// <summary>
        /// Gets the tracer settings
        /// </summary>
        public TracerSettings TracerSettings { get; }

        public static CIVisibilitySettings FromDefaultSources()
        {
            var source = GlobalSettings.CreateDefaultConfigurationSource();
            return new CIVisibilitySettings(source);
        }
    }
}
