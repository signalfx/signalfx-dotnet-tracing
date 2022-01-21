// <copyright file="CITracerManagerFactory.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using Datadog.Trace.Agent;
using Datadog.Trace.Ci.Agent;
using Datadog.Trace.Ci.Sampling;
using Datadog.Trace.Configuration;
using Datadog.Trace.Conventions;
using Datadog.Trace.Logging.DirectSubmission;
using Datadog.Trace.Propagation;
using Datadog.Trace.RuntimeMetrics;
using Datadog.Trace.Sampling;
using Datadog.Trace.Vendors.StatsdClient;

namespace Datadog.Trace.Ci
{
    internal class CITracerManagerFactory : TracerManagerFactory
    {
        protected override TracerManager CreateTracerManagerFrom(
            ImmutableTracerSettings settings,
            IAgentWriter agentWriter,
            ISampler sampler,
            IPropagator propagator,
            IScopeManager scopeManager,
            IDogStatsd statsd,
            RuntimeMetricsWriter runtimeMetrics,
            ITraceIdConvention traceIdConvention,
            DirectLogSubmissionManager logSubmissionManager,
            string defaultServiceName)
        {
            return new CITracerManager(settings, agentWriter, sampler, propagator, scopeManager, statsd, runtimeMetrics, traceIdConvention, logSubmissionManager, defaultServiceName);
        }

        protected override ISampler GetSampler(ImmutableTracerSettings settings)
        {
            return new CISampler();
        }

        protected override IAgentWriter GetAgentWriter(ImmutableTracerSettings settings, IDogStatsd statsd, ISampler sampler)
        {
            return new CIAgentWriter(settings, sampler);
        }
    }
}
