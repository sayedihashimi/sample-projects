using System.ComponentModel.DataAnnotations;

namespace FitnessStudioApi.DTOs;

/// <summary>Payload for creating an instructor.</summary>
public sealed record CreateInstructorRequest
{
    /// <summary>Instructor's first name.</summary>
    [Required, MaxLength(100)]
    public required string FirstName { get; init; }

    /// <summary>Instructor's last name.</summary>
    [Required, MaxLength(100)]
    public required string LastName { get; init; }

    /// <summary>Email address (must be unique).</summary>
    [Required, EmailAddress]
    public required string Email { get; init; }

    /// <summary>Phone number.</summary>
    [Required]
    public required string Phone { get; init; }

    /// <summary>Biography (max 1000 characters).</summary>
    [MaxLength(1000)]
    public string? Bio { get; init; }

    /// <summary>Comma-separated specializations (e.g., "Yoga, Pilates").</summary>
    public string? Specializations { get; init; }

    /// <summary>Date the instructor was hired.</summary>
    public required DateOnly HireDate { get; init; }
}

/// <summary>Payload for updating an instructor.</summary>
public sealed record UpdateInstructorRequest
{
    /// <summary>Instructor's first name.</summary>
    [Required, MaxLength(100)]
    public required string FirstName { get; init; }

    /// <summary>Instructor's last name.</summary>
    [Required, MaxLength(100)]
    public required string LastName { get; init; }

    /// <summary>Email address.</summary>
    [Required, EmailAddress]
    public required string Email { get; init; }

    /// <summary>Phone number.</summary>
    [Required]
    public required string Phone { get; init; }

    /// <summary>Biography.</summary>
    [MaxLength(1000)]
    public string? Bio { get; init; }

    /// <summary>Comma-separated specializations.</summary>
    public string? Specializations { get; init; }
}

/// <summary>Represents an instructor returned by the API.</summary>
public sealed record InstructorResponse(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string? Bio,
    string? Specializations,
    DateOnly HireDate,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
