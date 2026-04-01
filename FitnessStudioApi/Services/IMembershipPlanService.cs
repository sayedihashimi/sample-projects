using FitnessStudioApi.DTOs;

namespace FitnessStudioApi.Services;

public interface IMembershipPlanService
{
    Task<IReadOnlyList<MembershipPlanResponse>> GetAllActiveAsync(CancellationToken ct);
    Task<MembershipPlanResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<MembershipPlanResponse> CreateAsync(CreateMembershipPlanRequest request, CancellationToken ct);
    Task<MembershipPlanResponse> UpdateAsync(int id, UpdateMembershipPlanRequest request, CancellationToken ct);
    Task DeactivateAsync(int id, CancellationToken ct);
}
