using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authentication;
using PosCloud.Application.Common.Models;
using PosCloud.Application.Features.Loyalty;
using PosCloud.Application.Features.Loyalty.Dtos;
using PosCloud.Application.Features.ShopCatalog;
using PosCloud.Application.Features.ShopCatalog.Dtos;
using PosCloud.Application.Features.ShopLists;
using PosCloud.Application.Features.ShopMarketing;
using PosCloud.Application.Features.ShopMarketing.Dtos;
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

    // ---------------- Bosqich 9: sevimlilar, obuna, savatcha, taklif ----------------

    /// <summary>Sevimlilar — shu filial narxi/qoldig'i bilan (boshqa tashkilot mahsuloti chiqmaydi).</summary>
    [HttpGet("favorites")]
    public async Task<ActionResult<IReadOnlyList<ShopProductDto>>> GetFavorites([FromQuery] Guid branchId,
        [FromServices] IShopCatalogService catalog, CancellationToken ct) =>
        Ok(await catalog.GetFavoritesAsync(User.GetShopUserId(), branchId, ct));

    /// <summary>Sevimli mahsulot ID'lari (yurakchalarni tez bo'yash uchun).</summary>
    [HttpGet("favorites/ids")]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GetFavoriteIds([FromServices] IShopMarketingService marketing,
        CancellationToken ct) =>
        Ok(await marketing.GetFavoriteIdsAsync(User.GetShopUserId(), ct));

    [HttpPut("favorites/{productId:guid}")]
    public async Task<IActionResult> AddFavorite(Guid productId, [FromServices] IShopMarketingService marketing,
        CancellationToken ct)
    {
        await marketing.AddFavoriteAsync(User.GetShopUserId(), productId, ct);
        return NoContent();
    }

    [HttpDelete("favorites/{productId:guid}")]
    public async Task<IActionResult> RemoveFavorite(Guid productId, [FromServices] IShopMarketingService marketing,
        CancellationToken ct)
    {
        await marketing.RemoveFavoriteAsync(User.GetShopUserId(), productId, ct);
        return NoContent();
    }

    /// <summary>"Kelganda xabar berish" obunasi.</summary>
    [HttpPut("stock-subscriptions")]
    public async Task<IActionResult> Subscribe(StockSubscriptionRequest request, [FromServices] IShopMarketingService marketing,
        CancellationToken ct)
    {
        await marketing.SubscribeStockAsync(User.GetShopUserId(), request, ct);
        return NoContent();
    }

    [HttpDelete("stock-subscriptions/{productId:guid}")]
    public async Task<IActionResult> Unsubscribe(Guid productId, [FromQuery] Guid branchId,
        [FromServices] IShopMarketingService marketing, CancellationToken ct)
    {
        await marketing.UnsubscribeStockAsync(User.GetShopUserId(), productId, branchId, ct);
        return NoContent();
    }

    /// <summary>Savatcha nusxasi (eslatma push uchun). Bo'sh items — o'chiriladi.</summary>
    [HttpPut("cart")]
    public async Task<IActionResult> SaveCart(SaveShopCartRequest request, [FromServices] IShopMarketingService marketing,
        CancellationToken ct)
    {
        await marketing.SaveCartAsync(User.GetShopUserId(), request, ct);
        return NoContent();
    }

    /// <summary>Do'stni taklif qilish: kod (birinchi so'rovda yaratiladi), bonuslar va statistika.</summary>
    [HttpGet("referral")]
    public async Task<ActionResult<ShopReferralDto>> GetReferral([FromQuery] Guid organizationId,
        [FromServices] IShopMarketingService marketing, CancellationToken ct) =>
        Ok(await marketing.GetReferralAsync(User.GetShopUserId(), organizationId, ct));

    /// <summary>Taklif kodini kiritish (faqat shu do'konda hali yetkazilgan buyurtmasi yo'q mijoz).</summary>
    [HttpPost("referral/apply")]
    public async Task<IActionResult> ApplyReferral(ApplyReferralRequest request, [FromServices] IShopMarketingService marketing,
        CancellationToken ct)
    {
        await marketing.ApplyReferralAsync(User.GetShopUserId(), request, ct);
        return NoContent();
    }

    /// <summary>Qurilma tokenini o'chirish (chiqishdan oldin yoki bildirishnomalar o'chirilganda).</summary>
    [HttpDelete("devices/{token}")]
    public async Task<IActionResult> UnregisterDevice(string token, CancellationToken ct)
    {
        await _push.UnregisterDeviceAsync(User.GetShopUserId(), token, ct);
        return NoContent();
    }

    // ---------------- oilaviy ro'yxat (11.8) ----------------

    [HttpGet("lists")]
    public async Task<ActionResult<IReadOnlyList<ShopListBriefDto>>> GetLists([FromServices] IShopListService lists,
        CancellationToken ct) => Ok(await lists.GetListsAsync(User.GetShopUserId(), ct));

    [HttpPost("lists")]
    public async Task<ActionResult<ShopListDto>> CreateList(SaveShopListRequest request, [FromServices] IShopListService lists,
        CancellationToken ct) => Ok(await lists.CreateAsync(User.GetShopUserId(), request, ct));

    [HttpGet("lists/{id:guid}")]
    public async Task<ActionResult<ShopListDto>> GetList(Guid id, [FromQuery] Guid? branchId,
        [FromServices] IShopListService lists, CancellationToken ct) =>
        Ok(await lists.GetAsync(User.GetShopUserId(), id, branchId, ct));

    [HttpPut("lists/{id:guid}")]
    public async Task<ActionResult<ShopListDto>> RenameList(Guid id, SaveShopListRequest request,
        [FromServices] IShopListService lists, CancellationToken ct) =>
        Ok(await lists.RenameAsync(User.GetShopUserId(), id, request, ct));

    [HttpDelete("lists/{id:guid}")]
    public async Task<IActionResult> DeleteList(Guid id, [FromServices] IShopListService lists, CancellationToken ct)
    {
        await lists.DeleteAsync(User.GetShopUserId(), id, ct);
        return NoContent();
    }

    [HttpPost("lists/{id:guid}/members")]
    public async Task<ActionResult<ShopListDto>> AddListMember(Guid id, AddListMemberRequest request,
        [FromServices] IShopListService lists, CancellationToken ct) =>
        Ok(await lists.AddMemberAsync(User.GetShopUserId(), id, request, ct));

    [HttpDelete("lists/{id:guid}/members/{memberId:guid}")]
    public async Task<IActionResult> RemoveListMember(Guid id, Guid memberId, [FromServices] IShopListService lists,
        CancellationToken ct)
    {
        await lists.RemoveMemberAsync(User.GetShopUserId(), id, memberId, ct);
        return NoContent();
    }

    [HttpPost("lists/{id:guid}/items")]
    public async Task<ActionResult<ShopListItemDto>> AddListItem(Guid id, AddListItemRequest request,
        [FromServices] IShopListService lists, CancellationToken ct) =>
        Ok(await lists.AddItemAsync(User.GetShopUserId(), id, request, ct));

    [HttpPut("lists/{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<ShopListItemDto>> UpdateListItem(Guid id, Guid itemId, UpdateListItemRequest request,
        [FromServices] IShopListService lists, CancellationToken ct) =>
        Ok(await lists.UpdateItemAsync(User.GetShopUserId(), id, itemId, request, ct));

    [HttpDelete("lists/{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteListItem(Guid id, Guid itemId, [FromServices] IShopListService lists,
        CancellationToken ct)
    {
        await lists.DeleteItemAsync(User.GetShopUserId(), id, itemId, ct);
        return NoContent();
    }

    [HttpPost("lists/{id:guid}/clear-checked")]
    public async Task<IActionResult> ClearCheckedListItems(Guid id, [FromServices] IShopListService lists, CancellationToken ct)
    {
        await lists.ClearCheckedAsync(User.GetShopUserId(), id, ct);
        return NoContent();
    }
}
