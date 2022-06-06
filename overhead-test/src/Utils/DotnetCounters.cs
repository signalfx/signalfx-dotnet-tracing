using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace SignalFx.OverheadTest.Utils;

/// <summary>
///     Helper for starting counters collection inside docker container.
/// </summary>
internal class DotnetCounters
{
    private const string CountersResultsFile = "counters.json";
    private readonly ITestOutputHelper _outputHelper;

    public DotnetCounters(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
    }

    internal void StartCollecting(string containerName)
    {
        var args =
            $"exec {containerName} ./dotnet-counters collect --process-id 1 --refresh-interval 1 --format json --output /results/{CountersResultsFile}";
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false
        };

        _outputHelper.WriteLine("----------------Starting dotnet counters collection inside app container.");
        using var process = Process.Start(processStartInfo);

        using var helper = new ProcessHelper(process!);

        var waitTime = (int) TimeSpan.FromSeconds(5).TotalMilliseconds;
        process!.WaitForExit(waitTime);
        helper.Drain(waitTime);

        _outputHelper.WriteLine($"StdOut:{Environment.NewLine} {helper.StandardOutput}");
        _outputHelper.WriteLine($"StdErr:{Environment.NewLine} {helper.ErrorOutput}");
    }
}
