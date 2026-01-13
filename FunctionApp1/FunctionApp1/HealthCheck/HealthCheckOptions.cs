using System.ComponentModel.DataAnnotations;

namespace FunctionApp1.HealthCheck;

public sealed class HealthCheckOptions : IValidatableObject
{
    public const string SectionName = "HealthCheck";

    [Required]
    public string Schedule { get; set; } = string.Empty;

    [Required]
    public string TargetUrl { get; set; } = string.Empty;

    [Required]
    public string FallbackUrl { get; set; } = string.Empty;

    [Range(1, 300)]
    public int? TimeoutSeconds { get; set; }

    public void ApplyDefaults()
    {
        Schedule = string.IsNullOrWhiteSpace(Schedule) ? "0 */2 * * * *" : Schedule;
        TargetUrl = string.IsNullOrWhiteSpace(TargetUrl) ? "https://aspire.dev/" : TargetUrl;
        FallbackUrl = string.IsNullOrWhiteSpace(FallbackUrl) ? "https://github.com" : FallbackUrl;
    }

    public TimeSpan GetTimeout() => TimeSpan.FromSeconds(TimeoutSeconds ?? 5);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Schedule))
        {
            yield return new ValidationResult("HealthCheck:Schedule must be provided.", [nameof(Schedule)]);
        }

        if (!TryValidateAbsoluteUrl(TargetUrl, out _))
        {
            yield return new ValidationResult("HealthCheck:TargetUrl must be a valid absolute URL.", [nameof(TargetUrl)]);
        }

        if (!TryValidateAbsoluteUrl(FallbackUrl, out _))
        {
            yield return new ValidationResult("HealthCheck:FallbackUrl must be a valid absolute URL.", [nameof(FallbackUrl)]);
        }
    }

    internal static bool TryValidateAbsoluteUrl(string? value, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        uri = parsed;
        return true;
    }
}
