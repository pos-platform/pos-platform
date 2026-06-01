using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Products.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Products;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;
    private readonly IProductExcelService _excel;

    public ProductService(IApplicationDbContext db, IProductExcelService excel)
    {
        _db = db;
        _excel = excel;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Supplier)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return products.Select(ProductDto.FromEntity).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Mahsulot topilmadi: {id}");
        }

        return ProductDto.FromEntity(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Kategoriya topilmadi: {dto.CategoryId}");
        }

        var product = new Product
        {
            Name = dto.Name.Trim(),
            MxikCode = dto.MxikCode.Trim(),
            Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
            CostPrice = dto.CostPrice,
            Price = dto.Price,
            Quantity = dto.Quantity,
            ImageUrl = dto.ImageUrl,
            Description = dto.Description?.Trim(),
            IsVat = dto.IsVat,
            IsMarked = dto.IsMarked,
            SupplierId = dto.SupplierId,
            Unit = dto.Unit,
            IsWeighed = dto.IsWeighed,
            PluNumber = dto.PluNumber,
            Tare = dto.Tare,
            CategoryId = dto.CategoryId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return ProductDto.FromEntity(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Mahsulot topilmadi: {id}");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Kategoriya topilmadi: {dto.CategoryId}");
        }

        product.Name = dto.Name.Trim();
        product.MxikCode = dto.MxikCode.Trim();
        product.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
        product.CostPrice = dto.CostPrice;
        product.Price = dto.Price;
        product.Quantity = dto.Quantity;
        product.ImageUrl = dto.ImageUrl;
        product.Description = dto.Description?.Trim();
        product.IsVat = dto.IsVat;
        product.IsMarked = dto.IsMarked;
        product.SupplierId = dto.SupplierId;
        product.Unit = dto.Unit;
        product.IsWeighed = dto.IsWeighed;
        product.PluNumber = dto.PluNumber;
        product.Tare = dto.Tare;
        product.CategoryId = dto.CategoryId;

        await _db.SaveChangesAsync(cancellationToken);

        return ProductDto.FromEntity(product);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Mahsulot topilmadi: {id}");

        // Savdo/kirim/spisaniyeda ishlatilgan mahsulotni o'chirib bo'lmaydi.
        var used = await _db.SaleItems.AnyAsync(i => i.ProductId == id, cancellationToken)
            || await _db.StockReceiptItems.AnyAsync(i => i.ProductId == id, cancellationToken)
            || await _db.WriteOffItems.AnyAsync(i => i.ProductId == id, cancellationToken);
        if (used)
        {
            throw new ConflictException("Bu mahsulot savdo/kirim/spisaniyeda ishlatilgan — o'chirib bo'lmaydi.");
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default)
    {
        // 13 xonali tasodifiy raqamli kod, bazada mavjud emasligi tekshiriladi.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = "2" + Random.Shared.NextInt64(100_000_000_000L, 999_999_999_999L).ToString();
            var exists = await _db.Products.AnyAsync(p => p.Barcode == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }
        // Juda kam ehtimol — fallback sifatida vaqt asosidagi kod.
        return "2" + DateTime.UtcNow.Ticks.ToString()[^12..];
    }

    public async Task<int> GetNextPluAsync(CancellationToken cancellationToken = default)
    {
        var max = await _db.Products.MaxAsync(p => (int?)p.PluNumber, cancellationToken) ?? 0;
        return max + 1;
    }

    public async Task<ImportResultDto> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var rows = _excel.Parse(stream);
        var result = new ImportResultDto();
        if (rows.Count == 0) return result;

        // Kategoriyalarni nom bo'yicha keshlaymiz (yangi kategoriyalar yo'lda yaratiladi).
        var categories = await _db.Categories.ToListAsync(cancellationToken);
        var byName = categories.ToDictionary(c => c.Name.ToLowerInvariant(), c => c);

        foreach (var row in rows)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(row.Name))
                    throw new Exception("Nomi bo'sh");
                if (string.IsNullOrWhiteSpace(row.MxikCode))
                    throw new Exception("MXIK bo'sh");

                var catName = string.IsNullOrWhiteSpace(row.CategoryName) ? "Umumiy" : row.CategoryName!.Trim();
                var key = catName.ToLowerInvariant();
                if (!byName.TryGetValue(key, out var category))
                {
                    category = new Category { Name = catName };
                    _db.Categories.Add(category);
                    byName[key] = category;
                }

                var unit = (row.Unit ?? "").Trim().ToLowerInvariant() is "kg" or "kilogram" or "кг"
                    ? Unit.Kilogram
                    : Unit.Piece;

                _db.Products.Add(new Product
                {
                    Name = row.Name!.Trim(),
                    MxikCode = row.MxikCode!.Trim(),
                    Barcode = string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode!.Trim(),
                    CostPrice = row.CostPrice ?? 0,
                    Price = row.Price ?? 0,
                    Quantity = row.Quantity ?? 0,
                    IsVat = row.IsVat ?? true,
                    Unit = unit,
                    CategoryId = category.Id
                });
                result.Created++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add($"Qator {row.RowNumber}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<byte[]> ExportAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var rows = products.Select(p => new ProductExportRow
        {
            Name = p.Name,
            MxikCode = p.MxikCode,
            Barcode = p.Barcode,
            CostPrice = p.CostPrice,
            Price = p.Price,
            Quantity = p.Quantity,
            Unit = p.Unit == Unit.Kilogram ? "Kg" : "Dona",
            Category = p.Category?.Name ?? ""
        });

        return _excel.Export(rows);
    }

    public byte[] GetImportTemplate() => _excel.BuildTemplate();
}
