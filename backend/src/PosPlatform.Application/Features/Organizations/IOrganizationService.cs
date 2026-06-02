using PosPlatform.Application.Features.Organizations.Dtos;

namespace PosPlatform.Application.Features.Organizations;

/// <summary>Tashkilotlar va ularning obunalarini boshqarish — faqat SuperAdmin uchun.</summary>
public interface IOrganizationService
{
    Task<IReadOnlyList<OrganizationDto>> GetAllAsync(CancellationToken ct = default);
    Task<OrganizationDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default);
    Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default);
    Task<OrganizationDto> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);

    Task<OrgUserDto> CreateUserAsync(Guid orgId, CreateOrgUserRequest request, CancellationToken ct = default);
    Task<OrgUserDto> ExtendLicenseAsync(Guid userId, int months, CancellationToken ct = default);
    Task<OrgUserDto> RevokeLicenseAsync(Guid userId, CancellationToken ct = default);
    Task<OrgUserDto> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
}
