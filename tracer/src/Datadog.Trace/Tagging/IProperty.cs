// <copyright file="IProperty.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;

namespace Datadog.Trace.Tagging
{
    internal interface IProperty<TResult>
    {
        string Key { get; }

        Func<ITags, TResult> Getter { get; }
        string[] GetLocalMachineValueNames(string key);

        string? GetLocalMachineValue(string key);
    }
}
