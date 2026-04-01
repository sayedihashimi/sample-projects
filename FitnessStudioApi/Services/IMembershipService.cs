using FitnessStudioApi.DTOs;

namespace FitnessStudioApi.Services;

public interface IMembershipService
{
    Task<MembershipResponse> CreateAsync(CreateMembershipRequest request, CancellationToken ct);
    Task<MembershipResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<MembershipResponse> CancelAsync(int id, CancellationToken ct);
    Task<MembershipResponse> FreezeAsync(int id, FreezeMembershipRequest request, CancellationToken ct);
    Task<MembershipResponse> UnfreezeAsync(int id, CancellationToken ct);
    Task<MembershipResponse> RenewAsync(int id, CancellationToken ct);
}
