Imports System.Threading

Public Class ClassVb
    Public Shared Sub MethodVb(testParam As String)
        Console.WriteLine("Thread.Sleep - starting " + testParam)
        Thread.Sleep(TimeSpan.FromSeconds(6))
        Console.WriteLine("Thread.Sleep - finished")
    End Sub
End Class
