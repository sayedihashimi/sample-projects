using FitnessStudioApi.Data;
using FitnessStudioApi.DTOs;
using FitnessStudioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessStudioApi.Services;

public sealed class MembershipService(FitnessDbContext db, ILogger<MembershipService> logger) : IMembershipService
{
    public async Task<MembershipResponse> CreateAsync(CreateMembershipRequest request, CancellationToken ct)
    {
        var member = await db.Members.FindAsync([request.MemberId], ct)
            ?? throw new KeyNotFoundException($"Member with ID {request.MemberId} not found.");

        var plan = await db.MembershipPlans.FindAsync([request.MembershipPlanId], ct)
            ?? throw new KeyNotFoundException($"Membership plan with ID {request.MembershipPlanId} not found.");

        if (!plan.IsActive)
            throw new ArgumentException("Cannot create a membership with an inactive plan.");

        var hasActive = await db.Memberships.AnyAsync(
            ms => ms.MemberId == request.MemberId &&
                  (ms.Status == MembershipStatus.Active || ms.Status == MembershipStatus.Frozen), ct);

        if (hasActive)
            throw new InvalidOperationException("Member already has an active or frozen membership.");

        var membership = new Membership
        {
            MemberId = request.MemberId,
            MembershipPlanId = request.MembershipPlanId,
            StartDate = request.StartDate,
            EndDate = request.StartDate.AddMonths(plan.DurationMonths),
            Status = MembershipStatus.Active,
            PaymentStatus = PaymentStatus.Paid
        };

        db.Memberships.Add(membership);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created membership for member {MemberId} with plan {PlanName}", request.MemberId, plan.Name);

        return MapToResponse(membership, member, plan);
    }

    public async Task<MembershipResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var membership = await db.Memberships
            .Include(ms => ms.Member)
            .Include(ms => ms.MembershipPlan)
            .AsNoTracking()
            .FirstOrDefaultAsync(ms => ms.Id == id, ct);

        return membership is null ? null : MapToResponse(membership, membership.Member, membership.MembershipPlan);
    }

    public async Task<MembershipResponse> CancelAsync(int id, CancellationToken ct)
    {
        var membership = await db.Memberships
            .Include(ms => ms.Member)
            .Include(ms => ms.MembershipPlan)
            .FirstOrDefaultAsync(ms => ms.Id == id, ct)
            ?? throw new KeyNotFoundException($"Membership with ID {id} not found.");

        if (membership.Status == MembershipStatus.Cancelled)
            throw new InvalidOperationException("Membership is already cancelled.");

        if (membership.Status == MembershipStatus.Expired)
            throw new InvalidOperationException("Cannot cancel an expired membership.");

        membership.Status = MembershipStatus.Cancelled;
        membership.PaymentStatus = PaymentStatus.Refunded;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Cancelled membership {Id} for member {MemberId}", id, membership.MemberId);

        return MapToResponse(membership, membership.Member, membership.MembershipPlan);
    }

    public async Task<MembershipResponse> FreezeAsync(int id, FreezeMembershipRequest request, CancellationToken ct)
    {
        var membership = await db.Memberships
            .Include(ms => ms.Member)
            .Include(ms => ms.MembershipPlan)
            .FirstOrDefaultAsync(ms => ms.Id == id, ct)
            ?? throw new KeyNotFoundException($"Membership with ID {id} not found.");

        if (membership.Status != MembershipStatus.Active)
            throw new InvalidOperationException("Only active memberships can be frozen.");

        if (membership.FreezeStartDate.HasValue)
            throw new InvalidOperationException("This membership has already been frozen once. Only one freeze per term is allowed.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        membership.Status = MembershipStatus.Frozen;
        membership.FreezeStartDate = today;
        membership.FreezeEndDate = today.AddDays(request.FreezeDurationDays);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Froze membership {Id} for {Days} days", id, request.FreezeDurationDays);

        return MapToResponse(membership, membership.Member, membership.MembershipPlan);
    }

    public async Task<MembershipResponse> UnfreezeAsync(int id, CancellationToken ct)
    {
        var membership = await db.Memberships
            .Include(ms => ms.Member)
            .Include(ms => ms.MembershipPlan)
            .FirstOrDefaultAsync(ms => ms.Id == id, ct)
            ?? throw new KeyNotFoundException($"Membership with ID {id} not found.");

        if (membership.Status != MembershipStatus.Frozen)
            throw new InvalidOperationException("Only frozen memberships can be unfrozen.");

        if (!membership.FreezeStartDate.HasValue || !membership.FreezeEndDate.HasValue)
            throw new InvalidOperationException("Membership freeze dates are not set.");

        var freezeDuration = membership.FreezeEndDate.Value.DayNumber - membership.FreezeStartDate.Value.DayNumber;
        membership.EndDate = membership.EndDate.AddDays(freezeDuration);
        membership.Status = MembershipStatus.Active;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Unfroze membership {Id}, end date extended by {Days} days", id, freezeDuration);

        return MapToResponse(membership, membership.Member, membership.MembershipPlan);
    }

    public async Task<MembershipResponse> RenewAsync(int id, CancellationToken ct)
    {
        var membership = await db.Memberships
            .Include(ms => ms.Member)
            .Include(ms => ms.MembershipPlan)
            .FirstOrDefaultAsync(ms => ms.Id == id, ct)
            ?? throw new KeyNotFoundException($"Membership with ID {id} not found.");

        if (membership.Status != MembershipStatus.Expired)
            throw new InvalidOperationException("Only expired memberships can be renewed.");

        var hasActive = await db.Memberships.AnyAsync(
            ms => ms.MemberId == membership.MemberId &&
                  ms.Id != id &&
                  (ms.Status == MembershipStatus.Active || ms.Status == MembershipStatus.Frozen), ct);

        if (hasActive)
            throw new InvalidOperationException("Member already has an active or frozen membership.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var newMembership = new Membership
        {
            MemberId = membership.MemberId,
            MembershipPlanId = membership.MembershipPlanId,
            StartDate = today,
            EndDate = today.AddMonths(membership.MembershipPlan.DurationMonths),
            Status = MembershipStatus.Active,
            PaymentStatus = PaymentStatus.Paid
        };

        db.Memberships.Add(newMembership);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Renewed membership for member {MemberId}", membership.MemberId);

        return MapToResponse(newMembership, membership.Member, membership.MembershipPlan);
    }

    private static MembershipResponse MapToResponse(Membership ms, Member m, MembershipPlan p) => new(
        ms.Id, ms.MemberId,
        $"{m.FirstName} {m.LastName}",
        ms.MembershipPlanId, p.Name,
        ms.StartDate, ms.EndDate,
        ms.Status.ToString(), ms.PaymentStatus.ToString(),
        ms.FreezeStartDate, ms.FreezeEndDate,
        ms.CreatedAt, ms.UpdatedAt);
}
