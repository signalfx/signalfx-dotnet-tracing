using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Samples.AlwaysOnProfiler.Performance
{
    internal class AVeryLongClassNameThatIsNotVeryRepresentativeButFulfillsMyRequirements
    {
        public static void SomeInternalMethodA() { SomeInternalMethodB(); }
        public static void SomeInternalMethodB() { SomeInternalMethodC(); }
        public static void SomeInternalMethodC() { SomeInternalMethodD(); }
        public static void SomeInternalMethodD() { SomeInternalMethodE(); }
        public static void SomeInternalMethodE() { SomeInternalMethodF(); }
        public static void SomeInternalMethodF() { SomeInternalMethodG(); }
        public static void SomeInternalMethodG() { SomeInternalMethodH(); }
        public static void SomeInternalMethodH() { SomeInternalMethodI(); }
        public static void SomeInternalMethodI()
        {
            AnotherVeryLongClassNameThatIAmUsingToHaveParticularlyLongNames.AnotherMethodNameWithSomeLengthA();
        }
    }

    internal class AnotherVeryLongClassNameThatIAmUsingToHaveParticularlyLongNames
    {
        public static void AnotherMethodNameWithSomeLengthA() { AnotherMethodNameWithSomeLengthB(); }
        public static void AnotherMethodNameWithSomeLengthB() { AnotherMethodNameWithSomeLengthC(); }
        public static void AnotherMethodNameWithSomeLengthC() { AnotherMethodNameWithSomeLengthD(); }
        public static void AnotherMethodNameWithSomeLengthD() { AnotherMethodNameWithSomeLengthE(); }
        public static void AnotherMethodNameWithSomeLengthE() { AnotherMethodNameWithSomeLengthF(); }
        public static void AnotherMethodNameWithSomeLengthF() { AnotherMethodNameWithSomeLengthG(); }
        public static void AnotherMethodNameWithSomeLengthG() { AnotherMethodNameWithSomeLengthH(); }
        public static void AnotherMethodNameWithSomeLengthH() { AnotherMethodNameWithSomeLengthI(); }
        public static void AnotherMethodNameWithSomeLengthI()
        {
            AmRunningOutOfCreativeWaysToSayIHaveLongClassNames.AlsoLackCreativityOnTheMethodA();
        }
    }

    internal class AmRunningOutOfCreativeWaysToSayIHaveLongClassNames
    {
        public static void AlsoLackCreativityOnTheMethodA() { AlsoLackCreativityOnTheMethodB(); }
        public static void AlsoLackCreativityOnTheMethodB() { AlsoLackCreativityOnTheMethodC(); }
        public static void AlsoLackCreativityOnTheMethodC() { AlsoLackCreativityOnTheMethodD(); }
        public static void AlsoLackCreativityOnTheMethodD() { AlsoLackCreativityOnTheMethodE(); }
        public static void AlsoLackCreativityOnTheMethodE() { AlsoLackCreativityOnTheMethodF(); }
        public static void AlsoLackCreativityOnTheMethodF() { AlsoLackCreativityOnTheMethodG(); }
        public static void AlsoLackCreativityOnTheMethodG() { AlsoLackCreativityOnTheMethodH(); }
        public static void AlsoLackCreativityOnTheMethodH() { AlsoLackCreativityOnTheMethodI(); }
        public static void AlsoLackCreativityOnTheMethodI()
        {
            OneFinalVeryLongClassNameThatHasSomeLengthToIt.OneFinalLongMethodNameA();
        }
    }

    internal class OneFinalVeryLongClassNameThatHasSomeLengthToIt
    {
        public static void OneFinalLongMethodNameA() { OneFinalLongMethodNameB(); }
        public static void OneFinalLongMethodNameB() { OneFinalLongMethodNameC(); }
        public static void OneFinalLongMethodNameC() { OneFinalLongMethodNameD(); }
        public static void OneFinalLongMethodNameD() { OneFinalLongMethodNameE(); }
        public static void OneFinalLongMethodNameE() { OneFinalLongMethodNameF(); }
        public static void OneFinalLongMethodNameF() { OneFinalLongMethodNameG(); }
        public static void OneFinalLongMethodNameG() { OneFinalLongMethodNameH(); }
        public static void OneFinalLongMethodNameH() { OneFinalLongMethodNameI(); }
        public static void OneFinalLongMethodNameI()
        {
            if (!Sample.UseThreads) { Console.WriteLine("."); }
            Thread.Sleep(2000);
        }
    }

    internal class Sample
    {
        public static bool UseThreads = true;

        public static void SomeInternalMethodA() { SomeInternalMethodB(); }
        public static void SomeInternalMethodB() { SomeInternalMethodC(); }
        public static void SomeInternalMethodC() { SomeInternalMethodD(); }
        public static void SomeInternalMethodD() { SomeInternalMethodE(); }
        public static void SomeInternalMethodE() { SomeInternalMethodF(); }
        public static void SomeInternalMethodF() { SomeInternalMethodG(); }
        public static void SomeInternalMethodG() { SomeInternalMethodH(); }
        public static void SomeInternalMethodH() { SomeInternalMethodI(); }
        public static void SomeInternalMethodI()
        {
            AVeryLongClassNameThatIsNotVeryRepresentativeButFulfillsMyRequirements.SomeInternalMethodA();
        }

        public static void Loop10Times()
        {
            for (var i = 0; i < 10; i++)
            {
                SomeInternalMethodA();
            }
        }

        public static async Task WebRequest()
        {
            try
            {
                var hc = new HttpClient();
                var message = await hc.GetAsync("https://www.google.com/");
                Console.WriteLine("made async request: " + message);

            }
            catch (Exception e)
            {
                Console.WriteLine("Got exception" + e);
            }
        }

        public static async Task Main(string[] args)
        {
            if (args.Length != 0)
            {
                Console.WriteLine("Single threaded");
                UseThreads = false;
            }

            await WebRequest();

            var numThreads = 200;
            if (!UseThreads)
            {
                Console.WriteLine("Hello");
                Loop10Times();
                Console.WriteLine("DONE!");
                return;
            }
            Console.WriteLine("Hello (threads)");
            var threads = new Thread[numThreads];
            for (var i = 0; i < numThreads; i++)
            {
                threads[i] = new Thread(Loop10Times)
                {
                    Name = "My Cool Thread " + i
                };
                threads[i].Start();
            }
            Console.WriteLine("All threads started");
            for (var i = 0; i < numThreads; i++)
            {
                threads[i].Join();
            }
            Console.WriteLine("DONE");
        }
    }
}
