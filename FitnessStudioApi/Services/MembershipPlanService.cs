using FitnessStudioApi.Data;
using FitnessStudioApi.DTOs;
using FitnessStudioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessStudioApi.Services;

public sealed class MembershipPlanService(FitnessDbContext db, ILogger<MembershipPlanService> logger) : IMembershipPlanService
{
    public async Task<IReadOnlyList<MembershipPlanResponse>> GetAllActiveAsync(CancellationToken ct)
    {
        var plans = await db.MembershipPlans
            .Where(p => p.IsActive)
            .AsNoTracking()
            .OrderBy(p => p.Price)
            .ToListAsync(ct);

        return plans.Select(MapToResponse).ToList();
    }

    public async Task<MembershipPlanResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var plan = await db.MembershipPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return plan is null ? null : MapToResponse(plan);
    }

    public async Task<MembershipPlanResponse> CreateAsync(CreateMembershipPlanRequest request, CancellationToken ct)
    {
        var exists = await db.MembershipPlans.AnyAsync(p => p.Name == request.Name, ct);
        if (exists)
            throw new InvalidOperationException($"A membership plan with name '{request.Name}' already exists.");

        var plan = new MembershipPlan
        {
            Name = request.Name,
            Description = request.Description,
            DurationMonths = request.DurationMonths,
            Price = request.Price,
            MaxClassBookingsPerWeek = request.MaxClassBookingsPerWeek,
            AllowsPremiumClasses = request.AllowsPremiumClasses
        };

        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created membership plan {PlanName} (ID: {PlanId})", plan.Name, plan.Id);
        return MapToResponse(plan);
    }

    public async Task<MembershipPlanResponse> UpdateAsync(int id, UpdateMembershipPlanRequest request, CancellationToken ct)
    {
        var plan = await db.MembershipPlans.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Membership plan with ID {id} not found.");

        var nameExists = await db.MembershipPlans.AnyAsync(p => p.Name == request.Name && p.Id != id, ct);
        if (nameExists)
            throw new InvalidOperationException($"A membership plan with name '{request.Name}' already exists.");

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.DurationMonths = request.DurationMonths;
        plan.Price = request.Price;
        plan.MaxClassBookingsPerWeek = request.MaxClassBookingsPerWeek;
        plan.AllowsPremiumClasses = request.AllowsPremiumClasses;

        await db.SaveChangesAsync(ct);
        return MapToResponse(plan);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        var plan = await db.MembershipPlans.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Membership plan with ID {id} not found.");

        plan.IsActive = false;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deactivated membership plan {PlanName} (ID: {PlanId})", plan.Name, plan.Id);
    }

    private static MembershipPlanResponse MapToResponse(MembershipPlan plan) => new(
        plan.Id,
        plan.Name,
        plan.Description,
        plan.DurationMonths,
        plan.Price,
        plan.MaxClassBookingsPerWeek,
        plan.AllowsPremiumClasses,
        plan.IsActive,
        plan.CreatedAt,
        plan.UpdatedAt);
}
