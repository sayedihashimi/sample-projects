namespace FitnessStudioApi.Models;

public enum MembershipStatus
{
    Active,
    Expired,
    Cancelled,
    Frozen
}

public enum PaymentStatus
{
    Paid,
    Pending,
    Refunded
}

public enum DifficultyLevel
{
    Beginner,
    Intermediate,
    Advanced,
    AllLevels
}

public enum ClassScheduleStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public enum BookingStatus
{
    Confirmed,
    Waitlisted,
    Cancelled,
    Attended,
    NoShow
}
