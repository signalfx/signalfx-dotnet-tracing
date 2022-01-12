// <copyright file="CITracerManager.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.Conventions;
using Datadog.Trace.Propagation;
using Datadog.Trace.Logging.DirectSubmission;
using Datadog.Trace.RuntimeMetrics;
using Datadog.Trace.Sampling;
using Datadog.Trace.Vendors.StatsdClient;

namespace Datadog.Trace.Ci
{
    internal class CITracerManager : TracerManager, ILockedTracer
    {
        public CITracerManager(ImmutableTracerSettings settings, IAgentWriter agentWriter, ISampler sampler, IPropagator propagator, IScopeManager scopeManager, IDogStatsd statsd, RuntimeMetricsWriter runtimeMetricsWriter, ITraceIdConvention traceIdConvention, DirectLogSubmissionManager logSubmissionManager, string defaultServiceName)
            : base(settings, agentWriter, sampler, propagator, scopeManager, statsd, runtimeMetricsWriter, traceIdConvention, logSubmissionManager, defaultServiceName)
        {
        }
    }
}
