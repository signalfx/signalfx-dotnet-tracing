// <copyright file="TelemetryFactory.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

#nullable enable
using System;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.Telemetry
{
    internal class TelemetryFactory
    {
        internal static ITelemetryController CreateTelemetryController(ImmutableTracerSettings tracerSettings)
        {
            return NullTelemetryController.Instance;
        }
    }
}
