using System.ComponentModel.DataAnnotations;

namespace FitnessStudioApi.DTOs;

/// <summary>Payload for registering a new member.</summary>
public sealed record CreateMemberRequest
{
    /// <summary>Member's first name.</summary>
    [Required, MaxLength(100)]
    public required string FirstName { get; init; }

    /// <summary>Member's last name.</summary>
    [Required, MaxLength(100)]
    public required string LastName { get; init; }

    /// <summary>Email address (must be unique).</summary>
    [Required, EmailAddress]
    public required string Email { get; init; }

    /// <summary>Phone number.</summary>
    [Required]
    public required string Phone { get; init; }

    /// <summary>Date of birth (must be at least 16 years old).</summary>
    public required DateOnly DateOfBirth { get; init; }

    /// <summary>Emergency contact name.</summary>
    [Required, MaxLength(200)]
    public required string EmergencyContactName { get; init; }

    /// <summary>Emergency contact phone number.</summary>
    [Required]
    public required string EmergencyContactPhone { get; init; }
}

/// <summary>Payload for updating a member profile.</summary>
public sealed record UpdateMemberRequest
{
    /// <summary>Member's first name.</summary>
    [Required, MaxLength(100)]
    public required string FirstName { get; init; }

    /// <summary>Member's last name.</summary>
    [Required, MaxLength(100)]
    public required string LastName { get; init; }

    /// <summary>Email address (must be unique).</summary>
    [Required, EmailAddress]
    public required string Email { get; init; }

    /// <summary>Phone number.</summary>
    [Required]
    public required string Phone { get; init; }

    /// <summary>Date of birth.</summary>
    public required DateOnly DateOfBirth { get; init; }

    /// <summary>Emergency contact name.</summary>
    [Required, MaxLength(200)]
    public required string EmergencyContactName { get; init; }

    /// <summary>Emergency contact phone number.</summary>
    [Required]
    public required string EmergencyContactPhone { get; init; }
}

/// <summary>Represents a member returned by the API.</summary>
public sealed record MemberResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    string EmergencyContactName,
    string EmergencyContactPhone,
    DateOnly JoinDate,
    bool IsActive,
    MembershipSummary? ActiveMembership,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Summary of a member's active membership.</summary>
public sealed record MembershipSummary(
    int Id,
    string PlanName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status);

/// <summary>Represents a member in a list response.</summary>
public sealed record MemberListResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    bool IsActive,
    DateOnly JoinDate);
