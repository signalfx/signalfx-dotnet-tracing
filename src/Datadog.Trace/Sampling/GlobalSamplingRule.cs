using System;
using System.Collections.Generic;
using SignalFx.Tracing.Logging;

namespace SignalFx.Tracing.Sampling
{
    internal class GlobalSamplingRule : ISamplingRule
    {
        private static readonly SignalFx.Tracing.Vendors.Serilog.ILogger Log = SignalFxLogging.For<GlobalSamplingRule>();

        private readonly float _globalRate;

        public GlobalSamplingRule(float rate)
        {
            _globalRate = rate;
        }

        public string RuleName => "global-rate-rule";

        /// <summary>
        /// Gets the priority which is one beneath custom rules.
        /// </summary>
        public int Priority => 0;

        public bool IsMatch(Span span)
        {
            return true;
        }

        public float GetSamplingRate(Span span)
        {
            Log.Debug("Using the global sampling rate: {0}", _globalRate);
            span.SetMetric(Metrics.SamplingRuleDecision, _globalRate);
            return _globalRate;
        }
    }
}
