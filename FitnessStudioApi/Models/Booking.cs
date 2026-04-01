namespace FitnessStudioApi.Models;

public class Booking
{
    public int Id { get; set; }
    public int ClassScheduleId { get; set; }
    public int MemberId { get; set; }
    public DateTimeOffset BookingDate { get; set; }
    public BookingStatus Status { get; set; }
    public int? WaitlistPosition { get; set; }
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CancellationDate { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ClassSchedule ClassSchedule { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
