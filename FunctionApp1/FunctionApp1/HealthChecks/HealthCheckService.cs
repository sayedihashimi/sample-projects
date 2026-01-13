using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace FunctionApp1.HealthChecks;

public sealed class HealthCheckService
{
    public const string HttpClientName = "HealthCheck";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDbContextFactory<HealthCheckDbContext> _dbContextFactory;
    private readonly IOptions<HealthCheckOptions> _options;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        IHttpClientFactory httpClientFactory,
        IDbContextFactory<HealthCheckDbContext> dbContextFactory,
        IOptions<HealthCheckOptions> options,
        ILogger<HealthCheckService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _dbContextFactory = dbContextFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<HealthCheckResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var now = DateTimeOffset.UtcNow;
        var url = _options.Value.TargetUrl ?? HealthCheckOptions.DefaultTargetUrl;

        _logger.LogInformation("Health check starting. url={Url} ts={TimestampUtc}", url, now);

        HealthCheckResult result;

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(_options.Value.TimeoutSeconds ?? HealthCheckOptions.DefaultTimeoutSeconds));

            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);

            var isSuccess = response.IsSuccessStatusCode;
            result = new HealthCheckResult
            {
                CheckedUrl = url,
                TimestampUtc = now,
                IsSuccess = isSuccess,
                StatusCode = (int)response.StatusCode,
                ErrorMessage = isSuccess ? null : $"Non-success status code: {(int)response.StatusCode}"
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            result = new HealthCheckResult
            {
                CheckedUrl = url,
                TimestampUtc = now,
                IsSuccess = false,
                StatusCode = null,
                ErrorMessage = ex.Message
            };

            _logger.LogError(ex, "Health check failed. url={Url} ts={TimestampUtc}", url, now);
        }
        catch (OperationCanceledException ex)
        {
            result = new HealthCheckResult
            {
                CheckedUrl = url,
                TimestampUtc = now,
                IsSuccess = false,
                StatusCode = null,
                ErrorMessage = "Timed out or canceled: " + ex.Message
            };

            _logger.LogWarning(ex, "Health check canceled/timed out. url={Url} ts={TimestampUtc}", url, now);
        }

        await using (var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            db.Results.Add(result);
            await db.SaveChangesAsync(cancellationToken);
        }

        sw.Stop();
        _logger.LogInformation(
            "Health check finished. url={Url} success={Success} status={StatusCode} id={Id} durationMs={DurationMs}",
            result.CheckedUrl,
            result.IsSuccess,
            result.StatusCode,
            result.Id,
            sw.ElapsedMilliseconds);

        return result;
    }
}
