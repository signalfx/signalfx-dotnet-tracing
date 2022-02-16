// Modified by Splunk Inc.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Samples
{
    public class SampleHelpers
    {
        private static readonly Type NativeMethodsType = Type.GetType("Datadog.Trace.ClrProfiler.NativeMethods, SignalFx.Tracing");
        private static readonly Type TracerType = Type.GetType("Datadog.Trace.Tracer, SignalFx.Tracing");
        private static readonly MethodInfo GetTracerInstance = TracerType.GetProperty("Instance").GetMethod;
        private static readonly MethodInfo StartActiveMethod = TracerType.GetMethod("StartActive", types: new[] { typeof(string) });

        public static bool IsProfilerAttached()
        {
            if (NativeMethodsType is null)
            {
                return false;
            }

            try
            {
                MethodInfo profilerAttachedMethodInfo = NativeMethodsType.GetMethod("IsProfilerAttached");
                return (bool)profilerAttachedMethodInfo.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public static string GetTracerAssemblyLocation()
        {
            return NativeMethodsType?.Assembly.Location ?? "(none)";
        }

        public static void RunShutDownTasks(object caller)
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                foreach (var type in GetLoadableDefinedTypes(assembly))
                {
                    try
                    {
                        if (type.Namespace == "Coverlet.Core.Instrumentation.Tracker")
                        {
                            var unloadModuleMethod = type.GetMethod("UnloadModule", BindingFlags.Public | BindingFlags.Static);
                            unloadModuleMethod.Invoke(null, new object[] { caller, EventArgs.Empty });
                        }
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        // catch exception when the file assemlby cannot be loaded. In this case it is related to OpenTracing library
                    }
                }
            }
        }

        public static IDisposable CreateScope(string operationName)
        {
            var tracer = GetTracerInstance.Invoke(null, Array.Empty<object>());
            return (IDisposable) StartActiveMethod.Invoke(tracer, new object[] { operationName });
        }

        public static IEnumerable<KeyValuePair<string,string>> GetDatadogEnvironmentVariables()
        {
            var prefixes = new[] { "COR_", "CORECLR_", "SIGNALFX_" };

            var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                          from prefix in prefixes
                          let key = (envVar.Key as string)?.ToUpperInvariant()
                          let value = envVar.Value as string
                          where key.StartsWith(prefix)
                          orderby key
                          select new KeyValuePair<string, string>(key, value);

            return envVars.ToList();
        }

        private static IEnumerable<Type> GetLoadableDefinedTypes(Assembly assembly)
        {
            try
            {
                return assembly.DefinedTypes;
            }
            catch (ReflectionTypeLoadException rtlex)
            {
                // return only loadable types, defined in really existing library
                // it should prevent loading types in SignalFx.Tracing.OpenTracing existing in OpenTracing library
                return rtlex.Types.Where(t => t != null);
            }
        }
    }
}
