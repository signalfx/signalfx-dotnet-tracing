// <copyright file="IScopeManager.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by SignalFx

namespace Datadog.Trace
{
    /// <summary>
    /// Interface for managing a scope.
    /// </summary>
    public interface IScopeManager
    {
        /// <summary>
        /// Gets the active scope.
        /// </summary>
        Scope Active { get; }

        /// <summary>
        /// Activates the given span.
        /// </summary>
        /// <param name="span">Span to be activated.</param>
        /// <param name="finishOnClose">Flag indicating if the span should be finished when closed.</param>
        /// <returns>Active scope</returns>
        Scope Activate(ISpan span, bool finishOnClose);

        /// <summary>
        /// Closes the given scope.
        /// </summary>
        /// <param name="scope">Scope to be closed.</param>
        void Close(Scope scope);
    }
}
