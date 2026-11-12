using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    /// <summary>Do'konlar ro'yxati. lat/lng berilsa — masofa hisoblanadi va yaqinlari oldinda.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShopStoreDto>>> GetStores(
        [FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] string? search, CancellationToken ct)
    {
        return Ok(await _service.GetStoresAsync(lat, lng, search, ct));
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

    /// <summary>Mahsulotlar: ?search=&amp;categoryId=&amp;sort=popular|name|price_asc|price_desc&amp;inStockOnly=&amp;page=&amp;pageSize=</summary>
    [HttpGet("{branchId:guid}/products")]
    public async Task<ActionResult<PagedResult<ShopProductDto>>> GetProducts(
        Guid branchId, [FromQuery] ShopProductQuery query, CancellationToken ct)
    {
        return Ok(await _service.GetProductsAsync(branchId, query, ct));
    }

    [HttpGet("{branchId:guid}/products/{productId:guid}")]
    public async Task<ActionResult<ShopProductDetailDto>> GetProduct(Guid branchId, Guid productId, CancellationToken ct)
    {
        return Ok(await _service.GetProductAsync(branchId, productId, ct));
    }
}
