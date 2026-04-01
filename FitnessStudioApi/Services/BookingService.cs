using System.Globalization;
using FitnessStudioApi.Data;
using FitnessStudioApi.DTOs;
using FitnessStudioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessStudioApi.Services;

public sealed class BookingService(FitnessDbContext db, ILogger<BookingService> logger) : IBookingService
{
    public async Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken ct)
    {
        var member = await db.Members.FindAsync([request.MemberId], ct)
            ?? throw new KeyNotFoundException($"Member with ID {request.MemberId} not found.");

        if (!member.IsActive)
            throw new ArgumentException("Inactive members cannot book classes.");

        var schedule = await db.ClassSchedules
            .Include(cs => cs.ClassType)
            .Include(cs => cs.Instructor)
            .FirstOrDefaultAsync(cs => cs.Id == request.ClassScheduleId, ct)
            ?? throw new KeyNotFoundException($"Class schedule with ID {request.ClassScheduleId} not found.");

        if (schedule.Status != ClassScheduleStatus.Scheduled)
            throw new ArgumentException($"Cannot book a class with status '{schedule.Status}'.");

        // Rule 6: Active membership required
        var activeMembership = await db.Memberships
            .Include(ms => ms.MembershipPlan)
            .FirstOrDefaultAsync(ms =>
                ms.MemberId == request.MemberId &&
                ms.Status == MembershipStatus.Active, ct)
            ?? throw new ArgumentException("Member does not have an active membership. Frozen, expired, or cancelled memberships cannot book classes.");

        // Rule 4: Premium class access
        if (schedule.ClassType.IsPremium && !activeMembership.MembershipPlan.AllowsPremiumClasses)
            throw new ArgumentException(
                $"Your '{activeMembership.MembershipPlan.Name}' plan does not allow booking premium classes. " +
                "Please upgrade to a Premium or Elite plan.");

        // Rule 1: Booking window (up to 7 days in advance, no less than 30 minutes before)
        var now = DateTimeOffset.UtcNow;
        if (schedule.StartTime > now.AddDays(7))
            throw new ArgumentException("Cannot book classes more than 7 days in advance.");

        if (schedule.StartTime <= now.AddMinutes(30))
            throw new ArgumentException("Cannot book a class less than 30 minutes before start time.");

        // Rule 5: Weekly booking limit
        var plan = activeMembership.MembershipPlan;
        if (plan.MaxClassBookingsPerWeek != -1)
        {
            var isoWeek = ISOWeek.GetWeekOfYear(DateTime.UtcNow);
            var isoYear = ISOWeek.GetYear(DateTime.UtcNow);
            var weekStart = ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday);
            var weekEnd = weekStart.AddDays(7);

            var weeklyBookings = await db.Bookings.CountAsync(b =>
                b.MemberId == request.MemberId &&
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Attended) &&
                b.ClassSchedule.StartTime >= new DateTimeOffset(weekStart, TimeSpan.Zero) &&
                b.ClassSchedule.StartTime < new DateTimeOffset(weekEnd, TimeSpan.Zero), ct);

            if (weeklyBookings >= plan.MaxClassBookingsPerWeek)
                throw new ArgumentException(
                    $"Weekly booking limit reached ({plan.MaxClassBookingsPerWeek} bookings per week for your {plan.Name} plan).");
        }

        // Rule 7: No double booking (overlap check)
        var hasOverlap = await db.Bookings.AnyAsync(b =>
            b.MemberId == request.MemberId &&
            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Attended) &&
            b.ClassSchedule.StartTime < schedule.EndTime &&
            b.ClassSchedule.EndTime > schedule.StartTime, ct);

        if (hasOverlap)
            throw new InvalidOperationException("Member already has a booking for an overlapping class.");

        // Determine status based on capacity
        BookingStatus status;
        int? waitlistPosition = null;

        if (schedule.CurrentEnrollment < schedule.Capacity)
        {
            status = BookingStatus.Confirmed;
            schedule.CurrentEnrollment++;
        }
        else
        {
            status = BookingStatus.Waitlisted;
            schedule.WaitlistCount++;
            waitlistPosition = schedule.WaitlistCount;
        }

        var booking = new Booking
        {
            ClassScheduleId = request.ClassScheduleId,
            MemberId = request.MemberId,
            BookingDate = now,
            Status = status,
            WaitlistPosition = waitlistPosition
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created booking {Id} for member {MemberId} in class {ClassId} with status {Status}",
            booking.Id, request.MemberId, request.ClassScheduleId, status);

        return MapToResponse(booking, schedule, member);
    }

    public async Task<BookingResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var booking = await db.Bookings
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.ClassType)
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.Instructor)
            .Include(b => b.Member)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        return booking is null ? null : MapToResponse(booking);
    }

    public async Task<BookingResponse> CancelAsync(int id, CancelBookingRequest request, CancellationToken ct)
    {
        var booking = await db.Bookings
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.ClassType)
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.Instructor)
            .Include(b => b.Member)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"Booking with ID {id} not found.");

        if (booking.Status == BookingStatus.Cancelled)
            throw new InvalidOperationException("Booking is already cancelled.");

        if (booking.Status == BookingStatus.Attended)
            throw new InvalidOperationException("Cannot cancel a booking after check-in.");

        if (booking.Status == BookingStatus.NoShow)
            throw new InvalidOperationException("Cannot cancel a no-show booking.");

        var schedule = booking.ClassSchedule;
        var now = DateTimeOffset.UtcNow;

        // Rule 3: Cannot cancel after class started or completed
        if (schedule.Status is ClassScheduleStatus.Completed or ClassScheduleStatus.InProgress)
            throw new InvalidOperationException("Cannot cancel a booking for a class that has started or completed.");

        // Determine cancellation type
        var reason = request.Reason ?? "";
        if (schedule.StartTime - now < TimeSpan.FromHours(2) && schedule.StartTime > now)
        {
            reason = string.IsNullOrWhiteSpace(reason)
                ? "Late cancellation (less than 2 hours before class)"
                : $"{reason} [Late cancellation]";
        }

        var wasConfirmed = booking.Status == BookingStatus.Confirmed;
        booking.Status = BookingStatus.Cancelled;
        booking.CancellationDate = now;
        booking.CancellationReason = reason;

        if (wasConfirmed)
        {
            schedule.CurrentEnrollment--;

            // Promote first waitlisted member
            var firstWaitlisted = await db.Bookings
                .Where(b => b.ClassScheduleId == schedule.Id && b.Status == BookingStatus.Waitlisted)
                .OrderBy(b => b.WaitlistPosition)
                .FirstOrDefaultAsync(ct);

            if (firstWaitlisted is not null)
            {
                firstWaitlisted.Status = BookingStatus.Confirmed;
                firstWaitlisted.WaitlistPosition = null;
                schedule.CurrentEnrollment++;
                schedule.WaitlistCount--;

                // Re-number remaining waitlist
                var remaining = await db.Bookings
                    .Where(b => b.ClassScheduleId == schedule.Id && b.Status == BookingStatus.Waitlisted)
                    .OrderBy(b => b.WaitlistPosition)
                    .ToListAsync(ct);

                for (int i = 0; i < remaining.Count; i++)
                {
                    remaining[i].WaitlistPosition = i + 1;
                }

                logger.LogInformation("Promoted waitlisted booking {WaitlistBookingId} to confirmed for class {ClassId}",
                    firstWaitlisted.Id, schedule.Id);
            }
        }
        else if (booking.Status == BookingStatus.Waitlisted)
        {
            schedule.WaitlistCount--;

            // Re-number remaining waitlist
            var remaining = await db.Bookings
                .Where(b => b.ClassScheduleId == schedule.Id &&
                            b.Status == BookingStatus.Waitlisted &&
                            b.Id != id)
                .OrderBy(b => b.WaitlistPosition)
                .ToListAsync(ct);

            for (int i = 0; i < remaining.Count; i++)
            {
                remaining[i].WaitlistPosition = i + 1;
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Cancelled booking {Id}", id);
        return MapToResponse(booking);
    }

    public async Task<BookingResponse> CheckInAsync(int id, CancellationToken ct)
    {
        var booking = await db.Bookings
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.ClassType)
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.Instructor)
            .Include(b => b.Member)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"Booking with ID {id} not found.");

        if (booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Cannot check in a booking with status '{booking.Status}'. Only confirmed bookings can be checked in.");

        var now = DateTimeOffset.UtcNow;
        var schedule = booking.ClassSchedule;

        // Rule 11: Check-in window is 15 minutes before to 15 minutes after class start
        if (now < schedule.StartTime.AddMinutes(-15))
            throw new ArgumentException("Check-in is not available yet. Check-in opens 15 minutes before class start.");

        if (now > schedule.StartTime.AddMinutes(15))
            throw new ArgumentException("Check-in window has closed (15 minutes after class start).");

        booking.Status = BookingStatus.Attended;
        booking.CheckInTime = now;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Member {MemberId} checked in for class {ClassId}", booking.MemberId, booking.ClassScheduleId);
        return MapToResponse(booking);
    }

    public async Task<BookingResponse> NoShowAsync(int id, CancellationToken ct)
    {
        var booking = await db.Bookings
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.ClassType)
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.Instructor)
            .Include(b => b.Member)
            .FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"Booking with ID {id} not found.");

        if (booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Cannot mark as no-show a booking with status '{booking.Status}'. Only confirmed bookings can be marked as no-show.");

        // Rule 12: Can be flagged as NoShow after 15 minutes past start time
        var now = DateTimeOffset.UtcNow;
        if (now < booking.ClassSchedule.StartTime.AddMinutes(15))
            throw new ArgumentException("Cannot mark as no-show until 15 minutes after class start time.");

        booking.Status = BookingStatus.NoShow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Marked booking {Id} as no-show", id);
        return MapToResponse(booking);
    }

    private static BookingResponse MapToResponse(Booking b) => new(
        b.Id, b.ClassScheduleId, b.ClassSchedule.ClassType.Name,
        $"{b.ClassSchedule.Instructor.FirstName} {b.ClassSchedule.Instructor.LastName}",
        b.ClassSchedule.StartTime, b.ClassSchedule.EndTime, b.ClassSchedule.Room,
        b.MemberId, $"{b.Member.FirstName} {b.Member.LastName}",
        b.BookingDate, b.Status, b.WaitlistPosition,
        b.CheckInTime, b.CancellationDate, b.CancellationReason,
        b.CreatedAt, b.UpdatedAt);

    private static BookingResponse MapToResponse(Booking b, ClassSchedule cs, Member m) => new(
        b.Id, b.ClassScheduleId, cs.ClassType.Name,
        $"{cs.Instructor.FirstName} {cs.Instructor.LastName}",
        cs.StartTime, cs.EndTime, cs.Room,
        b.MemberId, $"{m.FirstName} {m.LastName}",
        b.BookingDate, b.Status, b.WaitlistPosition,
        b.CheckInTime, b.CancellationDate, b.CancellationReason,
        b.CreatedAt, b.UpdatedAt);
}
