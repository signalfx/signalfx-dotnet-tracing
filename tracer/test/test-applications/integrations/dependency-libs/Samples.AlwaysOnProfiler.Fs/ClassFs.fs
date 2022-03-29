namespace Samples.AlwaysOnProfiler.Fs

open System;
open System.Threading;

module ClassFs =
    let methodFs testParam =
        Console.WriteLine("Thread.Sleep - starting " + testParam)
        Thread.Sleep(TimeSpan.FromSeconds(6))
        Console.WriteLine("Thread.Sleep - finished")
