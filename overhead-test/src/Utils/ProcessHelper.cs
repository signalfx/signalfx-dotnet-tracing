// <copyright file="ProcessHelper.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignalFx.OverheadTest.Utils;

/// <summary>
///     Drains the standard and error output of a process
/// </summary>
internal class ProcessHelper : IDisposable
{
    private readonly StringBuilder _errorBuffer = new();
    private readonly TaskCompletionSource<bool> _errorTask = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly StringBuilder _outputBuffer = new();
    private readonly TaskCompletionSource<bool> _outputTask = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ProcessHelper(Process process)
    {
        Task.WhenAll(_outputTask.Task, _errorTask.Task);
        process.OutputDataReceived += (_, e) => DrainOutput(e, _outputBuffer, _outputTask);
        process.ErrorDataReceived += (_, e) => DrainOutput(e, _errorBuffer, _errorTask);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        Process = process;
    }

    private Process Process { get; }

    public string StandardOutput => _outputBuffer.ToString();

    public string ErrorOutput => _errorBuffer.ToString();

    public virtual void Dispose()
    {
        if (!Process.HasExited) Process.Kill();
    }

    public bool Drain(int timeout = Timeout.Infinite)
    {
        if (timeout != Timeout.Infinite) timeout /= 2;

        return _outputTask.Task.Wait(timeout) && _errorTask.Task.Wait(timeout);
    }

    private static void DrainOutput(DataReceivedEventArgs e, StringBuilder buffer, TaskCompletionSource<bool> tcs)
    {
        if (e.Data == null)
            tcs.TrySetResult(true);
        else
            buffer.AppendLine(e.Data);
    }
}
