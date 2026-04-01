using FitnessStudioApi.Data;
using FitnessStudioApi.DTOs;
using FitnessStudioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessStudioApi.Services;

public sealed class ClassTypeService(FitnessDbContext db, ILogger<ClassTypeService> logger) : IClassTypeService
{
    public async Task<IReadOnlyList<ClassTypeResponse>> GetAllAsync(DifficultyLevel? difficulty, bool? isPremium, CancellationToken ct)
    {
        var query = db.ClassTypes.Where(c => c.IsActive).AsNoTracking();

        if (difficulty.HasValue)
            query = query.Where(c => c.DifficultyLevel == difficulty.Value);
        if (isPremium.HasValue)
            query = query.Where(c => c.IsPremium == isPremium.Value);

        var classTypes = await query.OrderBy(c => c.Name).ToListAsync(ct);
        return classTypes.Select(MapToResponse).ToList();
    }

    public async Task<ClassTypeResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var classType = await db.ClassTypes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return classType is null ? null : MapToResponse(classType);
    }

    public async Task<ClassTypeResponse> CreateAsync(CreateClassTypeRequest request, CancellationToken ct)
    {
        var exists = await db.ClassTypes.AnyAsync(c => c.Name == request.Name, ct);
        if (exists)
            throw new InvalidOperationException($"A class type with name '{request.Name}' already exists.");

        var classType = new ClassType
        {
            Name = request.Name,
            Description = request.Description,
            DefaultDurationMinutes = request.DefaultDurationMinutes,
            DefaultCapacity = request.DefaultCapacity,
            IsPremium = request.IsPremium,
            CaloriesPerSession = request.CaloriesPerSession,
            DifficultyLevel = request.DifficultyLevel
        };

        db.ClassTypes.Add(classType);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created class type {ClassName} (ID: {ClassId})", classType.Name, classType.Id);
        return MapToResponse(classType);
    }

    public async Task<ClassTypeResponse> UpdateAsync(int id, UpdateClassTypeRequest request, CancellationToken ct)
    {
        var classType = await db.ClassTypes.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Class type with ID {id} not found.");

        var nameExists = await db.ClassTypes.AnyAsync(c => c.Name == request.Name && c.Id != id, ct);
        if (nameExists)
            throw new InvalidOperationException($"A class type with name '{request.Name}' already exists.");

        classType.Name = request.Name;
        classType.Description = request.Description;
        classType.DefaultDurationMinutes = request.DefaultDurationMinutes;
        classType.DefaultCapacity = request.DefaultCapacity;
        classType.IsPremium = request.IsPremium;
        classType.CaloriesPerSession = request.CaloriesPerSession;
        classType.DifficultyLevel = request.DifficultyLevel;

        await db.SaveChangesAsync(ct);
        return MapToResponse(classType);
    }

    private static ClassTypeResponse MapToResponse(ClassType ct) => new(
        ct.Id,
        ct.Name,
        ct.Description,
        ct.DefaultDurationMinutes,
        ct.DefaultCapacity,
        ct.IsPremium,
        ct.CaloriesPerSession,
        ct.DifficultyLevel,
        ct.IsActive,
        ct.CreatedAt,
        ct.UpdatedAt);
}
