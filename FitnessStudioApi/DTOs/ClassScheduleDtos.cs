using System.ComponentModel.DataAnnotations;
using FitnessStudioApi.Models;

namespace FitnessStudioApi.DTOs;

/// <summary>Payload for scheduling a new class.</summary>
public sealed record CreateClassScheduleRequest
{
    /// <summary>ID of the class type.</summary>
    public required int ClassTypeId { get; init; }

    /// <summary>ID of the instructor.</summary>
    public required int InstructorId { get; init; }

    /// <summary>Class start time.</summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>Class end time.</summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>Maximum capacity (1–50).</summary>
    [Range(1, 50)]
    public required int Capacity { get; init; }

    /// <summary>Room name (e.g., "Studio A").</summary>
    [Required, MaxLength(50)]
    public required string Room { get; init; }
}

/// <summary>Payload for updating a class schedule.</summary>
public sealed record UpdateClassScheduleRequest
{
    /// <summary>ID of the class type.</summary>
    public required int ClassTypeId { get; init; }

    /// <summary>ID of the instructor.</summary>
    public required int InstructorId { get; init; }

    /// <summary>Class start time.</summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>Class end time.</summary>
    public required DateTimeOffset EndTime { get; init; }

    /// <summary>Maximum capacity (1–50).</summary>
    [Range(1, 50)]
    public required int Capacity { get; init; }

    /// <summary>Room name.</summary>
    [Required, MaxLength(50)]
    public required string Room { get; init; }
}

/// <summary>Payload for cancelling a class.</summary>
public sealed record CancelClassRequest
{
    /// <summary>Reason for cancellation.</summary>
    [MaxLength(500)]
    public string? Reason { get; init; }
}

/// <summary>Represents a class schedule returned by the API.</summary>
public sealed record ClassScheduleResponse(
    int Id,
    int ClassTypeId,
    string ClassTypeName,
    int InstructorId,
    string InstructorName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int Capacity,
    int CurrentEnrollment,
    int WaitlistCount,
    int AvailableSpots,
    string Room,
    ClassScheduleStatus Status,
    string? CancellationReason,
    bool IsPremium,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Represents a member on a class roster.</summary>
public sealed record RosterEntryResponse(
    int BookingId,
    int MemberId,
    string MemberName,
    string Status,
    DateTimeOffset BookingDate,
    DateTimeOffset? CheckInTime);

/// <summary>Represents a member on a class waitlist.</summary>
public sealed record WaitlistEntryResponse(
    int BookingId,
    int MemberId,
    string MemberName,
    int? WaitlistPosition,
    DateTimeOffset BookingDate);
