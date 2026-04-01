using FitnessStudioApi.DTOs;

namespace FitnessStudioApi.Services;

public interface IInstructorService
{
    Task<IReadOnlyList<InstructorResponse>> GetAllAsync(string? specialization, bool? isActive, CancellationToken ct);
    Task<InstructorResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<InstructorResponse> CreateAsync(CreateInstructorRequest request, CancellationToken ct);
    Task<InstructorResponse> UpdateAsync(int id, UpdateInstructorRequest request, CancellationToken ct);
    Task<IReadOnlyList<ClassScheduleResponse>> GetScheduleAsync(int instructorId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct);
}
