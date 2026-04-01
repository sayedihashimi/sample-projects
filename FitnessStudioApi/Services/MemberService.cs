using FitnessStudioApi.Data;
using FitnessStudioApi.DTOs;
using FitnessStudioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessStudioApi.Services;

public sealed class MemberService(FitnessDbContext db, ILogger<MemberService> logger) : IMemberService
{
    public async Task<PaginatedList<MemberListResponse>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Members.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(m => m.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(m =>
                m.FirstName.ToLower().Contains(term) ||
                m.LastName.ToLower().Contains(term) ||
                m.Email.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var responses = items.Select(m => new MemberListResponse(
            m.Id, m.FirstName, m.LastName, m.Email, m.Phone, m.IsActive, m.JoinDate)).ToList();

        return new PaginatedList<MemberListResponse>(responses, totalCount, page, pageSize);
    }

    public async Task<MemberResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var member = await db.Members
            .Include(m => m.Memberships).ThenInclude(ms => ms.MembershipPlan)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (member is null) return null;

        var activeMembership = member.Memberships
            .FirstOrDefault(ms => ms.Status == MembershipStatus.Active || ms.Status == MembershipStatus.Frozen);

        MembershipSummary? summary = null;
        if (activeMembership is not null)
        {
            summary = new MembershipSummary(
                activeMembership.Id,
                activeMembership.MembershipPlan.Name,
                activeMembership.StartDate,
                activeMembership.EndDate,
                activeMembership.Status.ToString());
        }

        return new MemberResponse(
            member.Id, member.FirstName, member.LastName, member.Email, member.Phone,
            member.DateOfBirth, member.EmergencyContactName, member.EmergencyContactPhone,
            member.JoinDate, member.IsActive, summary, member.CreatedAt, member.UpdatedAt);
    }

    public async Task<MemberResponse> CreateAsync(CreateMemberRequest request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - request.DateOfBirth.Year;
        if (request.DateOfBirth > today.AddYears(-age)) age--;
        if (age < 16)
            throw new ArgumentException("Member must be at least 16 years old.");

        var emailExists = await db.Members.AnyAsync(m => m.Email == request.Email, ct);
        if (emailExists)
            throw new InvalidOperationException($"A member with email '{request.Email}' already exists.");

        var member = new Member
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            JoinDate = today
        };

        db.Members.Add(member);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Registered member {Name} (ID: {Id})", $"{member.FirstName} {member.LastName}", member.Id);

