using FitnessStudioApi.Data;
using FitnessStudioApi.DTOs;
using FitnessStudioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessStudioApi.Services;

public sealed class ClassScheduleService(FitnessDbContext db, ILogger<ClassScheduleService> logger) : IClassScheduleService
{
    public async Task<PaginatedList<ClassScheduleResponse>> GetAllAsync(
        DateOnly? fromDate, DateOnly? toDate, int? classTypeId, int? instructorId,
        bool? hasAvailability, int page, int pageSize, CancellationToken ct)
    {
        var query = db.ClassSchedules
            .Include(cs => cs.ClassType)
            .Include(cs => cs.Instructor)
            .AsNoTracking()
            .AsQueryable();

        if (classTypeId.HasValue)
            query = query.Where(cs => cs.ClassTypeId == classTypeId.Value);
        if (instructorId.HasValue)
            query = query.Where(cs => cs.InstructorId == instructorId.Value);
        if (fromDate.HasValue)
            query = query.Where(cs => cs.StartTime >= new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (toDate.HasValue)
            query = query.Where(cs => cs.StartTime <= new DateTimeOffset(toDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero));
        if (hasAvailability == true)
            query = query.Where(cs => cs.CurrentEnrollment < cs.Capacity && cs.Status == ClassScheduleStatus.Scheduled);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(cs => cs.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var responses = items.Select(MapToResponse).ToList();
        return new PaginatedList<ClassScheduleResponse>(responses, totalCount, page, pageSize);
    }

    public async Task<ClassScheduleResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var cs = await db.ClassSchedules
            .Include(c => c.ClassType)
            .Include(c => c.Instructor)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return cs is null ? null : MapToResponse(cs);
    }

    public async Task<ClassScheduleResponse> CreateAsync(CreateClassScheduleRequest request, CancellationToken ct)
    {
        var classType = await db.ClassTypes.FindAsync([request.ClassTypeId], ct)
            ?? throw new KeyNotFoundException($"Class type with ID {request.ClassTypeId} not found.");

        var instructor = await db.Instructors.FindAsync([request.InstructorId], ct)
            ?? throw new KeyNotFoundException($"Instructor with ID {request.InstructorId} not found.");

        if (!instructor.IsActive)
            throw new ArgumentException("Cannot assign an inactive instructor to a class.");

        if (request.EndTime <= request.StartTime)
            throw new ArgumentException("End time must be after start time.");

        // Check instructor schedule conflicts
        var hasConflict = await db.ClassSchedules.AnyAsync(cs =>
            cs.InstructorId == request.InstructorId &&
            cs.Status != ClassScheduleStatus.Cancelled &&
            cs.StartTime < request.EndTime &&
            cs.EndTime > request.StartTime, ct);

        if (hasConflict)
            throw new InvalidOperationException("Instructor has a schedule conflict during this time.");

        var schedule = new ClassSchedule
        {
            ClassTypeId = request.ClassTypeId,
            InstructorId = request.InstructorId,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Capacity = request.Capacity,
            Room = request.Room,
            Status = ClassScheduleStatus.Scheduled
        };

        db.ClassSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);

        // Reload with navigation properties
        await db.Entry(schedule).Reference(s => s.ClassType).LoadAsync(ct);
        await db.Entry(schedule).Reference(s => s.Instructor).LoadAsync(ct);

        logger.LogInformation("Scheduled class {ClassName} (ID: {Id}) at {StartTime}", classType.Name, schedule.Id, schedule.StartTime);
        return MapToResponse(schedule);
    }

    public async Task<ClassScheduleResponse> UpdateAsync(int id, UpdateClassScheduleRequest request, CancellationToken ct)
    {
        var schedule = await db.ClassSchedules
            .Include(cs => cs.ClassType)
            .Include(cs => cs.Instructor)
            .FirstOrDefaultAsync(cs => cs.Id == id, ct)
            ?? throw new KeyNotFoundException($"Class schedule with ID {id} not found.");

        if (schedule.Status == ClassScheduleStatus.Cancelled)
            throw new InvalidOperationException("Cannot update a cancelled class.");

        var classType = await db.ClassTypes.FindAsync([request.ClassTypeId], ct)
            ?? throw new KeyNotFoundException($"Class type with ID {request.ClassTypeId} not found.");

        var instructor = await db.Instructors.FindAsync([request.InstructorId], ct)
            ?? throw new KeyNotFoundException($"Instructor with ID {request.InstructorId} not found.");

        if (request.EndTime <= request.StartTime)
            throw new ArgumentException("End time must be after start time.");

        // Check instructor conflicts (exclude self)
        var hasConflict = await db.ClassSchedules.AnyAsync(cs =>
            cs.Id != id &&
            cs.InstructorId == request.InstructorId &&
            cs.Status != ClassScheduleStatus.Cancelled &&
            cs.StartTime < request.EndTime &&
            cs.EndTime > request.StartTime, ct);

        if (hasConflict)
            throw new InvalidOperationException("Instructor has a schedule conflict during this time.");

        schedule.ClassTypeId = request.ClassTypeId;
        schedule.InstructorId = request.InstructorId;
        schedule.StartTime = request.StartTime;
        schedule.EndTime = request.EndTime;
        schedule.Capacity = request.Capacity;
        schedule.Room = request.Room;

        await db.SaveChangesAsync(ct);

        // Reload nav properties if changed
        await db.Entry(schedule).Reference(s => s.ClassType).LoadAsync(ct);
        await db.Entry(schedule).Reference(s => s.Instructor).LoadAsync(ct);

        return MapToResponse(schedule);
    }

