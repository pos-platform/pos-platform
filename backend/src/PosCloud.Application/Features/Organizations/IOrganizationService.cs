using PosCloud.Application.Common.Models;
using PosCloud.Application.Features.Organizations.Dtos;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Organizations;

/// <summary>Tashkilotlar va ularning obunalarini boshqarish — faqat SuperAdmin uchun.</summary>
public interface IOrganizationService
{
    Task<PagedResult<OrganizationDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<OrganizationDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default);
    Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationRequest request, CancellationToken ct = default);
    Task<OrganizationDto> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);

    Task<OrgUserDto> CreateUserAsync(Guid orgId, CreateOrgUserRequest request, CancellationToken ct = default);

    /// <summary>Mavjud desktop foydalanuvchini (kassir/menejer) tahrirlaydi.</summary>
    Task<OrgUserDto> UpdateUserAsync(Guid userId, UpdateOrgUserRequest request, CancellationToken ct = default);
    Task<OrgUserDto> ExtendLicenseAsync(Guid userId, int months, CancellationToken ct = default);
    Task<OrgUserDto> RevokeLicenseAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Foydalanuvchini doimiy (bir martalik to'liq sotib olingan) litsenziyaga o'tkazadi yoki qaytaradi.</summary>
    Task<OrgUserDto> SetLifetimeAsync(Guid userId, bool isLifetime, CancellationToken ct = default);
    /// <summary>
    /// Loginning kompyuterga bog'lanishini bekor qiladi — keyingi kirishda yangi kompyuter
    /// biriktiriladi (kassa kompyuteri almashganda/qayta o'rnatilganda ishlatiladi).
    /// </summary>
    Task<OrgUserDto> ResetDeviceAsync(Guid userId, CancellationToken ct = default);

    Task<OrgUserDto> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default);
    Task<IReadOnlyList<OrgRoleDto>> GetRolesAsync(Guid orgId, CancellationToken ct = default);
    Task<OrgUserDto> AssignAppRoleAsync(Guid userId, Guid? roleId, CancellationToken ct = default);
    Task<OrgAlertsDto> GetAlertsAsync(CancellationToken ct = default);

    /// <summary>Tashkilot qisqacha ma'lumoti (statistika bilan) — org admin sozlamalar uchun.</summary>
    Task<OrganizationDto> GetSummaryAsync(Guid id, CancellationToken ct = default);

    /// <summary>Tashkilot faoliyat turini (Do'kon/Restoran) o'zgartiradi.</summary>
    Task<OrganizationDto> SetBusinessTypeAsync(Guid id, BusinessType type, CancellationToken ct = default);
}
