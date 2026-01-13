using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp1.HealthCheck;

public sealed class HealthCheckTimerFunction
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthCheckTimerFunction> _logger;

    public HealthCheckTimerFunction(HealthCheckService healthCheckService, ILogger<HealthCheckTimerFunction> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    [Function("HealthCheckTimer")]
    public async Task RunAsync(
        [TimerTrigger("%HealthCheck:Schedule%")]
        TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("HealthCheckTimer triggered. ScheduleStatus={ScheduleStatus}", timerInfo.ScheduleStatus);
        await _healthCheckService.RunAsync(cancellationToken);
    }
}
