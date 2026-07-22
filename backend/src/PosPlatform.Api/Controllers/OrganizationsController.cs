using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Common.Models;
using PosPlatform.Application.Features.Organizations;
using PosPlatform.Application.Features.Organizations.Dtos;
using PosPlatform.Application.Features.Stores;
using PosPlatform.Application.Features.Stores.Dtos;

namespace PosPlatform.Api.Controllers;

/// <summary>Tashkilotlar va obunalarni boshqarish — faqat platforma egasi (SuperAdmin) uchun.</summary>
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/organizations")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _service;
    private readonly IStoreService _stores;

    public OrganizationsController(IOrganizationService service, IStoreService stores)
    {
        _service = service;
        _stores = stores;
    }

    /// <summary>To'lov qilinmagan va muddati yaqin tugayotgan kassirlar bo'lgan tashkilotlar.</summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<OrgAlertsDto>> GetAlerts(CancellationToken ct)
        => Ok(await _service.GetAlertsAsync(ct));

    /// <summary>Barcha tashkilotlar (sahifalash bilan).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrganizationDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(page, pageSize, ct));

    /// <summary>Bitta tashkilot tafsiloti — foydalanuvchilar va obuna holatlari bilan.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationDetailDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _service.GetByIdAsync(id, ct));

    /// <summary>Yangi tashkilot + uning bosh admini.</summary>
    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> Create(CreateOrganizationRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request, ct));

    /// <summary>Tashkilot ma'lumotlarini tahrirlash.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrganizationDto>> Update(Guid id, UpdateOrganizationRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<OrganizationDto>> Activate(Guid id, CancellationToken ct)
        => Ok(await _service.SetActiveAsync(id, true, ct));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<OrganizationDto>> Deactivate(Guid id, CancellationToken ct)
        => Ok(await _service.SetActiveAsync(id, false, ct));

    /// <summary>Tashkilot ichida yangi foydalanuvchi (kassir/menejer/admin) yaratish.</summary>
    [HttpPost("{id:guid}/users")]
    public async Task<ActionResult<OrgUserDto>> CreateUser(Guid id, CreateOrgUserRequest request, CancellationToken ct)
        => Ok(await _service.CreateUserAsync(id, request, ct));

    /// <summary>Mavjud desktop foydalanuvchini (kassir/menejer) tahrirlash.</summary>
    [HttpPut("users/{userId:guid}")]
    public async Task<ActionResult<OrgUserDto>> UpdateUser(Guid userId, UpdateOrgUserRequest request, CancellationToken ct)
        => Ok(await _service.UpdateUserAsync(userId, request, ct));

    /// <summary>Foydalanuvchi obunasini uzaytirish (oylik to'lov qabul qilingach faollashtirish).</summary>
    [HttpPost("users/{userId:guid}/extend-license")]
    public async Task<ActionResult<OrgUserDto>> ExtendLicense(Guid userId, ExtendLicenseRequest request, CancellationToken ct)
        => Ok(await _service.ExtendLicenseAsync(userId, request.Months, ct));

    /// <summary>Obunani bekor qilish (muddatni olib tashlash — foydalanuvchi kira olmaydi).</summary>
    [HttpPost("users/{userId:guid}/revoke-license")]
    public async Task<ActionResult<OrgUserDto>> RevokeLicense(Guid userId, CancellationToken ct)
        => Ok(await _service.RevokeLicenseAsync(userId, ct));

    /// <summary>Foydalanuvchini doimiy (bir martalik to'liq sotib olingan) litsenziyaga o'tkazish.</summary>
    [HttpPost("users/{userId:guid}/set-lifetime")]
    public async Task<ActionResult<OrgUserDto>> SetLifetime(Guid userId, CancellationToken ct)
        => Ok(await _service.SetLifetimeAsync(userId, true, ct));

    /// <summary>Doimiy litsenziyani bekor qilish (oylik obunaga qaytarish).</summary>
    [HttpPost("users/{userId:guid}/unset-lifetime")]
    public async Task<ActionResult<OrgUserDto>> UnsetLifetime(Guid userId, CancellationToken ct)
        => Ok(await _service.SetLifetimeAsync(userId, false, ct));

    /// <summary>Loginning kompyuterga bog'lanishini bekor qilish (kassa kompyuteri almashganda).</summary>
    [HttpPost("users/{userId:guid}/reset-device")]
    public async Task<ActionResult<OrgUserDto>> ResetDevice(Guid userId, CancellationToken ct)
        => Ok(await _service.ResetDeviceAsync(userId, ct));

    [HttpPost("users/{userId:guid}/activate")]
    public async Task<ActionResult<OrgUserDto>> ActivateUser(Guid userId, CancellationToken ct)
        => Ok(await _service.SetUserActiveAsync(userId, true, ct));

    [HttpPost("users/{userId:guid}/deactivate")]
    public async Task<ActionResult<OrgUserDto>> DeactivateUser(Guid userId, CancellationToken ct)
        => Ok(await _service.SetUserActiveAsync(userId, false, ct));

    [HttpGet("{id:guid}/roles")]
    public async Task<ActionResult<IReadOnlyList<OrgRoleDto>>> GetRoles(Guid id, CancellationToken ct)
        => Ok(await _service.GetRolesAsync(id, ct));

    /// <summary>Tashkilotga tegishli do'konlar (SuperAdmin panel uchun).</summary>
    [HttpGet("{id:guid}/stores")]
    public async Task<ActionResult<IReadOnlyList<StoreDto>>> GetStores(Guid id, CancellationToken ct)
        => Ok(await _stores.GetByOrgAsync(id, ct));

    /// <summary>Filial faoliyat turini (Do'kon/Restoran) o'zgartirish — faqat SuperAdmin.</summary>
    [HttpPut("stores/{storeId:guid}/business-type")]
    public async Task<ActionResult<StoreDto>> SetStoreBusinessType(Guid storeId, [FromBody] SetBusinessTypeRequest request, CancellationToken ct)
        => Ok(await _stores.SetBusinessTypeAsync(storeId, request.BusinessType, ct));

    [HttpPut("users/{userId:guid}/app-role")]
    public async Task<ActionResult<OrgUserDto>> AssignAppRole(Guid userId, AssignOrgUserRoleRequest request, CancellationToken ct)
        => Ok(await _service.AssignAppRoleAsync(userId, request.RoleId, ct));
}
