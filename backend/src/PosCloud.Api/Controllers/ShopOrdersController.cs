using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authentication;
using PosCloud.Application.Common.Models;
using PosCloud.Application.Features.ShopOrders;
using PosCloud.Application.Features.ShopOrders.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Mobil ilova mijozining buyurtmalari.</summary>
[ApiController]
[Authorize(Policy = ShopAuthDefaults.Policy)]
[Route("api/shop/orders")]
public class ShopOrdersController : ControllerBase
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".heic"];

    private readonly IShopOrderService _service;

    public ShopOrdersController(IShopOrderService service)
    {
        _service = service;
    }

    /// <summary>Savatcha hisobi: server narxi, qoldiq, yetkazish, minimal summa. Saqlamaydi; kirmasdan ham ishlaydi.</summary>
    [HttpPost("quote")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopOrderQuoteDto>> Quote(ShopOrderQuoteRequest request, CancellationToken ct) =>
        Ok(await _service.QuoteAsync(request, ct));

    [HttpPost]
    public async Task<ActionResult<ShopOrderDto>> Create(CreateShopOrderRequest request, CancellationToken ct) =>
        Ok(await _service.CreateAsync(User.GetShopUserId(), request, ct));

    /// <summary>active=true — faol, false — tarix, berilmasa — hammasi.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ShopOrderSummaryDto>>> GetList(
        [FromQuery] bool? active, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _service.GetListAsync(User.GetShopUserId(), active, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShopOrderDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _service.GetAsync(User.GetShopUserId(), id, ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ShopOrderDto>> Cancel(Guid id, CancelShopOrderRequest? request, CancellationToken ct) =>
        Ok(await _service.CancelAsync(User.GetShopUserId(), id, request?.Reason, ct));

    /// <summary>O'tkazma cheki rasmi (multipart, "file", 5 MB gacha).</summary>
    [HttpPost("{id:guid}/transfer-proof")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ShopOrderDto>> UploadTransferProof(Guid id, IFormFile file,
        [FromServices] IWebHostEnvironment env, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Fayl bo'sh." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
            return BadRequest(new { error = "Faqat rasm (jpg, png, webp, heic) yuklash mumkin." });

        var userId = User.GetShopUserId();
        await _service.EnsureCanUploadTransferProofAsync(userId, id, ct);

        var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "transfers");
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}{ext}";

        await using (var stream = System.IO.File.Create(Path.Combine(dir, fileName)))
        {
            await file.CopyToAsync(stream, ct);
        }

        return Ok(await _service.SetTransferProofAsync(userId, id, $"/uploads/transfers/{fileName}", ct));
    }
}
