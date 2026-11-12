using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authentication;
using PosCloud.Application.Common.Models;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Application.Features.Loyalty.Dtos;
using PosCloud.Application.Features.ShopOrders;
using PosCloud.Application.Features.ShopOrders.Dtos;
using PosCloud.Application.Features.ShopProfile;
using PosCloud.Application.Features.ShopProfile.Dtos;
using PosCloud.Application.Features.ShopPush;
using PosCloud.Application.Features.ShopPush.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Mobil ilova mijozining profili va manzillari.</summary>
[ApiController]
[Authorize(Policy = ShopAuthDefaults.Policy)]
[Route("api/me")]
public class ShopMeController : ControllerBase
{
    private readonly IShopProfileService _service;
    private readonly IShopPushService _push;

    public ShopMeController(IShopProfileService service, IShopPushService push)
    {
        _service = service;
        _push = push;
    }

    [HttpGet]
    public async Task<ActionResult<ShopProfileDto>> Get(CancellationToken ct) =>
        Ok(await _service.GetAsync(User.GetShopUserId(), ct));

    [HttpPut]
    public async Task<ActionResult<ShopProfileDto>> Update(UpdateShopProfileRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(User.GetShopUserId(), request, ct));

    /// <summary>Akkauntni o'chirish. Shaxsiy ma'lumotlar tozalanadi, barcha sessiyalar yopiladi.</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        await _service.DeleteAccountAsync(User.GetShopUserId(), ct);
        return NoContent();
    }

    /// <summary>Do'kondagi bonus balli (1 ball = 1 so'm).</summary>
    [HttpGet("bonus")]
    public async Task<ActionResult<ShopBonusDto>> GetBonus([FromQuery] Guid branchId,
        [FromServices] IShopOrderService orders, CancellationToken ct) =>
        Ok(await orders.GetBonusAsync(User.GetShopUserId(), branchId, ct));

    /// <summary>Dinamik QR bonus karta (90 s). Do'konda mijoz kartochkasi bo'lmasa — yaratiladi.</summary>
    [HttpGet("bonus-card")]
    public async Task<ActionResult<BonusCardDto>> GetBonusCard([FromQuery] Guid branchId,
        [FromServices] IShopBonusService bonus, CancellationToken ct) =>
        Ok(await bonus.GetCardAsync(User.GetShopUserId(), branchId, ct));

    /// <summary>Mijoz bog'langan barcha do'konlar va ulardagi ball.</summary>
    [HttpGet("bonus/stores")]
    public async Task<ActionResult<IReadOnlyList<BonusStoreDto>>> GetBonusStores(
        [FromServices] IShopBonusService bonus, CancellationToken ct) =>
        Ok(await bonus.GetStoresAsync(User.GetShopUserId(), ct));

    /// <summary>Do'kondagi bonus tarixi (sahifalangan).</summary>
    [HttpGet("bonus/history")]
    public async Task<ActionResult<PagedResult<LoyaltyHistoryItemDto>>> GetBonusHistory([FromQuery] Guid organizationId,
        [FromServices] IShopBonusService bonus, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await bonus.GetHistoryAsync(User.GetShopUserId(), organizationId, page, pageSize, ct));

    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<ShopAddressDto>>> GetAddresses(CancellationToken ct) =>
        Ok(await _service.GetAddressesAsync(User.GetShopUserId(), ct));

    [HttpPost("addresses")]
    public async Task<ActionResult<ShopAddressDto>> CreateAddress(SaveShopAddressRequest request, CancellationToken ct) =>
        Ok(await _service.CreateAddressAsync(User.GetShopUserId(), request, ct));

    [HttpPut("addresses/{id:guid}")]
    public async Task<ActionResult<ShopAddressDto>> UpdateAddress(Guid id, SaveShopAddressRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAddressAsync(User.GetShopUserId(), id, request, ct));

    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        await _service.DeleteAddressAsync(User.GetShopUserId(), id, ct);
        return NoContent();
    }

    /// <summary>
    /// Push uchun qurilma tokenini ro'yxatdan o'tkazish (ilova har ochilganda/token yangilanganda chaqiradi).
    /// Token boshqa akkauntga tegishli bo'lsa — shu akkauntga o'tkaziladi.
    /// </summary>
    [HttpPost("devices")]
    public async Task<IActionResult> RegisterDevice(RegisterShopDeviceRequest request, CancellationToken ct)
    {
        await _push.RegisterDeviceAsync(User.GetShopUserId(), request, ct);
        return NoContent();
    }

    /// <summary>Qurilma tokenini o'chirish (chiqishdan oldin yoki bildirishnomalar o'chirilganda).</summary>
    [HttpDelete("devices/{token}")]
    public async Task<IActionResult> UnregisterDevice(string token, CancellationToken ct)
    {
        await _push.UnregisterDeviceAsync(User.GetShopUserId(), token, ct);
        return NoContent();
    }
}
