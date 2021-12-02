// <copyright file="TracerHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System.Collections.Generic;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Plugins;
using Datadog.Trace.Sampling;
using Datadog.Trace.Vendors.StatsdClient;

namespace Datadog.Trace.TestHelpers
{
    internal static class TracerHelper
    {
        /// <summary>
        /// Create a test instance of the Tracer, that doesn't used any shared instances
        /// </summary>
        public static Tracer Create(
            TracerSettings settings = null,
            IReadOnlyCollection<IOTelExtension> plugins = null,
            IAgentWriter agentWriter = null,
            ISampler sampler = null,
            IScopeManager scopeManager = null,
            IDogStatsd statsd = null)
        {
            return new Tracer(settings, plugins, agentWriter, sampler, scopeManager, statsd);
        }
    }
}
