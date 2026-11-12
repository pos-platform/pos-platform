using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.OnlineStores;
using PosCloud.Application.Features.OnlineStores.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>SuperAdmin: online do'konlarni yoqish, tasdiqlash, bloklash, to'lov usullari, tavsiya, tarif muddati.</summary>
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/admin/online-stores")]
public class AdminOnlineStoresController : ControllerBase
{
    private readonly IOnlineStoreService _service;

    public AdminOnlineStoresController(IOnlineStoreService service)
    {
        _service = service;
    }

    /// <summary>filter: all | requested | enabled | blocked.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OnlineStoreListItemDto>>> GetAll(
        [FromQuery] string? filter, [FromQuery] string? search, CancellationToken ct) =>
        Ok(await _service.GetAllAsync(filter, search, ct));

    [HttpGet("{organizationId:guid}")]
    public async Task<ActionResult<OnlineStoreSettingsDto>> Get(Guid organizationId, CancellationToken ct) =>
        Ok(await _service.GetForOrganizationAsync(organizationId, ct));

    [HttpPut("{organizationId:guid}")]
    public async Task<ActionResult<OnlineStoreSettingsDto>> Update(Guid organizationId, UpdateOnlineStoreAdminRequest request,
        CancellationToken ct) =>
        Ok(await _service.UpdateAdminAsync(organizationId, request, ct));
}

/// <summary>Do'kon admini: o'z online do'koni sozlamalari. Kassa: buyurtma qabul qilishni yoqish/o'chirish.</summary>
[ApiController]
[Authorize]
[Route("api/online-store")]
public class OnlineStoreController : ControllerBase
{
    private readonly IOnlineStoreService _service;

    public OnlineStoreController(IOnlineStoreService service)
    {
        _service = service;
    }

    [HttpGet("settings")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<OnlineStoreSettingsDto>> Get(CancellationToken ct) =>
        Ok(await _service.GetMineAsync(ct));

    [HttpPut("settings")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<OnlineStoreSettingsDto>> Update(UpdateOnlineStoreSettingsRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateMineAsync(request, ct));

    /// <summary>Ilovaga chiqish so'rovi (SuperAdmin ko'rib chiqadi).</summary>
    [HttpPost("request")]
    [HasPermission(Permissions.SettingsManage)]
    public async Task<ActionResult<OnlineStoreSettingsDto>> RequestListing(CancellationToken ct) =>
        Ok(await _service.RequestListingAsync(ct));

    /// <summary>Kassadagi "Buyurtma qabul qilish" tugmasi.</summary>
    [HttpPut("accepting")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineStoreSettingsDto>> SetAccepting(SetAcceptingOrdersRequest request, CancellationToken ct) =>
        Ok(await _service.SetAcceptingOrdersAsync(request.IsAccepting, ct));
}
