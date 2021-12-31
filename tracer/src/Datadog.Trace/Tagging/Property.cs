// <copyright file="Property.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;

namespace Datadog.Trace.Tagging
{
    internal class Property<TTags, TResult> : IProperty<TResult>
    {
        public Property(string key, Func<TTags, TResult> getter)
        {
            Key = key;
            Getter = tags => getter((TTags)tags);
        }

        public string Key { get; }

        public Func<ITags, TResult> Getter { get; }
    }
}
