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
    private readonly ICurrentUser _currentUser;

    public ProductService(IApplicationDbContext db, IProductExcelService excel, ICurrentUser currentUser)
    {
        _db = db;
        _excel = excel;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _db.Products
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Branch)
            .Include(p => p.Stocks);

        // Agar foydalanuvchi do'konga biriktirilgan bo'lsa (kassir/menejer), faqat shu do'kon mahsulotlarini ko'rsin.
        if (_currentUser.BranchId is not null)
            query = query.Where(p => p.BranchId == _currentUser.BranchId || p.BranchId == null);

        var products = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return products.Select(p => ProductDto.FromEntity(p, _currentUser.BranchId)).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Branch)
            .Include(p => p.Stocks)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            throw new NotFoundException($"Mahsulot topilmadi: {id}");

        return ProductDto.FromEntity(product, _currentUser.BranchId);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Kategoriya topilmadi: {dto.CategoryId}");
        }

        var productBranchId = dto.BranchId ?? _currentUser.BranchId;

        var product = new Product
        {
            Name = dto.Name.Trim(),
            MxikCode = string.IsNullOrWhiteSpace(dto.MxikCode) ? "00000000" : dto.MxikCode.Trim(),
            Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
            CostPrice = dto.CostPrice,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            Description = dto.Description?.Trim(),
            IsVat = dto.IsVat,
            IsMarked = dto.IsMarked,
            SupplierId = dto.SupplierId,
            Unit = dto.Unit,
            IsWeighed = dto.IsWeighed,
            PluNumber = dto.PluNumber,
            Tare = dto.Tare,
            CategoryId = dto.CategoryId,
            BranchId = productBranchId
        };

        // Boshlang'ich qoldiq — faqat filial ko'rsatilgan bo'lsa
        if (dto.Quantity > 0 && productBranchId.HasValue)
        {
            product.Stocks.Add(new BranchStock
            {
                BranchId = productBranchId.Value,
                Quantity = dto.Quantity
            });
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return ProductDto.FromEntity(product, _currentUser.BranchId);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Mahsulot topilmadi: {id}");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Kategoriya topilmadi: {dto.CategoryId}");
        }

        product.Name = dto.Name.Trim();
        product.MxikCode = string.IsNullOrWhiteSpace(dto.MxikCode) ? "00000000" : dto.MxikCode.Trim();
        product.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
        product.CostPrice = dto.CostPrice;
        product.Price = dto.Price;
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

        product.BranchId = dto.BranchId;

        // Soni (qoldiq) yangilash — faqat filial ko'rsatilgan mahsulotlar uchun
        var effectiveBranchId = dto.BranchId ?? _currentUser.BranchId;
        if (effectiveBranchId.HasValue)
        {
            var stock = await _db.BranchStocks
                .FirstOrDefaultAsync(s => s.ProductId == id && s.BranchId == effectiveBranchId.Value, cancellationToken);
            if (stock is not null)
                stock.Quantity = dto.Quantity;
            else if (dto.Quantity > 0)
                _db.BranchStocks.Add(new BranchStock { ProductId = id, BranchId = effectiveBranchId.Value, Quantity = dto.Quantity });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
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

        var categories = await _db.Categories.ToListAsync(cancellationToken);
        var byName = categories.ToDictionary(c => c.Name.ToLowerInvariant(), c => c);

        // Filiallar — importda BranchName ustuni orqali topish uchun
        var branches = await _db.Branches.ToListAsync(cancellationToken);
        var branchByName = branches.ToDictionary(b => b.Name.Trim().ToLowerInvariant(), b => b);

        // Mavjud barcode'lar — takrorlanishni oldini olish uchun
        var existingBarcodes = (await _db.Products
            .Where(p => p.Barcode != null)
            .Select(p => p.Barcode!)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var row in rows)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(row.Name))
                    throw new Exception("Nomi bo'sh");

                // Kategoriya: K ustun → yo'q bo'lsa "Umumiy"
                var catName = string.IsNullOrWhiteSpace(row.CategoryName) ? "Umumiy" : row.CategoryName!.Trim();
                var key = catName.ToLowerInvariant();
                if (!byName.TryGetValue(key, out var category))
                {
                    category = new Category { Name = catName };
                    _db.Categories.Add(category);
                    byName[key] = category;
                }

                // Birlik: F(шт/кг) yoki O(Birlik) ustunlaridan
                var unitRaw = (row.UnitType ?? row.Unit ?? "").Trim().ToLowerInvariant();
                var unit = unitRaw is "кг" or "kg" or "kilogram" or "kilogramm"
                    ? Unit.Kilogram
                    : Unit.Piece;

                // Narx/naценка: agar narx yo'q bo'lsa tannarx + naценka hisoblash
                var cost = row.CostPrice ?? 0m;
                decimal price;
                if (row.Price is > 0)
                {
                    price = row.Price.Value;
                }
                else if (row.MarkupPercent is > 0)
                {
                    price = Math.Round(cost * (1 + row.MarkupPercent.Value / 100m), 0);
                }
                else
                {
                    price = cost;
                }

                var barcode = string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode!.Trim();
                if (barcode != null && existingBarcodes.Contains(barcode))
                    barcode = null; // takrorlanishni e'tiborsiz qoldiramiz

                var importQty = row.Quantity ?? 0;

                // BranchId: 1) joriy foydalanuvchi filiali, 2) Excel J ustunidagi nomi, 3) yagona filial
                Guid? importBranchId = _currentUser.BranchId;
                if (importBranchId is null && !string.IsNullOrWhiteSpace(row.BranchName))
                {
                    var bKey = row.BranchName!.Trim().ToLowerInvariant();
                    if (branchByName.TryGetValue(bKey, out var foundBranch))
                        importBranchId = foundBranch.Id;
                }
                if (importBranchId is null && branches.Count == 1)
                    importBranchId = branches[0].Id;

                var importProduct = new Product
                {
                    Name      = row.Name!.Trim(),
                    MxikCode  = string.IsNullOrWhiteSpace(row.MxikCode) ? "00000000" : row.MxikCode!.Trim(),
                    Barcode   = barcode,
                    PluNumber = row.PluNumber,
                    CostPrice = cost,
                    Price     = price,
                    IsVat     = row.IsVat ?? false,
                    IsMarked  = row.IsMarked ?? false,
                    Unit      = unit,
                    CategoryId = category.Id
                };

                if (importQty > 0 && importBranchId.HasValue)
                {
                    importProduct.Stocks.Add(new BranchStock
                    {
                        BranchId = importBranchId.Value,
                        Quantity = importQty
                    });
                }

                _db.Products.Add(importProduct);
                if (barcode != null) existingBarcodes.Add(barcode);
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
        IQueryable<Product> query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Include(p => p.Branch)
            .Include(p => p.Stocks);
        if (_currentUser.BranchId is not null)
            query = query.Where(p => p.BranchId == _currentUser.BranchId || p.BranchId == null);
        var products = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);

        var rows = products.Select(p =>
        {
            var qty = _currentUser.BranchId.HasValue
                ? p.Stocks.FirstOrDefault(s => s.BranchId == _currentUser.BranchId)?.Quantity ?? 0m
                : p.Stocks.Sum(s => s.Quantity);
            var unitType = p.Unit == Unit.Kilogram ? "кг" : "шт";
            var unitLabel = p.Unit == Unit.Kilogram ? "Kilogramm" : "Донна";
            var markup = p.CostPrice > 0
                ? Math.Round((p.Price - p.CostPrice) / p.CostPrice * 100, 2)
                : 0m;
            return new ProductExportRow
            {
                Name          = p.Name,
                Quantity      = qty,
                Barcode       = p.Barcode,
                PluNumber     = p.PluNumber,
                UnitType      = unitType,
                CostPrice     = p.CostPrice,
                Price         = p.Price,
                MarkupPercent = markup,
                BranchName    = p.Branch?.Name,
                Category      = p.Category?.Name ?? "",
                HasDiscount   = "нет",
                DiscountPrice = null,
                IsVat         = p.IsVat ? "есть" : "нет",
                Unit          = unitLabel,
                MxikCode      = p.MxikCode,
                IsMarked      = p.IsMarked ? "есть" : "нет",
                PackageCode   = null
            };
        });

        return _excel.Export(rows);
    }

    public byte[] GetImportTemplate() => _excel.BuildTemplate();
}
