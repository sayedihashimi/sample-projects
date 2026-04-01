using FitnessStudioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessStudioApi.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(FitnessDbContext db)
    {
        if (await db.MembershipPlans.AnyAsync())
            return;

        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Membership Plans
        var basicPlan = new MembershipPlan
        {
            Name = "Basic",
            Description = "Access to standard classes with limited weekly bookings",
            DurationMonths = 1,
            Price = 29.99m,
            MaxClassBookingsPerWeek = 3,
            AllowsPremiumClasses = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        var premiumPlan = new MembershipPlan
        {
            Name = "Premium",
            Description = "Access to all classes including premium with more weekly bookings",
            DurationMonths = 1,
            Price = 49.99m,
            MaxClassBookingsPerWeek = 5,
            AllowsPremiumClasses = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var elitePlan = new MembershipPlan
        {
            Name = "Elite",
            Description = "Unlimited access to all classes including premium",
            DurationMonths = 1,
            Price = 79.99m,
            MaxClassBookingsPerWeek = -1,
            AllowsPremiumClasses = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.MembershipPlans.AddRange(basicPlan, premiumPlan, elitePlan);
        await db.SaveChangesAsync();

        // Members
        var members = new List<Member>
        {
            new() { FirstName = "Alice", LastName = "Johnson", Email = "alice.johnson@email.com", Phone = "555-0101",
                DateOfBirth = new DateOnly(1990, 3, 15), EmergencyContactName = "Bob Johnson", EmergencyContactPhone = "555-0102",
                JoinDate = today.AddMonths(-6), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "Brian", LastName = "Smith", Email = "brian.smith@email.com", Phone = "555-0201",
                DateOfBirth = new DateOnly(1985, 7, 22), EmergencyContactName = "Carol Smith", EmergencyContactPhone = "555-0202",
                JoinDate = today.AddMonths(-4), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "Carmen", LastName = "Garcia", Email = "carmen.garcia@email.com", Phone = "555-0301",
                DateOfBirth = new DateOnly(1992, 11, 8), EmergencyContactName = "Diego Garcia", EmergencyContactPhone = "555-0302",
                JoinDate = today.AddMonths(-3), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "David", LastName = "Lee", Email = "david.lee@email.com", Phone = "555-0401",
                DateOfBirth = new DateOnly(1988, 1, 30), EmergencyContactName = "Emily Lee", EmergencyContactPhone = "555-0402",
                JoinDate = today.AddMonths(-5), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "Elena", LastName = "Patel", Email = "elena.patel@email.com", Phone = "555-0501",
                DateOfBirth = new DateOnly(1995, 5, 12), EmergencyContactName = "Raj Patel", EmergencyContactPhone = "555-0502",
                JoinDate = today.AddMonths(-2), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "Frank", LastName = "Wilson", Email = "frank.wilson@email.com", Phone = "555-0601",
                DateOfBirth = new DateOnly(1983, 9, 25), EmergencyContactName = "Grace Wilson", EmergencyContactPhone = "555-0602",
                JoinDate = today.AddMonths(-8), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "Grace", LastName = "Kim", Email = "grace.kim@email.com", Phone = "555-0701",
                DateOfBirth = new DateOnly(1998, 2, 14), EmergencyContactName = "Henry Kim", EmergencyContactPhone = "555-0702",
                JoinDate = today.AddMonths(-1), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "Henry", LastName = "Brown", Email = "henry.brown@email.com", Phone = "555-0801",
                DateOfBirth = new DateOnly(1991, 12, 3), EmergencyContactName = "Irene Brown", EmergencyContactPhone = "555-0802",
                JoinDate = today.AddMonths(-7), CreatedAt = now, UpdatedAt = now }
        };
        db.Members.AddRange(members);
        await db.SaveChangesAsync();

        // Memberships
        var memberships = new List<Membership>
        {
            // Alice - Basic Active
            new() { MemberId = members[0].Id, MembershipPlanId = basicPlan.Id,
                StartDate = today.AddMonths(-1), EndDate = today.AddMonths(0),
                Status = MembershipStatus.Active, PaymentStatus = PaymentStatus.Paid, CreatedAt = now, UpdatedAt = now },
            // Brian - Premium Active
            new() { MemberId = members[1].Id, MembershipPlanId = premiumPlan.Id,
                StartDate = today.AddDays(-15), EndDate = today.AddDays(15),
                Status = MembershipStatus.Active, PaymentStatus = PaymentStatus.Paid, CreatedAt = now, UpdatedAt = now },
            // Carmen - Elite Active
            new() { MemberId = members[2].Id, MembershipPlanId = elitePlan.Id,
                StartDate = today.AddDays(-10), EndDate = today.AddDays(20),
                Status = MembershipStatus.Active, PaymentStatus = PaymentStatus.Paid, CreatedAt = now, UpdatedAt = now },
            // David - Premium Active
            new() { MemberId = members[3].Id, MembershipPlanId = premiumPlan.Id,
                StartDate = today.AddDays(-20), EndDate = today.AddDays(10),
                Status = MembershipStatus.Active, PaymentStatus = PaymentStatus.Paid, CreatedAt = now, UpdatedAt = now },
            // Elena - Basic Active
            new() { MemberId = members[4].Id, MembershipPlanId = basicPlan.Id,
                StartDate = today.AddDays(-5), EndDate = today.AddDays(25),
                Status = MembershipStatus.Active, PaymentStatus = PaymentStatus.Paid, CreatedAt = now, UpdatedAt = now },
            // Frank - Elite Active
            new() { MemberId = members[5].Id, MembershipPlanId = elitePlan.Id,
                StartDate = today.AddDays(-25), EndDate = today.AddDays(5),
                Status = MembershipStatus.Active, PaymentStatus = PaymentStatus.Paid, CreatedAt = now, UpdatedAt = now },
            // Grace - Expired Basic
            new() { MemberId = members[6].Id, MembershipPlanId = basicPlan.Id,
                StartDate = today.AddMonths(-2), EndDate = today.AddMonths(-1),
                Status = MembershipStatus.Expired, PaymentStatus = PaymentStatus.Paid, CreatedAt = now, UpdatedAt = now },
            // Henry - Expired Premium
            new() { MemberId = members[7].Id, MembershipPlanId = premiumPlan.Id,
                StartDate = today.AddMonths(-3), EndDate = today.AddMonths(-2),
                Status = MembershipStatus.Expired, PaymentStatus = PaymentStatus.Paid, CreatedAt = now, UpdatedAt = now }
        };
        db.Memberships.AddRange(memberships);
        await db.SaveChangesAsync();

        // Instructors
        var instructors = new List<Instructor>
        {
            new() { FirstName = "Sarah", LastName = "Chen", Email = "sarah.chen@zenith.com", Phone = "555-1001",
                Bio = "Certified yoga instructor with 10 years of experience in Hatha and Vinyasa yoga.",
                Specializations = "Yoga, Pilates, Meditation", HireDate = new DateOnly(2020, 1, 15), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "Marcus", LastName = "Rivera", Email = "marcus.rivera@zenith.com", Phone = "555-1002",
                Bio = "Former competitive athlete specializing in high-intensity interval training and boxing.",
                Specializations = "HIIT, Boxing, Spin", HireDate = new DateOnly(2021, 6, 1), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "Priya", LastName = "Sharma", Email = "priya.sharma@zenith.com", Phone = "555-1003",
                Bio = "Pilates master trainer and certified rehabilitation specialist.",
                Specializations = "Pilates, Yoga", HireDate = new DateOnly(2019, 3, 10), CreatedAt = now, UpdatedAt = now },
            new() { FirstName = "James", LastName = "O'Brien", Email = "james.obrien@zenith.com", Phone = "555-1004",
                Bio = "Spinning and HIIT coach with a background in competitive cycling.",
                Specializations = "Spin, HIIT, Boxing", HireDate = new DateOnly(2022, 9, 20), CreatedAt = now, UpdatedAt = now }
        };
        db.Instructors.AddRange(instructors);
        await db.SaveChangesAsync();

        // Class Types
        var classTypes = new List<ClassType>
        {
            new() { Name = "Yoga", Description = "Traditional yoga practice focusing on flexibility and mindfulness",
                DefaultDurationMinutes = 60, DefaultCapacity = 20, IsPremium = false, CaloriesPerSession = 250,
                DifficultyLevel = DifficultyLevel.AllLevels, CreatedAt = now, UpdatedAt = now },
            new() { Name = "HIIT", Description = "High-intensity interval training for maximum calorie burn",
                DefaultDurationMinutes = 45, DefaultCapacity = 15, IsPremium = false, CaloriesPerSession = 500,
                DifficultyLevel = DifficultyLevel.Advanced, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Spin", Description = "Indoor cycling class with music-driven intensity",
                DefaultDurationMinutes = 45, DefaultCapacity = 20, IsPremium = false, CaloriesPerSession = 450,
                DifficultyLevel = DifficultyLevel.Intermediate, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Pilates", Description = "Core-strengthening and body conditioning exercises",
                DefaultDurationMinutes = 55, DefaultCapacity = 15, IsPremium = false, CaloriesPerSession = 300,
                DifficultyLevel = DifficultyLevel.Beginner, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Boxing", Description = "Premium boxing fitness class with personal attention",
                DefaultDurationMinutes = 60, DefaultCapacity = 10, IsPremium = true, CaloriesPerSession = 600,
                DifficultyLevel = DifficultyLevel.Advanced, CreatedAt = now, UpdatedAt = now },
            new() { Name = "Meditation", Description = "Premium guided meditation and mindfulness practice",
                DefaultDurationMinutes = 30, DefaultCapacity = 12, IsPremium = true, CaloriesPerSession = 50,
                DifficultyLevel = DifficultyLevel.Beginner, CreatedAt = now, UpdatedAt = now }
        };
        db.ClassTypes.AddRange(classTypes);
        await db.SaveChangesAsync();

        // Class Schedules (12+ over next 7 days)
        var tomorrow = today.AddDays(1);
        var schedules = new List<ClassSchedule>
        {
            // Day 1 - tomorrow
            new() { ClassTypeId = classTypes[0].Id, InstructorId = instructors[0].Id,
                StartTime = new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(7, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
                Capacity = 20, Room = "Studio A", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },
            new() { ClassTypeId = classTypes[1].Id, InstructorId = instructors[1].Id,
                StartTime = new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(9, 45)), TimeSpan.Zero),
                Capacity = 15, Room = "Main Floor", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },
            new() { ClassTypeId = classTypes[2].Id, InstructorId = instructors[3].Id,
                StartTime = new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(10, 45)), TimeSpan.Zero),
                Capacity = 20, Room = "Studio B", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },

            // Day 2
            new() { ClassTypeId = classTypes[3].Id, InstructorId = instructors[2].Id,
                StartTime = new DateTimeOffset(tomorrow.AddDays(1).ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.AddDays(1).ToDateTime(new TimeOnly(8, 55)), TimeSpan.Zero),
                Capacity = 15, Room = "Studio A", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },
            new() { ClassTypeId = classTypes[4].Id, InstructorId = instructors[1].Id,
                StartTime = new DateTimeOffset(tomorrow.AddDays(1).ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.AddDays(1).ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero),
                Capacity = 3, Room = "Studio B", Status = ClassScheduleStatus.Scheduled, CurrentEnrollment = 3, WaitlistCount = 2,
                CreatedAt = now, UpdatedAt = now },
            new() { ClassTypeId = classTypes[5].Id, InstructorId = instructors[0].Id,
                StartTime = new DateTimeOffset(tomorrow.AddDays(1).ToDateTime(new TimeOnly(17, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.AddDays(1).ToDateTime(new TimeOnly(17, 30)), TimeSpan.Zero),
                Capacity = 12, Room = "Studio A", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },

            // Day 3
            new() { ClassTypeId = classTypes[0].Id, InstructorId = instructors[2].Id,
                StartTime = new DateTimeOffset(tomorrow.AddDays(2).ToDateTime(new TimeOnly(7, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.AddDays(2).ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
                Capacity = 20, Room = "Studio A", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },
            new() { ClassTypeId = classTypes[1].Id, InstructorId = instructors[3].Id,
                StartTime = new DateTimeOffset(tomorrow.AddDays(2).ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.AddDays(2).ToDateTime(new TimeOnly(12, 45)), TimeSpan.Zero),
                Capacity = 15, Room = "Main Floor", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },

            // Day 4
            new() { ClassTypeId = classTypes[2].Id, InstructorId = instructors[3].Id,
                StartTime = new DateTimeOffset(tomorrow.AddDays(3).ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.AddDays(3).ToDateTime(new TimeOnly(9, 45)), TimeSpan.Zero),
                Capacity = 20, Room = "Studio B", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },
            new() { ClassTypeId = classTypes[3].Id, InstructorId = instructors[2].Id,
                StartTime = new DateTimeOffset(tomorrow.AddDays(3).ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.AddDays(3).ToDateTime(new TimeOnly(14, 55)), TimeSpan.Zero),
                Capacity = 15, Room = "Studio A", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },

            // Day 5
            new() { ClassTypeId = classTypes[4].Id, InstructorId = instructors[1].Id,
                StartTime = new DateTimeOffset(tomorrow.AddDays(4).ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.AddDays(4).ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero),
                Capacity = 10, Room = "Studio B", Status = ClassScheduleStatus.Scheduled, CreatedAt = now, UpdatedAt = now },

            // Day 6 - cancelled class
            new() { ClassTypeId = classTypes[0].Id, InstructorId = instructors[0].Id,
                StartTime = new DateTimeOffset(tomorrow.AddDays(5).ToDateTime(new TimeOnly(7, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(tomorrow.AddDays(5).ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
                Capacity = 20, Room = "Studio A", Status = ClassScheduleStatus.Cancelled,
                CancellationReason = "Instructor unavailable", CreatedAt = now, UpdatedAt = now },

            // Past class (yesterday, completed)
            new() { ClassTypeId = classTypes[0].Id, InstructorId = instructors[0].Id,
                StartTime = new DateTimeOffset(today.AddDays(-1).ToDateTime(new TimeOnly(7, 0)), TimeSpan.Zero),
                EndTime = new DateTimeOffset(today.AddDays(-1).ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero),
                Capacity = 20, CurrentEnrollment = 5, Room = "Studio A", Status = ClassScheduleStatus.Completed,
                CreatedAt = now, UpdatedAt = now }
        };
        db.ClassSchedules.AddRange(schedules);
        await db.SaveChangesAsync();

        // Bookings (15+ in various states)
        var bookings = new List<Booking>
        {
            // Confirmed bookings for tomorrow's Yoga (schedule[0])
            new() { ClassScheduleId = schedules[0].Id, MemberId = members[0].Id,
                BookingDate = now.AddHours(-12), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },
            new() { ClassScheduleId = schedules[0].Id, MemberId = members[1].Id,
                BookingDate = now.AddHours(-10), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },
            new() { ClassScheduleId = schedules[0].Id, MemberId = members[2].Id,
                BookingDate = now.AddHours(-8), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },

            // Confirmed bookings for Boxing (schedule[4]) - at capacity
            new() { ClassScheduleId = schedules[4].Id, MemberId = members[1].Id,
                BookingDate = now.AddHours(-24), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },
            new() { ClassScheduleId = schedules[4].Id, MemberId = members[2].Id,
                BookingDate = now.AddHours(-22), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },
            new() { ClassScheduleId = schedules[4].Id, MemberId = members[3].Id,
                BookingDate = now.AddHours(-20), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },
            // Waitlisted for Boxing
            new() { ClassScheduleId = schedules[4].Id, MemberId = members[5].Id,
                BookingDate = now.AddHours(-18), Status = BookingStatus.Waitlisted, WaitlistPosition = 1, CreatedAt = now, UpdatedAt = now },
            new() { ClassScheduleId = schedules[4].Id, MemberId = members[4].Id,
                BookingDate = now.AddHours(-16), Status = BookingStatus.Waitlisted, WaitlistPosition = 2, CreatedAt = now, UpdatedAt = now },

            // Confirmed for HIIT (schedule[1])
            new() { ClassScheduleId = schedules[1].Id, MemberId = members[3].Id,
                BookingDate = now.AddHours(-6), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },
            new() { ClassScheduleId = schedules[1].Id, MemberId = members[5].Id,
                BookingDate = now.AddHours(-5), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },

            // Cancelled booking
            new() { ClassScheduleId = schedules[2].Id, MemberId = members[0].Id,
                BookingDate = now.AddDays(-1), Status = BookingStatus.Cancelled,
                CancellationDate = now.AddHours(-3), CancellationReason = "Schedule conflict",
                CreatedAt = now, UpdatedAt = now },

            // Attended bookings (yesterday's completed class)
            new() { ClassScheduleId = schedules[12].Id, MemberId = members[0].Id,
                BookingDate = now.AddDays(-2), Status = BookingStatus.Attended,
                CheckInTime = now.AddDays(-1).AddHours(-17), CreatedAt = now, UpdatedAt = now },
            new() { ClassScheduleId = schedules[12].Id, MemberId = members[1].Id,
                BookingDate = now.AddDays(-2), Status = BookingStatus.Attended,
                CheckInTime = now.AddDays(-1).AddHours(-17).AddMinutes(5), CreatedAt = now, UpdatedAt = now },

            // No-show booking
            new() { ClassScheduleId = schedules[12].Id, MemberId = members[3].Id,
                BookingDate = now.AddDays(-2), Status = BookingStatus.NoShow, CreatedAt = now, UpdatedAt = now },

            // More confirmed for upcoming classes
            new() { ClassScheduleId = schedules[3].Id, MemberId = members[2].Id,
                BookingDate = now.AddHours(-4), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },
            new() { ClassScheduleId = schedules[5].Id, MemberId = members[2].Id,
                BookingDate = now.AddHours(-3), Status = BookingStatus.Confirmed, CreatedAt = now, UpdatedAt = now },
        };

        // Update enrollment counts
        schedules[0].CurrentEnrollment = 3;
        schedules[1].CurrentEnrollment = 2;
        schedules[3].CurrentEnrollment = 1;
        schedules[5].CurrentEnrollment = 1;

        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync();
    }
}
