using System.ComponentModel.DataAnnotations;
using FitnessStudioApi.Models;

namespace FitnessStudioApi.DTOs;

/// <summary>Payload for creating a class type.</summary>
public sealed record CreateClassTypeRequest
{
    /// <summary>Class type name (e.g., "Yoga", "HIIT").</summary>
    [Required, MaxLength(100)]
    public required string Name { get; init; }

    /// <summary>Optional description.</summary>
    [MaxLength(500)]
    public string? Description { get; init; }

    /// <summary>Default duration in minutes (30–120).</summary>
    [Range(30, 120)]
    public required int DefaultDurationMinutes { get; init; }

    /// <summary>Default class capacity (1–50).</summary>
    [Range(1, 50)]
    public required int DefaultCapacity { get; init; }

    /// <summary>Whether this is a premium class type.</summary>
    public required bool IsPremium { get; init; }

    /// <summary>Estimated calories burned per session.</summary>
    public int? CaloriesPerSession { get; init; }

    /// <summary>Difficulty level of the class.</summary>
    public required DifficultyLevel DifficultyLevel { get; init; }
}

/// <summary>Payload for updating a class type.</summary>
public sealed record UpdateClassTypeRequest
{
    /// <summary>Class type name.</summary>
    [Required, MaxLength(100)]
    public required string Name { get; init; }

    /// <summary>Optional description.</summary>
    [MaxLength(500)]
    public string? Description { get; init; }

    /// <summary>Default duration in minutes (30–120).</summary>
    [Range(30, 120)]
    public required int DefaultDurationMinutes { get; init; }

    /// <summary>Default class capacity (1–50).</summary>
    [Range(1, 50)]
    public required int DefaultCapacity { get; init; }

    /// <summary>Whether this is a premium class type.</summary>
    public required bool IsPremium { get; init; }

    /// <summary>Estimated calories burned per session.</summary>
    public int? CaloriesPerSession { get; init; }

    /// <summary>Difficulty level of the class.</summary>
    public required DifficultyLevel DifficultyLevel { get; init; }
}

/// <summary>Represents a class type returned by the API.</summary>
public sealed record ClassTypeResponse(
    int Id,
    string Name,
    string? Description,
    int DefaultDurationMinutes,
    int DefaultCapacity,
    bool IsPremium,
    int? CaloriesPerSession,
    DifficultyLevel DifficultyLevel,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
