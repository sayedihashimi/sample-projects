using System.Diagnostics;
using System.Net;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunctionApp1.HealthCheck;

public sealed class HealthCheckService
{
    public const string HttpClientName = "HealthCheck";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<HealthCheckOptions> _options;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        IHttpClientFactory httpClientFactory,
        IOptions<HealthCheckOptions> options,
        ILogger<HealthCheckService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var opts = _options.Value;
        var start = Stopwatch.GetTimestamp();

        _logger.LogInformation("Health check run starting. TargetUrl={TargetUrl}", opts.TargetUrl);

        HttpStatusCode? statusCode = null;
        string? error = null;

        try
        {
            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            httpClient.Timeout = opts.GetTimeout();

            using var request = new HttpRequestMessage(HttpMethod.Get, opts.TargetUrl);
            using var response = await httpClient.SendAsync(request, cancellationToken);

            statusCode = response.StatusCode;

            if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            {
                var elapsed = Stopwatch.GetElapsedTime(start);
                _logger.LogInformation("Health check succeeded. StatusCode={StatusCode} DurationMs={DurationMs}", (int)response.StatusCode, elapsed.TotalMilliseconds);
                return;
            }

            error = $"Non-success status code: {(int)response.StatusCode} ({response.ReasonPhrase})";
            _logger.LogError("Health check failed. CheckedUrl={CheckedUrl} StatusCode={StatusCode} Reason={Reason}", opts.TargetUrl, (int)response.StatusCode, response.ReasonPhrase);
        }
        catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
        {
            error = "Request timed out.";
            _logger.LogError(oce, "Health check timed out. CheckedUrl={CheckedUrl} TimeoutSeconds={TimeoutSeconds}", opts.TargetUrl, opts.GetTimeout().TotalSeconds);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _logger.LogError(ex, "Health check failed with exception. CheckedUrl={CheckedUrl}", opts.TargetUrl);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            _logger.LogInformation("Health check run completed. DurationMs={DurationMs}", elapsed.TotalMilliseconds);
        }

        await TryNotifyFallbackAsync(opts, statusCode, error, cancellationToken);
    }

    private async Task TryNotifyFallbackAsync(
        HealthCheckOptions options,
        HttpStatusCode? statusCode,
        string? error,
        CancellationToken cancellationToken)
    {
        try
        {
            var timestampUtc = DateTimeOffset.UtcNow;

            var fallbackUri = BuildFallbackUri(options, statusCode, error, timestampUtc);

            _logger.LogInformation(
                "Attempting fallback GET. FallbackUrl={FallbackUrl} CheckedUrl={CheckedUrl} StatusCode={StatusCode}",
                options.FallbackUrl,
                options.TargetUrl,
                statusCode.HasValue ? (int)statusCode.Value : null);

            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            httpClient.Timeout = options.GetTimeout();

            using var request = new HttpRequestMessage(HttpMethod.Get, fallbackUri);
            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Fallback GET succeeded. StatusCode={StatusCode}", (int)response.StatusCode);
            }
            else
            {
                _logger.LogError("Fallback GET failed. StatusCode={StatusCode} Reason={Reason}", (int)response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (OperationCanceledException oce) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(oce, "Fallback GET timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback GET failed with exception.");
        }
    }

    private static Uri BuildFallbackUri(
        HealthCheckOptions options,
        HttpStatusCode? statusCode,
        string? error,
        DateTimeOffset timestampUtc)
    {
        var baseUri = new Uri(options.FallbackUrl, UriKind.Absolute);

        var separator = string.IsNullOrEmpty(baseUri.Query) ? "?" : "&";

        string Encode(string v) => UrlEncoder.Default.Encode(v);

        var query = string.Join("&",
            $"checkedUrl={Encode(options.TargetUrl)}",
            $"timestampUtc={Encode(timestampUtc.ToString("O"))}",
            $"statusCode={Encode(statusCode.HasValue ? ((int)statusCode.Value).ToString() : string.Empty)}",
            $"error={Encode(error ?? string.Empty)}");

        return new Uri(baseUri + separator + query);
    }
}
