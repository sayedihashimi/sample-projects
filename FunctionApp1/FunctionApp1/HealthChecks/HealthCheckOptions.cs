using System.ComponentModel.DataAnnotations;

namespace FunctionApp1.HealthChecks;

public sealed class HealthCheckOptions
{
    public const string SectionName = "HealthCheck";

    public const string DefaultSchedule = "0 */1 * * * *";
    public const string DefaultTargetUrl = "https://aspire.dev/";
    public const int DefaultTimeoutSeconds = 5;

    public string? Schedule { get; set; }

    [Required]
    public string? TargetUrl { get; set; }

    [Range(1, 300)]
    public int? TimeoutSeconds { get; set; }
}