        return new MemberResponse(
            member.Id, member.FirstName, member.LastName, member.Email, member.Phone,
            member.DateOfBirth, member.EmergencyContactName, member.EmergencyContactPhone,
            member.JoinDate, member.IsActive, null, member.CreatedAt, member.UpdatedAt);
    }

    public async Task<MemberResponse> UpdateAsync(int id, UpdateMemberRequest request, CancellationToken ct)
    {
        var member = await db.Members
            .Include(m => m.Memberships).ThenInclude(ms => ms.MembershipPlan)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new KeyNotFoundException($"Member with ID {id} not found.");

        var emailExists = await db.Members.AnyAsync(m => m.Email == request.Email && m.Id != id, ct);
        if (emailExists)
            throw new InvalidOperationException($"A member with email '{request.Email}' already exists.");

        member.FirstName = request.FirstName;
        member.LastName = request.LastName;
        member.Email = request.Email;
        member.Phone = request.Phone;
        member.DateOfBirth = request.DateOfBirth;
        member.EmergencyContactName = request.EmergencyContactName;
        member.EmergencyContactPhone = request.EmergencyContactPhone;

        await db.SaveChangesAsync(ct);

        var activeMembership = member.Memberships
            .FirstOrDefault(ms => ms.Status == MembershipStatus.Active || ms.Status == MembershipStatus.Frozen);

        MembershipSummary? summary = null;
        if (activeMembership is not null)
        {
            summary = new MembershipSummary(
                activeMembership.Id,
                activeMembership.MembershipPlan.Name,
                activeMembership.StartDate,
                activeMembership.EndDate,
                activeMembership.Status.ToString());
        }

        return new MemberResponse(
            member.Id, member.FirstName, member.LastName, member.Email, member.Phone,
            member.DateOfBirth, member.EmergencyContactName, member.EmergencyContactPhone,
            member.JoinDate, member.IsActive, summary, member.CreatedAt, member.UpdatedAt);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        var member = await db.Members.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Member with ID {id} not found.");

        var now = DateTimeOffset.UtcNow;
        var hasFutureBookings = await db.Bookings.AnyAsync(
            b => b.MemberId == id &&
                 b.Status == BookingStatus.Confirmed &&
                 b.ClassSchedule.StartTime > now, ct);

        if (hasFutureBookings)
            throw new InvalidOperationException("Cannot deactivate member with future confirmed bookings. Cancel bookings first.");

        member.IsActive = false;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deactivated member {Id}", id);
    }

    public async Task<PaginatedList<BookingResponse>> GetBookingsAsync(int memberId, string? status, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken ct)
    {
        var exists = await db.Members.AnyAsync(m => m.Id == memberId, ct);
        if (!exists) throw new KeyNotFoundException($"Member with ID {memberId} not found.");

        var query = db.Bookings
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.ClassType)
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.Instructor)
            .Include(b => b.Member)
            .Where(b => b.MemberId == memberId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
            query = query.Where(b => b.Status == bookingStatus);

        if (fromDate.HasValue)
            query = query.Where(b => b.ClassSchedule.StartTime >= new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (toDate.HasValue)
            query = query.Where(b => b.ClassSchedule.StartTime <= new DateTimeOffset(toDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(b => b.BookingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var responses = items.Select(MapBookingToResponse).ToList();
        return new PaginatedList<BookingResponse>(responses, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<BookingResponse>> GetUpcomingBookingsAsync(int memberId, CancellationToken ct)
    {
        var exists = await db.Members.AnyAsync(m => m.Id == memberId, ct);
        if (!exists) throw new KeyNotFoundException($"Member with ID {memberId} not found.");

        var now = DateTimeOffset.UtcNow;
        var bookings = await db.Bookings
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.ClassType)
            .Include(b => b.ClassSchedule).ThenInclude(cs => cs.Instructor)
            .Include(b => b.Member)
            .Where(b => b.MemberId == memberId &&
                        b.Status == BookingStatus.Confirmed &&
                        b.ClassSchedule.StartTime > now)
            .OrderBy(b => b.ClassSchedule.StartTime)
            .AsNoTracking()
            .ToListAsync(ct);

        return bookings.Select(MapBookingToResponse).ToList();
    }

    public async Task<IReadOnlyList<MembershipResponse>> GetMembershipsAsync(int memberId, CancellationToken ct)
    {
        var exists = await db.Members.AnyAsync(m => m.Id == memberId, ct);
        if (!exists) throw new KeyNotFoundException($"Member with ID {memberId} not found.");

        var memberships = await db.Memberships
            .Include(ms => ms.Member)
            .Include(ms => ms.MembershipPlan)
            .Where(ms => ms.MemberId == memberId)
            .OrderByDescending(ms => ms.StartDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return memberships.Select(ms => new MembershipResponse(
            ms.Id, ms.MemberId,
            $"{ms.Member.FirstName} {ms.Member.LastName}",
            ms.MembershipPlanId, ms.MembershipPlan.Name,
            ms.StartDate, ms.EndDate,
            ms.Status.ToString(), ms.PaymentStatus.ToString(),
            ms.FreezeStartDate, ms.FreezeEndDate,
            ms.CreatedAt, ms.UpdatedAt)).ToList();
    }

    private static BookingResponse MapBookingToResponse(Booking b) => new(
        b.Id, b.ClassScheduleId, b.ClassSchedule.ClassType.Name,
        $"{b.ClassSchedule.Instructor.FirstName} {b.ClassSchedule.Instructor.LastName}",
        b.ClassSchedule.StartTime, b.ClassSchedule.EndTime, b.ClassSchedule.Room,
        b.MemberId, $"{b.Member.FirstName} {b.Member.LastName}",
        b.BookingDate, b.Status, b.WaitlistPosition,
        b.CheckInTime, b.CancellationDate, b.CancellationReason,
        b.CreatedAt, b.UpdatedAt);
}
