namespace FitnessStudioApi.Models;

public class Member
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public required string EmergencyContactName { get; set; }
    public required string EmergencyContactPhone { get; set; }
    public DateOnly JoinDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Membership> Memberships { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}
