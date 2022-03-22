using System;
using System.Collections.Generic;
using System.Threading;
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

        public static void MethodAFloats(
            float fl,
            double db)
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
            void Action(string s) => InternalClassB<string, int>.DoubleInternalClassB.TripleInternalClassB<int>.MethodB(s, new int[] { 3 }, TimeSpan.Zero, 0, new List<string>{"a"}, new List<string>(0));
            Action("test arg");
        }

        internal static class InternalClassB<TA, TD>
        {
            internal static class DoubleInternalClassB
            {
                internal static class TripleInternalClassB<TC>
                {
                    public static void MethodB<TB>(string testArg, TC[] a, TB b, TD t, IList<TA> c, IList<string> d)
                    {
                        GenericClassC<string>.GenericMethodCFromGenericClass(testArg);
                    }
                }
            }
        }
    }

    internal static class GenericClassC<T>
    {
        public static void GenericMethodCFromGenericClass(T arg)
        {
            ClassD<TimeSpan>.GenericMethodDFromGenericClass(TimeSpan.MaxValue, arg, 1);
        }
    }

    internal static class ClassD<TClass>
    {
        public static void GenericMethodDFromGenericClass<TMethod, TMethod2>(TClass classArg, TMethod methodArg, TMethod2 additionalArg)
        {
            Console.WriteLine("Thread.Sleep - starting " + classArg + methodArg);
            Thread.Sleep(TimeSpan.FromSeconds(6));
            Console.WriteLine("Thread.Sleep - finished");
        }
    }
}