    public async Task<ClassScheduleResponse> CancelAsync(int id, CancelClassRequest request, CancellationToken ct)
    {
        var schedule = await db.ClassSchedules
            .Include(cs => cs.ClassType)
            .Include(cs => cs.Instructor)
            .Include(cs => cs.Bookings)
            .FirstOrDefaultAsync(cs => cs.Id == id, ct)
            ?? throw new KeyNotFoundException($"Class schedule with ID {id} not found.");

        if (schedule.Status == ClassScheduleStatus.Cancelled)
            throw new InvalidOperationException("Class is already cancelled.");

        if (schedule.Status == ClassScheduleStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed class.");

        schedule.Status = ClassScheduleStatus.Cancelled;
        schedule.CancellationReason = request.Reason ?? "Class cancelled by studio";

        // Cascade cancel all bookings
        foreach (var booking in schedule.Bookings.Where(b => b.Status is BookingStatus.Confirmed or BookingStatus.Waitlisted))
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationDate = DateTimeOffset.UtcNow;
            booking.CancellationReason = "Class cancelled by studio";
        }

        schedule.CurrentEnrollment = 0;
        schedule.WaitlistCount = 0;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Cancelled class {Id} with {Count} bookings", id, schedule.Bookings.Count);
        return MapToResponse(schedule);
    }

    public async Task<IReadOnlyList<RosterEntryResponse>> GetRosterAsync(int classId, CancellationToken ct)
    {
        var exists = await db.ClassSchedules.AnyAsync(cs => cs.Id == classId, ct);
        if (!exists) throw new KeyNotFoundException($"Class schedule with ID {classId} not found.");

        var bookings = await db.Bookings
            .Include(b => b.Member)
            .Where(b => b.ClassScheduleId == classId &&
                        (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Attended))
            .OrderBy(b => b.BookingDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return bookings.Select(b => new RosterEntryResponse(
            b.Id, b.MemberId,
            $"{b.Member.FirstName} {b.Member.LastName}",
            b.Status.ToString(), b.BookingDate, b.CheckInTime)).ToList();
    }

    public async Task<IReadOnlyList<WaitlistEntryResponse>> GetWaitlistAsync(int classId, CancellationToken ct)
    {
        var exists = await db.ClassSchedules.AnyAsync(cs => cs.Id == classId, ct);
        if (!exists) throw new KeyNotFoundException($"Class schedule with ID {classId} not found.");

        var bookings = await db.Bookings
            .Include(b => b.Member)
            .Where(b => b.ClassScheduleId == classId && b.Status == BookingStatus.Waitlisted)
            .OrderBy(b => b.WaitlistPosition)
            .AsNoTracking()
            .ToListAsync(ct);

        return bookings.Select(b => new WaitlistEntryResponse(
            b.Id, b.MemberId,
            $"{b.Member.FirstName} {b.Member.LastName}",
            b.WaitlistPosition, b.BookingDate)).ToList();
    }

    public async Task<IReadOnlyList<ClassScheduleResponse>> GetAvailableAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var sevenDaysLater = now.AddDays(7);

        var classes = await db.ClassSchedules
            .Include(cs => cs.ClassType)
            .Include(cs => cs.Instructor)
            .Where(cs => cs.Status == ClassScheduleStatus.Scheduled &&
                         cs.StartTime > now &&
                         cs.StartTime <= sevenDaysLater &&
                         cs.CurrentEnrollment < cs.Capacity)
            .OrderBy(cs => cs.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

        return classes.Select(MapToResponse).ToList();
    }

    private static ClassScheduleResponse MapToResponse(ClassSchedule cs) => new(
        cs.Id, cs.ClassTypeId, cs.ClassType.Name,
        cs.InstructorId, $"{cs.Instructor.FirstName} {cs.Instructor.LastName}",
        cs.StartTime, cs.EndTime, cs.Capacity,
        cs.CurrentEnrollment, cs.WaitlistCount,
        Math.Max(0, cs.Capacity - cs.CurrentEnrollment),
        cs.Room, cs.Status, cs.CancellationReason,
        cs.ClassType.IsPremium, cs.CreatedAt, cs.UpdatedAt);
}
