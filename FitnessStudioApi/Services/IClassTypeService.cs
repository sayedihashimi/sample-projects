using FitnessStudioApi.DTOs;
using FitnessStudioApi.Models;

namespace FitnessStudioApi.Services;

public interface IClassTypeService
{
    Task<IReadOnlyList<ClassTypeResponse>> GetAllAsync(DifficultyLevel? difficulty, bool? isPremium, CancellationToken ct);
    Task<ClassTypeResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ClassTypeResponse> CreateAsync(CreateClassTypeRequest request, CancellationToken ct);
    Task<ClassTypeResponse> UpdateAsync(int id, UpdateClassTypeRequest request, CancellationToken ct);
}
