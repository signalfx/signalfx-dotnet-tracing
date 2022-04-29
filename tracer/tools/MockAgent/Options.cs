using CommandLine;

namespace MockAgent
{
    public class Options
    {
        public static string TempPath = Path.GetTempPath();

        public static readonly string DefaultPipesTrace = "apm.mock.windows.pipe";
        public static readonly string DefaultPipesStats = "dsd.mock.windows.pipe";

        public static readonly int DefaultPortTrace = 11126;
        public static readonly int DefaultPortStats = 11125;

        [Option('w', "wnp", Required = false, HelpText = "Receive traces and stats over windows named pipes.")]
        public bool WindowsNamedPipe { get; set; }

        [Option("trace-pipe-name", Required = false, HelpText = "Set the windows named pipe for traces.")]
        public string TracesPipeName { get; set; } = DefaultPipesTrace;

        [Option("stats-pipe-name", Required = false, HelpText = "Set the windows named pipe for metrics.")]
        public string MetricsPipeName { get; set; } = DefaultPipesStats;

        [Option('t', "tcp", Required = false, HelpText = "Receive traces and stats over TCP")]
        public bool Tcp { get; set; }

        [Option("trace-port", Required = false, HelpText = "Set the TCP port for traces.")]
        public int TracesPort { get; set; } = DefaultPortTrace;

        [Option("stats-port", Required = false, HelpText = "Set the UDP port for metrics.")]
        public int MetricsPort { get; set; } = DefaultPortStats;
    }
}
