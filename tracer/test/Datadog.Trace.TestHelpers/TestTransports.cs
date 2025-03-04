// <copyright file="TestTransports.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

namespace Datadog.Trace.TestHelpers
{
    public enum TestTransports
    {
        /// <summary>
        /// Default transport
        /// </summary>
        Tcp,

        /// <summary>
        /// Windows Named Pipes, primarily used in Azure App Service scenarios
        /// </summary>
        WindowsNamedPipe
    }
}
