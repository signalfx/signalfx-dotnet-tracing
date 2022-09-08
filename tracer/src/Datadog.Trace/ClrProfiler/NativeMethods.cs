// <copyright file="NativeMethods.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

// Modified by Splunk Inc.

using System;
using System.Runtime.InteropServices;

// ReSharper disable MemberHidesStaticFromOuterClass
namespace Datadog.Trace.ClrProfiler
{
    internal static class NativeMethods
    {
        private static readonly bool IsWindows = FrameworkDescription.Instance.IsWindows();

        public static bool IsProfilerAttached()
        {
            if (IsWindows)
            {
                return Windows.IsProfilerAttached();
            }

            return NonWindows.IsProfilerAttached();
        }

        public static void InitializeProfiler(string id, NativeCallTargetDefinition[] methodArrays)
        {
            if (methodArrays is null || methodArrays.Length == 0)
            {
                return;
            }

            if (IsWindows)
            {
                Windows.InitializeProfiler(id, methodArrays, methodArrays.Length);
            }
            else
            {
                NonWindows.InitializeProfiler(id, methodArrays, methodArrays.Length);
            }
        }

        public static void EnableByRefInstrumentation()
        {
            if (IsWindows)
            {
                Windows.EnableByRefInstrumentation();
            }
            else
            {
                NonWindows.EnableByRefInstrumentation();
            }
        }

        public static void EnableCallTargetStateByRef()
        {
            if (IsWindows)
            {
                Windows.EnableCallTargetStateByRef();
            }
            else
            {
                NonWindows.EnableCallTargetStateByRef();
            }
        }

        public static void AddDerivedInstrumentations(string id, NativeCallTargetDefinition[] methodArrays)
        {
            if (methodArrays is null || methodArrays.Length == 0)
            {
                return;
            }

            if (IsWindows)
            {
                Windows.AddDerivedInstrumentations(id, methodArrays, methodArrays.Length);
            }
            else
            {
                NonWindows.AddDerivedInstrumentations(id, methodArrays, methodArrays.Length);
            }
        }

        public static void AddTraceAttributeInstrumentation(string id, string assemblyName, string typeName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName)
                || string.IsNullOrWhiteSpace(typeName))
            {
                return;
            }

            if (IsWindows)
            {
                Windows.AddTraceAttributeInstrumentation(id, assemblyName, typeName);
            }
            else
            {
                NonWindows.AddTraceAttributeInstrumentation(id, assemblyName, typeName);
            }
        }

        public static void InitializeTraceMethods(string id, string assemblyName, string typeName, string configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration)
                || string.IsNullOrWhiteSpace(assemblyName)
                || string.IsNullOrWhiteSpace(typeName))
            {
                return;
            }

            if (IsWindows)
            {
                Windows.InitializeTraceMethods(id, assemblyName, typeName, configuration);
            }
            else
            {
                NonWindows.InitializeTraceMethods(id, assemblyName, typeName, configuration);
            }
        }

        public static int SignalFxReadThreadSamples(int len, byte[] buf)
        {
            return IsWindows ? Windows.SignalFxReadThreadSamples(len, buf) : NonWindows.SignalFxReadThreadSamples(len, buf);
        }

        public static int SignalFxReadAllocationSamples(int len, byte[] buf)
        {
            return IsWindows ? Windows.SignalFxReadAllocationSamples(len, buf) : NonWindows.SignalFxReadAllocationSamples(len, buf);
        }

        public static void SignalFxSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId, int managedThreadId)
        {
            if (IsWindows)
            {
                Windows.SignalFxSetNativeContext(traceIdHigh, traceIdLow, spanId, managedThreadId);
            }
            else
            {
                NonWindows.SignalFxSetNativeContext(traceIdHigh, traceIdLow, spanId, managedThreadId);
            }
        }

        // the "dll" extension is required on .NET Framework
        // and optional on .NET Core
        private static class Windows
        {
            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern bool IsProfilerAttached();

            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern void InitializeProfiler([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern void EnableByRefInstrumentation();

            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern void EnableCallTargetStateByRef();

            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern void AddTraceAttributeInstrumentation([MarshalAs(UnmanagedType.LPWStr)] string id, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, [MarshalAs(UnmanagedType.LPWStr)] string typeName);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern void InitializeTraceMethods([MarshalAs(UnmanagedType.LPWStr)] string id, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, [MarshalAs(UnmanagedType.LPWStr)] string typeName, [MarshalAs(UnmanagedType.LPWStr)] string configuration);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern int SignalFxReadThreadSamples(int len, byte[] buf);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern int SignalFxReadAllocationSamples(int len, byte[] buf);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native.dll")]
            public static extern void SignalFxSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId, int managedThreadId);
        }

        // assume .NET Core if not running on Windows
        private static class NonWindows
        {
            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern bool IsProfilerAttached();

            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern void InitializeProfiler([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern void EnableByRefInstrumentation();

            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern void EnableCallTargetStateByRef();

            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern void AddTraceAttributeInstrumentation([MarshalAs(UnmanagedType.LPWStr)] string id, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, [MarshalAs(UnmanagedType.LPWStr)] string typeName);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern void InitializeTraceMethods([MarshalAs(UnmanagedType.LPWStr)] string id, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, [MarshalAs(UnmanagedType.LPWStr)] string typeName, [MarshalAs(UnmanagedType.LPWStr)] string configuration);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern int SignalFxReadThreadSamples(int len, byte[] buf);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern int SignalFxReadAllocationSamples(int len, byte[] buf);

            [DllImport("SignalFx.Tracing.ClrProfiler.Native")]
            public static extern void SignalFxSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId, int managedThreadId);
        }
    }
}
