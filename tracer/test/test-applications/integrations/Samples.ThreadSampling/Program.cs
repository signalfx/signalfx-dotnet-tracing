using System;
using System.Threading;

ClassA.MethodA();

internal static class ClassA
{
    public static void MethodA()
    {
        ClassB.MethodB();
    }
}
internal static class ClassB
{
    public static void MethodB()
    {
        ClassC.MethodC();
    }
}
internal static class ClassC
{
    public static void MethodC()
    {
        ClassD.MethodD();
    }
}
internal static class ClassD
{
    public static void MethodD()
    {
        Console.WriteLine("Thread.Sleep - starting");
        Thread.Sleep(TimeSpan.FromSeconds(value: 5));
        Console.WriteLine("Thread.Sleep - finished");
    }
}
