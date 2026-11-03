using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Products;
using PosCloud.Application.Features.Products.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Rasm yuklash javobi.</summary>
public record UploadImageResponse(string Url);

[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Barcha mahsulotlar ro'yxati.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }

    /// <summary>Kassa (POS) uchun yengil mahsulotlar ro'yxati — faqat zarur maydonlar.</summary>
    [HttpGet("pos")]
    [ProducesResponseType(typeof(IReadOnlyList<PosProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PosProductDto>>> GetForPos(CancellationToken cancellationToken)
        => Ok(await _productService.GetForPosAsync(cancellationToken));

    /// <summary>
    /// Kassa katalogining tamg'asi — o'ta yengil javob (~100 bayt). Kassa buni har
    /// daqiqada so'raydi va faqat oldingisidan farq qilsa to'liq ro'yxatni yuklaydi.
    /// </summary>
    [HttpGet("pos/stamp")]
    [ProducesResponseType(typeof(PosCatalogStampDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PosCatalogStampDto>> GetPosStamp(CancellationToken cancellationToken)
        => Ok(await _productService.GetPosStampAsync(cancellationToken));

    /// <summary>Id bo'yicha mahsulot.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    /// <summary>Yangi mahsulot yaratadi.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto, CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Mahsulotni tahrirlaydi.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductDto dto, CancellationToken cancellationToken)
        => Ok(await _productService.UpdateAsync(id, dto, cancellationToken));

    /// <summary>Mahsulotni o'chiradi (ishlatilmagan bo'lsa).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Mahsulotlarni import qiladi: Excel (.xlsx) yoki matn (.txt/.csv, TAB bilan).</summary>
    [HttpPost("import")]
    [Authorize(Roles = "Admin,Manager")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportResultDto>> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Fayl bo'sh." });
        }
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = file.OpenReadStream();
        var result = ext is ".txt" or ".csv" or ".tsv"
            ? await _productService.ImportTextAsync(stream, cancellationToken)
            : await _productService.ImportAsync(stream, cancellationToken);
        return Ok(result);
    }

    /// <summary>Mahsulotlarni Excel'ga eksport qiladi.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var bytes = await _productService.ExportAsync(cancellationToken);
        return File(bytes, XlsxMime, "mahsulotlar.xlsx");
    }

    /// <summary>Import shablonini yuklab beradi.</summary>
    [HttpGet("import-template")]
    public IActionResult ImportTemplate()
        => File(_productService.GetImportTemplate(), XlsxMime, "mahsulot-shablon.xlsx");

    /// <summary>Matnli (.txt) import shablonini yuklab beradi.</summary>
    [HttpGet("import-template-txt")]
    public IActionResult ImportTemplateTxt()
        => File(_productService.GetTextImportTemplate(), "text/plain; charset=utf-8", "mahsulot-shablon.txt");

    /// <summary>Noyob shtrix-kod (kod tovara) generatsiya qiladi.</summary>
    [HttpGet("generate-barcode")]
    public async Task<ActionResult<object>> GenerateBarcode(CancellationToken cancellationToken)
        => Ok(new { barcode = await _productService.GenerateBarcodeAsync(cancellationToken) });

    /// <summary>Keyingi bo'sh PLU raqami.</summary>
    [HttpGet("next-plu")]
    public async Task<ActionResult<object>> NextPlu(CancellationToken cancellationToken)
        => Ok(new { plu = await _productService.GetNextPluAsync(cancellationToken) });

    /// <summary>Mahsulot rasmini yuklaydi va URL qaytaradi.</summary>
    [HttpPost("image")]
    [ProducesResponseType(typeof(UploadImageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<ActionResult<UploadImageResponse>> UploadImage(
        IFormFile file,
        [FromServices] IWebHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Fayl bo'sh." });
        }

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
        {
            return BadRequest(new { error = "Faqat rasm fayllari (jpg, png, webp, gif) ruxsat etiladi." });
        }

        var uploadsRoot = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        // Frontend uchun nisbiy URL (statik fayllar orqali xizmat qilinadi).
        return Ok(new UploadImageResponse($"/uploads/{fileName}"));
    }
}
