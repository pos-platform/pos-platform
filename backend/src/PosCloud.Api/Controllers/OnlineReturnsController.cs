using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authentication;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.OnlineReturns;
using PosCloud.Application.Features.OnlineReturns.Dtos;
using PosCloud.Domain.Enums;

namespace PosCloud.Api.Controllers;

/// <summary>Xodim: online qaytarishlar (sales.view — ko'rish, sales.create — amallar).</summary>
[ApiController]
[Authorize]
[Route("api/online-returns")]
public class OnlineReturnsController : ControllerBase
{
    private readonly IOnlineReturnService _service;

    public OnlineReturnsController(IOnlineReturnService service) => _service = service;

    [HttpGet]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<OnlineReturnDto>>> GetList([FromQuery] OnlineReturnStatus? status,
        [FromQuery] Guid? branchId, CancellationToken ct) =>
        Ok(await _service.GetListAsync(status, branchId, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<OnlineReturnDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _service.GetAsync(id, ct));

    [HttpPost("{id:guid}/approve")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineReturnDto>> Approve(Guid id, ApproveReturnRequest request, CancellationToken ct) =>
        Ok(await _service.ApproveAsync(id, request.Method, ct));

    [HttpPost("{id:guid}/reject")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineReturnDto>> Reject(Guid id, RejectReturnRequest request, CancellationToken ct) =>
        Ok(await _service.RejectAsync(id, request.Reason, ct));

    [HttpPost("{id:guid}/assign-courier")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineReturnDto>> AssignCourier(Guid id, AssignReturnCourierRequest request, CancellationToken ct) =>
        Ok(await _service.AssignCourierAsync(id, request.CourierId, ct));

    /// <summary>Tovar do'konga keldi: qisman refund (keshbek ayiriladi; Bonus usulida pul o'rniga ball). Ochiq smena kerak (Bonusdan tashqari).</summary>
    [HttpPost("{id:guid}/received")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineReturnDto>> Received(Guid id, CancellationToken ct) =>
        Ok(await _service.ReceivedAsync(id, ct));

    /// <summary>Pul berildi / o'tkazildi.</summary>
    [HttpPost("{id:guid}/refunded")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineReturnDto>> Refunded(Guid id, CancellationToken ct) =>
        Ok(await _service.RefundedAsync(id, ct));
}

/// <summary>Mobil mijoz: qaytarish so'rovlari.</summary>
[ApiController]
[Authorize(Policy = ShopAuthDefaults.Policy)]
[Route("api/shop")]
public class ShopReturnsController : ControllerBase
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".heic"];
    private const long MaxPhotoBytes = 5 * 1024 * 1024;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IOnlineReturnService _service;

    public ShopReturnsController(IOnlineReturnService service) => _service = service;

    /// <summary>
    /// Multipart: items (JSON: [{ onlineOrderItemId | orderItemId, quantity }]), reason, comment?, refundMethod, photos[] (≤3, 5 MB).
    /// Rasmlar ochiq /uploads/returns/... manzilida saqlanadi.
    /// </summary>
    [HttpPost("orders/{id:guid}/returns")]
    [RequestSizeLimit(16 * 1024 * 1024)]
    public async Task<ActionResult<OnlineReturnDto>> Create(Guid id, [FromForm] string items, [FromForm] OnlineReturnReason reason,
        [FromForm] string? comment, [FromForm] ReturnRefundMethod refundMethod, [FromForm] List<IFormFile>? photos,
        [FromServices] IWebHostEnvironment env, CancellationToken ct)
    {
        List<CreateReturnItemRequest>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<CreateReturnItemRequest>>(items ?? "[]", Json);
        }
        catch (JsonException)
        {
            return BadRequest(new { status = 400, error = "items JSON noto'g'ri." });
        }

        photos ??= [];
        // "photos[]" nomi bilan kelganlar ham.
        photos.AddRange(Request.Form.Files.Where(f => f.Name == "photos[]"));
        if (photos.Count > OnlineReturnService.MaxPhotos)
            return BadRequest(new { status = 400, error = $"Ko'pi bilan {OnlineReturnService.MaxPhotos} ta rasm." });
        foreach (var f in photos)
        {
            var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
            if (f.Length == 0 || f.Length > MaxPhotoBytes || !AllowedImageExtensions.Contains(ext))
                return BadRequest(new { status = 400, error = "Faqat rasm (jpg, png, webp, heic), 5 MB gacha." });
        }

        var userId = User.GetShopUserId();
        await _service.EnsureCanCreateAsync(userId, id, ct);

        var urls = new List<string>();
        if (photos.Count > 0)
        {
            var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "returns");
            Directory.CreateDirectory(dir);
            foreach (var f in photos)
            {
                var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(f.FileName).ToLowerInvariant()}";
                await using var stream = System.IO.File.Create(Path.Combine(dir, fileName));
                await f.CopyToAsync(stream, ct);
                urls.Add($"/uploads/returns/{fileName}");
            }
        }

        return Ok(await _service.CreateAsync(userId, id, new CreateReturnRequest
        {
            Items = parsed ?? [],
            Reason = reason,
            Comment = comment,
            RefundMethod = refundMethod,
            PhotoUrls = urls
        }, ct));
    }

    [HttpGet("returns")]
    public async Task<ActionResult<IReadOnlyList<OnlineReturnDto>>> GetList(CancellationToken ct) =>
        Ok(await _service.GetMineAsync(User.GetShopUserId(), ct));

    [HttpGet("returns/{id:guid}")]
    public async Task<ActionResult<OnlineReturnDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _service.GetMineAsync(User.GetShopUserId(), id, ct));

    [HttpPost("returns/{id:guid}/cancel")]
    public async Task<ActionResult<OnlineReturnDto>> Cancel(Guid id, CancellationToken ct) =>
        Ok(await _service.CancelMineAsync(User.GetShopUserId(), id, ct));
}
