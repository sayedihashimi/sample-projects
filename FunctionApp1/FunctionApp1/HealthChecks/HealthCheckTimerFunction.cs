using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FunctionApp1.HealthChecks;

public sealed class HealthCheckTimerFunction
{
    private readonly HealthCheckService _service;
    private readonly ILogger<HealthCheckTimerFunction> _logger;

    public HealthCheckTimerFunction(HealthCheckService service, ILogger<HealthCheckTimerFunction> logger)
    {
        _service = service;
        _logger = logger;
    }

    [Function(nameof(HealthCheckTimerFunction))]
    public async Task RunAsync(
        [TimerTrigger("%HealthCheck:Schedule%", RunOnStartup = true)] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Timer fired. scheduleStatusLast={Last} scheduleStatusNext={Next} isPastDue={PastDue}",
            timerInfo?.ScheduleStatus?.Last,
            timerInfo?.ScheduleStatus?.Next,
            timerInfo?.IsPastDue);

        await _service.RunOnceAsync(cancellationToken);

        sw.Stop();
        _logger.LogInformation("Timer run complete. durationMs={DurationMs}", sw.ElapsedMilliseconds);
    }
}
