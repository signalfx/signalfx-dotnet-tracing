using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            void Action(string s) => InternalClassB.MethodB(s);
            Action("test arg");
        }

        internal static class InternalClassB
        {
            public static void MethodB(string testArg)
            {
                GenericClassC<string>.GenericMethodCFromGenericClass(testArg);
            }
        }
    }

    internal static class GenericClassC<T>
    {
        public static void GenericMethodCFromGenericClass(T arg)
        {
            ClassD<TimeSpan>.GenericMethodDFromGenericClass(TimeSpan.MaxValue, arg);
        }
    }

    internal static class ClassD<TClass>
    {
        public static void GenericMethodDFromGenericClass<TMethod>(TClass classArg, TMethod methodArg)
        {
            Console.WriteLine("Thread.Sleep - starting " + classArg + methodArg);
            var stackTrace = new StackTrace();
            Console.WriteLine(stackTrace);
            /*
   at My.Custom.Test.Namespace.ClassD`1.GenericMethodDFromNonGenericClass[TMethod](TClass classArg, TMethod methodArg)
   at My.Custom.Test.Namespace.GenericClassC`1.GenericMethodCFromGenericClass(T arg)
   at My.Custom.Test.Namespace.ClassA.InternalClassB.MethodB(String testArg)
   at My.Custom.Test.Namespace.ClassA.<MethodAOthers>g__Action|4_0[T](String s)
   at My.Custom.Test.Namespace.ClassA.MethodAOthers[T](String s, Object obj, CustomClass customClass, CustomStruct customStruct, CustomClass[] classArray, CustomStruct[] structArray, List`1 genericList)
   at My.Custom.Test.Namespace.ClassA.MethodAFloats(Single fl, Double db)
   at My.Custom.Test.Namespace.ClassA.MethodAInts(UInt16 ui16, Int16 i16, UInt32 ui32, Int32 i32, UInt64 ui64, Int64 i64, IntPtr nint, UIntPtr nuint)
   at My.Custom.Test.Namespace.ClassA.MethodABytes(Boolean b, Char c, SByte sb, Byte b2)
   at My.Custom.Test.Namespace.ClassA.MethodA()
   at Program.<Main>$(String[] args)
             */
            Thread.Sleep(TimeSpan.FromSeconds(6));
            Console.WriteLine("Thread.Sleep - finished");
        }
    }
}
