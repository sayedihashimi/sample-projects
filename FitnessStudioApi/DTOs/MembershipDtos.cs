using System.ComponentModel.DataAnnotations;

namespace FitnessStudioApi.DTOs;

/// <summary>Payload for purchasing/creating a membership.</summary>
public sealed record CreateMembershipRequest
{
    /// <summary>ID of the member.</summary>
    public required int MemberId { get; init; }

    /// <summary>ID of the membership plan.</summary>
    public required int MembershipPlanId { get; init; }

    /// <summary>Start date of the membership.</summary>
    public required DateOnly StartDate { get; init; }
}

/// <summary>Payload for freezing a membership.</summary>
public sealed record FreezeMembershipRequest
{
    /// <summary>Number of days to freeze (7–30).</summary>
    [Range(7, 30)]
    public required int FreezeDurationDays { get; init; }
}

/// <summary>Represents a membership returned by the API.</summary>
public sealed record MembershipResponse(
    int Id,
    int MemberId,
    string MemberName,
    int MembershipPlanId,
    string PlanName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    string PaymentStatus,
    DateOnly? FreezeStartDate,
    DateOnly? FreezeEndDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
