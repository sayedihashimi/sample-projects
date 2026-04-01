using System.ComponentModel.DataAnnotations;

namespace FitnessStudioApi.DTOs;

/// <summary>Payload for creating a new membership plan.</summary>
public sealed record CreateMembershipPlanRequest
{
    /// <summary>Plan name (e.g., "Basic", "Premium", "Elite").</summary>
    [Required, MaxLength(100)]
    public required string Name { get; init; }

    /// <summary>Optional plan description.</summary>
    [MaxLength(500)]
    public string? Description { get; init; }

    /// <summary>Duration of the plan in months (1–24).</summary>
    [Range(1, 24)]
    public required int DurationMonths { get; init; }

    /// <summary>Monthly price (must be positive).</summary>
    [Range(0.01, double.MaxValue)]
    public required decimal Price { get; init; }

    /// <summary>Max class bookings per week (-1 for unlimited).</summary>
    public required int MaxClassBookingsPerWeek { get; init; }

    /// <summary>Whether this plan grants access to premium classes.</summary>
    public required bool AllowsPremiumClasses { get; init; }
}

/// <summary>Payload for updating a membership plan.</summary>
public sealed record UpdateMembershipPlanRequest
{
    /// <summary>Plan name.</summary>
    [Required, MaxLength(100)]
    public required string Name { get; init; }

    /// <summary>Optional plan description.</summary>
    [MaxLength(500)]
    public string? Description { get; init; }

    /// <summary>Duration in months (1–24).</summary>
    [Range(1, 24)]
    public required int DurationMonths { get; init; }

    /// <summary>Monthly price.</summary>
    [Range(0.01, double.MaxValue)]
    public required decimal Price { get; init; }

    /// <summary>Max class bookings per week (-1 for unlimited).</summary>
    public required int MaxClassBookingsPerWeek { get; init; }

    /// <summary>Whether this plan grants access to premium classes.</summary>
    public required bool AllowsPremiumClasses { get; init; }
}

/// <summary>Represents a membership plan returned by the API.</summary>
public sealed record MembershipPlanResponse(
    int Id,
    string Name,
    string? Description,
    int DurationMonths,
    decimal Price,
    int MaxClassBookingsPerWeek,
    bool AllowsPremiumClasses,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
