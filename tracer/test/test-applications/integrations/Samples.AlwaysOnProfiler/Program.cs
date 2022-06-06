using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.InteropServices;
using My.Custom.Test.Namespace;

ClassA.MethodA();

namespace My.Custom.Test.Namespace
{
    internal class CustomClass
    {
    }

    internal struct CustomStruct
    {
    }

    internal static class ClassA
    {
        private delegate int Callback(int n);

        private static readonly Callback TestCallback = GenericClassC<int>.GenericMethodCFromGenericClass;

        [DllImport("Samples.AlwaysOnProfiler.NativeDep")]
        private static extern int SignalFxCallbackTest(Callback fp, int n);

        public static void MethodA()
        {
            MethodABytes(
                false,
                '\0',
                sbyte.MaxValue,
                byte.MaxValue);
        }

        public static void MethodABytes(
            bool b,
            char c,
            sbyte sb,
            byte b2)
        {
            MethodAInts(
                ushort.MaxValue,
                short.MaxValue,
                uint.MaxValue, 
                int.MaxValue,
                ulong.MaxValue,
                long.MaxValue,
                new nint(),
                new int());
        }

        public static void MethodAInts(
            ushort ui16,
            short i16,
            uint ui32,
            int i32,
            ulong ui64,
            long i64,
            nint nint,
            nuint nuint)
        {
            MethodAFloats(float.MaxValue, double.MaxValue);
        }

        public static unsafe void MethodAFloats(
            float fl,
            double db)
        {
            int a = 1;
            int* pointer = &a;
            MethodAPointer(pointer);
        }

        public static unsafe void MethodAPointer(int* pointer)
        {
            MethodAOthers(string.Empty,
                new object(),
                new CustomClass(),
                new CustomStruct(),
                Array.Empty<CustomClass>(),
                Array.Empty<CustomStruct>(),
                new List<string>());
        }

        public static void MethodAOthers<T>(
            string s,
            object obj,
            CustomClass customClass,
            CustomStruct customStruct,
            CustomClass[] classArray,
            CustomStruct[] structArray,
            List<T> genericList)
        {
            void Action(int s) => InternalClassB<string, int>.DoubleInternalClassB.TripleInternalClassB<int>.MethodB(s, new int[] { 3 }, TimeSpan.Zero, 0, new List<string>{"a"}, new List<string>(0));
            Action(3);
        }

        internal static class InternalClassB<TA, TD>
        {
            internal static class DoubleInternalClassB
            {

                internal static class TripleInternalClassB<TC>
                {
                    public static void MethodB<TB>(int testArg, TC[] a, TB b, TD t, IList<TA> c, IList<string> d)
                    {
                        SignalFxCallbackTest(TestCallback, testArg);
                    }
                }
            }
        }
    }

    internal static class GenericClassC<T>
    {
        public static int GenericMethodCFromGenericClass(T arg)
        {
            GenericMethodCFromGenericClass(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21);
            return 1;
        }

        public static void GenericMethodCFromGenericClass<T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(T01 p01, T02 p02, T03 p03, T04 p04, T05 p05, T06 p06, T07 p07, T08 p08, T09 p09, T10 p10, T11 p11, T12 p12, T13 p13, T14 p14, T15 p15, T16 p16, T17 p17, T18 p18, T19 p19, T20 p20, T21 p21)
        {
            // Always on profiler supports fetching at most 20 generic arguments. This method covers scenario where there are more than 20 parameters.
            ClassD<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>.MethodD(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21);
        }

    }

    internal static class ClassD<T01, T02, T03, T04, T05, T06, T07, T08, T09, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>
    {
        // Always on profiler supports fetching at most 20 generic arguments. This class covers scenario where there are more than 20 parameters.
        public static void MethodD(T01 p01, T02 p02, T03 p03, T04 p04, T05 p05, T06 p06, T07 p07, T08 p08, T09 p09, T10 p10, T11 p11, T12 p12, T13 p13, T14 p14, T15 p15, T16 p16, T17 p17, T18 p18, T19 p19, T20 p20, T21 p21)
        {
            ClassENonStandardCharactersĄĘÓŁŻŹĆąęółżźśćĜЖᏳⳄʤǋₓڿଟഐቐ〣‿؁੮ᾭ_<TimeSpan>.GenericMethodDFromGenericClass(TimeSpan.MaxValue, p01, 1);
        }
    }

    internal static class ClassENonStandardCharactersĄĘÓŁŻŹĆąęółżźśćĜЖᏳⳄʤǋₓڿଟഐቐ〣‿؁੮ᾭ_<TClass>
    {
        public static void GenericMethodDFromGenericClass<TMethod, TMethod2>(TClass classArg, TMethod methodArg, TMethod2 additionalArg)
        {
            dynamic test = new Datadog.Trace.TestDynamicClass();

            test("Param1", "Param2");
        }
    }


}

// Datadog.Trace namespace will be replaced by SignalFx.Tracing by the ThreadSampleNativeFormatParser
namespace Datadog.Trace
{
    internal class TestDynamicClass : DynamicObject
    {
        public override bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            Samples.AlwaysOnProfiler.Vb.ClassVb.MethodVb("testParam");
            result = null;
            return true;
        }
    }
}
