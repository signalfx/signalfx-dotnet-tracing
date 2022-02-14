// <copyright file="Startup.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Reflection;

namespace Datadog.Trace.ClrProfiler.Managed.Loader
{
    /// <summary>
    /// A class that attempts to load the SignalFx.Tracing .NET assembly.
    /// </summary>
    public partial class Startup
    {
        private const string AssemblyName = "SignalFx.Tracing, Version=0.2.2.0, Culture=neutral, PublicKeyToken=e43a27c2023d388a";

        /// <summary>
        /// Initializes static members of the <see cref="Startup"/> class.
        /// This method also attempts to load the SignalFx.TracingTrace .NET assembly.
        /// </summary>
        static Startup()
        {
            ManagedProfilerDirectory = ResolveManagedProfilerDirectory();

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve_ManagedProfilerDependencies;
            }
            catch (Exception ex)
            {
                StartupLogger.Log(ex, "Unable to register a callback to the CurrentDomain.AssemblyResolve event.");
            }

            TryLoadManagedAssembly();
        }

        internal static string ManagedProfilerDirectory { get; }

        private static void TryLoadManagedAssembly()
        {
            try
            {
                var assembly = Assembly.Load(AssemblyName);

                if (assembly != null)
                {
                    // call method Datadog.Trace.ClrProfiler.Instrumentation.Initialize()
                    var type = assembly.GetType("Datadog.Trace.ClrProfiler.Instrumentation", throwOnError: false);
                    var method = type?.GetRuntimeMethod("Initialize", parameters: Type.EmptyTypes);
                    method?.Invoke(obj: null, parameters: null);
                }
            }
            catch (Exception ex)
            {
                StartupLogger.Log(ex, "Error when loading managed assemblies.");
            }
        }

        private static string ReadEnvironmentVariable(string key)
        {
            try
            {
                return Environment.GetEnvironmentVariable(key);
            }
            catch (Exception ex)
            {
                StartupLogger.Log(ex, "Error while loading environment variable " + key);
            }

            return null;
        }
    }
}
