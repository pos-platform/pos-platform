using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Common.Interfaces;

namespace PosCloud.Api.Controllers;

public record AiImageResult(Guid Id, string Name, string? Category, string? ImageUrl, string? Error);

/// <summary>
/// Gemini (Nano Banana) orqali mahsulot rasmlarini AI bilan generatsiya qiladi.
/// Kalit: Gemini:ApiKey (user-secrets yoki Gemini__ApiKey muhit o'zgaruvchisi).
/// </summary>
[ApiController]
[Authorize]
[Route("api/products")]
public class ProductAiImagesController : ControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ProductAiImagesController> _logger;

    public ProductAiImagesController(IApplicationDbContext db, IHttpClientFactory httpFactory,
        IConfiguration config, IWebHostEnvironment env, ILogger<ProductAiImagesController> logger)
    {
        _db = db; _httpFactory = httpFactory; _config = config; _env = env; _logger = logger;
    }

    /// <summary>Bitta mahsulotga AI rasm generatsiya qiladi (eski rasm almashtiriladi).</summary>
    [HttpPost("{id:guid}/generate-image")]
    [HasPermission(Permissions.ProductsEdit)]
    public async Task<ActionResult<AiImageResult>> GenerateOne(Guid id, CancellationToken ct)
    {
        var p = await _db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound();
        return Ok(await GenerateAsync(p, ct));
    }

    /// <summary>Rasmi yo'q mahsulotlarga AI rasm generatsiya qiladi (ketma-ket, limit ta).</summary>
    [HttpPost("generate-images")]
    [HasPermission(Permissions.ProductsEdit)]
    public async Task<ActionResult<IReadOnlyList<AiImageResult>>> GenerateMissing(
        [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var products = await _db.Products.Include(x => x.Category)
            .Where(x => x.ImageUrl == null || x.ImageUrl == "")
            .OrderBy(x => x.Name).Take(limit).ToListAsync(ct);

        var results = new List<AiImageResult>();
        foreach (var p in products)
            results.Add(await GenerateAsync(p, ct));
        return Ok(results);
    }

    private async Task<AiImageResult> GenerateAsync(Domain.Entities.Product p, CancellationToken ct)
    {
        var category = p.Category?.Name;
        try
        {
            var bytes = await CallGeminiAsync(BuildPrompt(p.Name, category, p.Description), ct);
            var dir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "ai");
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid():N}.png";
            await System.IO.File.WriteAllBytesAsync(Path.Combine(dir, fileName), bytes, ct);

            p.ImageUrl = $"/uploads/ai/{fileName}";
            await _db.SaveChangesAsync(ct);
            return new AiImageResult(p.Id, p.Name, category, p.ImageUrl, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "AI rasm xatosi: {Product}", p.Name);
            return new AiImageResult(p.Id, p.Name, category, null, ex.Message);
        }
    }

    private static string BuildPrompt(string name, string? category, string? description) =>
        $"Realistic commercial product photography for a supermarket app. Product: \"{name}\""
        + (string.IsNullOrWhiteSpace(category) ? "" : $" (category: {category})")
        + (string.IsNullOrWhiteSpace(description) ? "" : $". Details: {description}")
        + ". The product name may be in Uzbek or Russian; show exactly this product as it is sold in a supermarket in Uzbekistan. "
        + "If it is a known brand, show the authentic packaging with the correct logo and correct spelling, sharp clean lettering. "
        + "If it is an unbranded or loose item (fruit, vegetables, meat, bread), show the fresh product itself. "
        + "A single product (or a natural small portion for loose goods), standing upright, centered, front view. "
        + "Pure white seamless background (#FFFFFF), soft diffused studio lighting, soft natural shadow under the product. "
        + "Sharp focus, rich colors, high detail. Product fills about 70% of the image height. Square 1:1. No watermark, no extra text.";

    private async Task<byte[]> CallGeminiAsync(string prompt, CancellationToken ct)
    {
        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Gemini:ApiKey sozlanmagan.");
        var model = _config["Gemini:ImageModel"] ?? "gemini-2.5-flash-image";

        var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(120);
        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");
        req.Headers.Add("x-goog-api-key", apiKey);
        req.Content = JsonContent.Create(new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                responseModalities = new[] { "IMAGE" },
                imageConfig = new { aspectRatio = "1:1" }
            }
        });

        using var res = await http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Gemini {(int)res.StatusCode}: {body[..Math.Min(body.Length, 300)]}");

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("candidates", out var cands))
            foreach (var cand in cands.EnumerateArray())
                if (cand.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts))
                    foreach (var part in parts.EnumerateArray())
                        if (part.TryGetProperty("inlineData", out var data))
                            return Convert.FromBase64String(data.GetProperty("data").GetString()!);

        throw new InvalidOperationException("Gemini rasm qaytarmadi: " + body[..Math.Min(body.Length, 300)]);
    }
}
