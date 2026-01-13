namespace FunctionApp1.HealthChecks;

public sealed class HealthCheckResult
{
    public int Id { get; set; }

    public required string CheckedUrl { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    public bool IsSuccess { get; set; }

    public int? StatusCode { get; set; }

    public string? ErrorMessage { get; set; }
}
