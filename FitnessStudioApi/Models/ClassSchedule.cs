namespace FitnessStudioApi.Models;

public class ClassSchedule
{
    public int Id { get; set; }
    public int ClassTypeId { get; set; }
    public int InstructorId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public int Capacity { get; set; }
    public int CurrentEnrollment { get; set; }
    public int WaitlistCount { get; set; }
    public required string Room { get; set; }
    public ClassScheduleStatus Status { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ClassType ClassType { get; set; } = null!;
    public Instructor Instructor { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = [];
}
