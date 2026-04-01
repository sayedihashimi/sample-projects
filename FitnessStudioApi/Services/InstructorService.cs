using FitnessStudioApi.Data;
using FitnessStudioApi.DTOs;
using FitnessStudioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessStudioApi.Services;

public sealed class InstructorService(FitnessDbContext db, ILogger<InstructorService> logger) : IInstructorService
{
    public async Task<IReadOnlyList<InstructorResponse>> GetAllAsync(string? specialization, bool? isActive, CancellationToken ct)
    {
        var query = db.Instructors.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(i => i.IsActive == isActive.Value);
        else
            query = query.Where(i => i.IsActive);

        if (!string.IsNullOrWhiteSpace(specialization))
            query = query.Where(i => i.Specializations != null && i.Specializations.Contains(specialization));

        var instructors = await query.OrderBy(i => i.LastName).ThenBy(i => i.FirstName).ToListAsync(ct);
        return instructors.Select(MapToResponse).ToList();
    }

    public async Task<InstructorResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var instructor = await db.Instructors.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        return instructor is null ? null : MapToResponse(instructor);
    }

    public async Task<InstructorResponse> CreateAsync(CreateInstructorRequest request, CancellationToken ct)
    {
        var emailExists = await db.Instructors.AnyAsync(i => i.Email == request.Email, ct);
        if (emailExists)
            throw new InvalidOperationException($"An instructor with email '{request.Email}' already exists.");

        var instructor = new Instructor
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Bio = request.Bio,
            Specializations = request.Specializations,
            HireDate = request.HireDate
        };

        db.Instructors.Add(instructor);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created instructor {Name} (ID: {Id})", $"{instructor.FirstName} {instructor.LastName}", instructor.Id);
        return MapToResponse(instructor);
    }

    public async Task<InstructorResponse> UpdateAsync(int id, UpdateInstructorRequest request, CancellationToken ct)
    {
        var instructor = await db.Instructors.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Instructor with ID {id} not found.");

        var emailExists = await db.Instructors.AnyAsync(i => i.Email == request.Email && i.Id != id, ct);
        if (emailExists)
            throw new InvalidOperationException($"An instructor with email '{request.Email}' already exists.");

        instructor.FirstName = request.FirstName;
        instructor.LastName = request.LastName;
        instructor.Email = request.Email;
        instructor.Phone = request.Phone;
        instructor.Bio = request.Bio;
        instructor.Specializations = request.Specializations;

        await db.SaveChangesAsync(ct);
        return MapToResponse(instructor);
    }

    public async Task<IReadOnlyList<ClassScheduleResponse>> GetScheduleAsync(int instructorId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct)
    {
        var exists = await db.Instructors.AnyAsync(i => i.Id == instructorId, ct);
        if (!exists)
            throw new KeyNotFoundException($"Instructor with ID {instructorId} not found.");

        var query = db.ClassSchedules
            .Include(cs => cs.ClassType)
            .Include(cs => cs.Instructor)
            .Where(cs => cs.InstructorId == instructorId)
            .AsNoTracking();

        if (fromDate.HasValue)
            query = query.Where(cs => cs.StartTime >= new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (toDate.HasValue)
            query = query.Where(cs => cs.StartTime <= new DateTimeOffset(toDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero));

        var schedules = await query.OrderBy(cs => cs.StartTime).ToListAsync(ct);
        return schedules.Select(MapScheduleToResponse).ToList();
    }

    private static InstructorResponse MapToResponse(Instructor i) => new(
        i.Id, i.FirstName, i.LastName, i.Email, i.Phone,
        i.Bio, i.Specializations, i.HireDate, i.IsActive,
        i.CreatedAt, i.UpdatedAt);

    private static ClassScheduleResponse MapScheduleToResponse(ClassSchedule cs) => new(
        cs.Id, cs.ClassTypeId, cs.ClassType.Name,
        cs.InstructorId, $"{cs.Instructor.FirstName} {cs.Instructor.LastName}",
        cs.StartTime, cs.EndTime, cs.Capacity,
        cs.CurrentEnrollment, cs.WaitlistCount,
        Math.Max(0, cs.Capacity - cs.CurrentEnrollment),
        cs.Room, cs.Status, cs.CancellationReason,
        cs.ClassType.IsPremium, cs.CreatedAt, cs.UpdatedAt);
}
