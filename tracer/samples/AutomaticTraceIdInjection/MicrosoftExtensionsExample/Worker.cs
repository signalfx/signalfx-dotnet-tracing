using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTracing.Util;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Obtain the automatically registered OpenTracing.Util.GlobalTracer instance
        var tracer = GlobalTracer.Instance;

        while (!stoppingToken.IsCancellationRequested)
        {
            using (_logger.BeginScope(new Dictionary<string, object>{{"order-number", 1024}}))
            {
                _logger.LogInformation("Message before a trace.");

                using (tracer.BuildSpan("Microsoft.Extensions.Example - Worker.ExecuteAsync()").StartActive(finishSpanOnDispose: true))
                {
                    _logger.LogInformation("Message during a trace.");

                    using (tracer.BuildSpan("Microsoft.Extensions.Example - Nested span").StartActive(finishSpanOnDispose: true))
                    {
                        _logger.LogInformation("Message during a child span.");
                    }
                }

                _logger.LogInformation("Message after a trace.");
            }

            await Task.Delay(1_000, stoppingToken);
        }
    }
}
