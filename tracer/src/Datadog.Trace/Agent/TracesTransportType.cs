// <copyright file="TracesTransportType.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

namespace Datadog.Trace.Agent
{
    /// <summary>
    /// Available types of transports.
    /// </summary>
    internal enum TracesTransportType
    {
        /// <summary>
        /// Default transport.
        /// Defers transport logic to agent API.
        /// </summary>
        Default,

        /// <summary>
        /// Windows Named Pipe strategy.
        /// Transport used primarily for Azure App Service.
        /// </summary>
        WindowsNamedPipe
    }
}
