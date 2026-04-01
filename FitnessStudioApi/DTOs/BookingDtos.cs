using System.ComponentModel.DataAnnotations;
using FitnessStudioApi.Models;

namespace FitnessStudioApi.DTOs;

/// <summary>Payload for creating a booking.</summary>
public sealed record CreateBookingRequest
{
    /// <summary>ID of the class schedule to book.</summary>
    public required int ClassScheduleId { get; init; }

    /// <summary>ID of the member making the booking.</summary>
    public required int MemberId { get; init; }
}

/// <summary>Payload for cancelling a booking.</summary>
public sealed record CancelBookingRequest
{
    /// <summary>Reason for cancellation.</summary>
    [MaxLength(500)]
    public string? Reason { get; init; }
}

/// <summary>Represents a booking returned by the API.</summary>
public sealed record BookingResponse(
    int Id,
    int ClassScheduleId,
    string ClassName,
    string InstructorName,
    DateTimeOffset ClassStartTime,
    DateTimeOffset ClassEndTime,
    string Room,
    int MemberId,
    string MemberName,
    DateTimeOffset BookingDate,
    BookingStatus Status,
    int? WaitlistPosition,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CancellationDate,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
