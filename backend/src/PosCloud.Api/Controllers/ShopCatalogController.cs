using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authentication;
using PosCloud.Application.Common.Models;
using PosCloud.Application.Features.ShopCatalog;
using PosCloud.Application.Features.ShopCatalog.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>
/// Mobil ilova katalogi. Tokensiz ochiq — "Hozircha kirmasdan ko'rish" uchun.
/// Faqat online'da yoqilgan do'konlar va ShowOnline mahsulotlar qaytadi.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/shop/stores")]
public class ShopCatalogController : ControllerBase
{
    private readonly IShopCatalogService _service;

    public ShopCatalogController(IShopCatalogService service)
    {
        _service = service;
    }

    /// <summary>
    /// Do'konlar ro'yxati. lat/lng berilsa — masofa hisoblanadi va yaqinlari oldinda.
    /// organizationId berilsa — faqat shu tashkilot filiallari (white-label ilova).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShopStoreDto>>> GetStores(
        [FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] string? search,
        [FromQuery] Guid? organizationId, CancellationToken ct)
    {
        return Ok(await _service.GetStoresAsync(lat, lng, search, organizationId, ct));
    }

    [HttpGet("{branchId:guid}")]
    public async Task<ActionResult<ShopStoreDto>> GetStore(Guid branchId, CancellationToken ct)
    {
        return Ok(await _service.GetStoreAsync(branchId, ct));
    }

    /// <summary>Sotuvda mahsuloti bor kategoriyalar.</summary>
    [HttpGet("{branchId:guid}/categories")]
    public async Task<ActionResult<IReadOnlyList<ShopCategoryDto>>> GetCategories(Guid branchId, CancellationToken ct)
    {
        return Ok(await _service.GetCategoriesAsync(branchId, ct));
    }

    /// <summary>
    /// Mahsulotlar: ?search=&amp;categoryId=&amp;sort=popular|name|price_asc|price_desc&amp;inStockOnly=&amp;discounted=&amp;page=&amp;pageSize=
    /// Token bo'lsa (ixtiyoriy) — isFavorite to'ldiriladi.
    /// </summary>
    [HttpGet("{branchId:guid}/products")]
    public async Task<ActionResult<PagedResult<ShopProductDto>>> GetProducts(
        Guid branchId, [FromQuery] ShopProductQuery query, CancellationToken ct)
    {
        return Ok(await _service.GetProductsAsync(branchId, query, await HttpContext.TryGetShopUserIdAsync(), ct));
    }

    [HttpGet("{branchId:guid}/products/{productId:guid}")]
    public async Task<ActionResult<ShopProductDetailDto>> GetProduct(Guid branchId, Guid productId, CancellationToken ct)
    {
        return Ok(await _service.GetProductAsync(branchId, productId, await HttpContext.TryGetShopUserIdAsync(), ct));
    }

    /// <summary>Bosh sahifa: bannerlar, Top 10, kolleksiyalar, aksiyalar soni, stories; token bo'lsa — "Yana olasizmi?".</summary>
    [HttpGet("{branchId:guid}/home")]
    public async Task<ActionResult<ShopHomeDto>> GetHome(Guid branchId, CancellationToken ct) =>
        Ok(await _service.GetHomeAsync(branchId, await HttpContext.TryGetShopUserIdAsync(), ct));

    /// <summary>Eng ko'p sotilganlar (miqdor bo'yicha): ?days=7&amp;limit=10. Javobda rank 1..10.</summary>
    [HttpGet("{branchId:guid}/top")]
    public async Task<ActionResult<IReadOnlyList<ShopProductDto>>> GetTop(Guid branchId, [FromQuery] int days = 7,
        [FromQuery] int limit = 10, CancellationToken ct = default) =>
        Ok(await _service.GetTopAsync(branchId, days, limit, await HttpContext.TryGetShopUserIdAsync(), ct));

    [HttpGet("{branchId:guid}/recipes")]
    public async Task<ActionResult<IReadOnlyList<ShopRecipeBriefDto>>> GetRecipes(Guid branchId, CancellationToken ct) =>
        Ok(await _service.GetRecipesAsync(branchId, ct));

    [HttpGet("{branchId:guid}/recipes/{id:guid}")]
    public async Task<ActionResult<ShopRecipeDto>> GetRecipe(Guid branchId, Guid id, CancellationToken ct) =>
        Ok(await _service.GetRecipeAsync(branchId, id, await HttpContext.TryGetShopUserIdAsync(), ct));

    [HttpGet("{branchId:guid}/collections/{id:guid}")]
    public async Task<ActionResult<ShopCollectionDto>> GetCollection(Guid branchId, Guid id, CancellationToken ct) =>
        Ok(await _service.GetCollectionAsync(branchId, id, await HttpContext.TryGetShopUserIdAsync(), ct));

    /// <summary>"Bular bilan birga olishadi" (oxirgi 90 kun), yetmasa — shu kategoriyadan ommaboplar.</summary>
    [HttpGet("{branchId:guid}/products/{productId:guid}/related")]
    public async Task<ActionResult<IReadOnlyList<ShopProductDto>>> GetRelated(Guid branchId, Guid productId,
        [FromQuery] int limit = 10, CancellationToken ct = default) =>
        Ok(await _service.GetRelatedAsync(branchId, productId, limit, await HttpContext.TryGetShopUserIdAsync(), ct));

    /// <summary>Savatdagilar bilan birga olinadiganlar (≤10, savatdagilar chiqmaydi).</summary>
    [HttpPost("{branchId:guid}/cart-suggestions")]
    public async Task<ActionResult<IReadOnlyList<ShopProductDto>>> GetCartSuggestions(Guid branchId,
        CartSuggestionsRequest request, CancellationToken ct) =>
        Ok(await _service.GetCartSuggestionsAsync(branchId, request.ProductIds, await HttpContext.TryGetShopUserIdAsync(), ct));

    /// <summary>Yozish paytidagi takliflar: ≤8 mahsulot, ≤3 kategoriya.</summary>
    [HttpGet("{branchId:guid}/search-suggest")]
    public async Task<ActionResult<ShopSearchSuggestDto>> SearchSuggest(Guid branchId, [FromQuery] string? q,
        CancellationToken ct) =>
        Ok(await _service.SearchSuggestAsync(branchId, q, ct));
}

/// <summary>
/// Alohida (white-label) do'kon ilovasi: GET /api/shop/store-app/{slug} (tokensiz).
/// Do'kon ko'rinmasa — 404 {error:"Do'kon vaqtincha ishlamayapti"}.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/shop/store-app")]
public class ShopStoreAppController : ControllerBase
{
    private readonly IShopCatalogService _service;

    public ShopStoreAppController(IShopCatalogService service)
    {
        _service = service;
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ShopStoreAppDto>> Get(string slug, CancellationToken ct) =>
        Ok(await _service.GetStoreAppAsync(slug, ct));
}
