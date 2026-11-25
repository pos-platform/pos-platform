using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Products.Dtos;
using PosCloud.Application.Features.Stock;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Products;

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
        // Xizmat (yetkazish) mahsuloti yashirin — avtomatik boshqariladi.
        var branchId = _currentUser.BranchId;
        IQueryable<Product> query = _db.Products.AsNoTracking().Where(p => p.ProductType != ProductType.Service);

        // Agar foydalanuvchi do'konga biriktirilgan bo'lsa (kassir/menejer), faqat shu do'kon mahsulotlarini ko'rsin.
        if (branchId is not null)
            query = query.Where(p => p.BranchId == branchId || p.BranchId == null);

        // SQL proyeksiyasi: Include(Stocks) bilan butun entity'larni yuklash o'rniga faqat
        // kerakli ustunlar o'qiladi, qoldiq esa subquery orqali SQL'da yig'iladi —
        // o'n minglab mahsulotda bu ancha tez va kam xotira oladi.
        // Yangi qo'shilgan mahsulotlar birinchi chiqadi.
        var list = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                ProductType = p.ProductType,
                PrepMinutes = p.PrepMinutes,
                MxikCode = p.MxikCode,
                MxikSuggestion = p.MxikSuggestion,
                MxikSuggestionSource = p.MxikSuggestionSource,
                Barcode = p.Barcode,
                CostPrice = p.CostPrice,
                Price = p.Price,
                Quantity = p.Stocks
                    .Where(s => branchId == null || s.BranchId == branchId)
                    .Sum(s => s.Quantity),
                ImageUrl = p.ImageUrl,
                ShowOnline = p.ShowOnline,
                IsPinned = p.IsPinned,
                Description = p.Description,
                IsVat = p.IsVat,
                IsMarked = p.IsMarked,
                MarkingCode = p.MarkingCode,
                PackageCode = p.PackageCode,
                CommitentTin = p.CommitentTin,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier != null ? p.Supplier.Name : null,
                Unit = p.Unit,
                IsWeighed = p.IsWeighed,
                PluNumber = p.PluNumber,
                Tare = p.Tare,
                CategoryId = p.CategoryId,
                BranchId = p.BranchId,
                BranchName = p.Branch != null ? p.Branch.Name : null,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        foreach (var d in list)
            d.MarkupPercent = d.CostPrice > 0 ? Math.Round((d.Price - d.CostPrice) / d.CostPrice * 100m, 2) : 0m;
        return list;
    }

    public async Task<IReadOnlyList<PosProductDto>> GetForPosAsync(CancellationToken cancellationToken = default)
    {
        var branchId = _currentUser.BranchId;

        IQueryable<Product> query = _db.Products.AsNoTracking().Where(p => p.ProductType != ProductType.Service);
        if (branchId is not null)
            query = query.Where(p => p.BranchId == branchId || p.BranchId == null);

        // Qoldig'i 0 yoki minus bo'lgan tovar kassaga chiqmaydi (taom/ingredientlarga tegilmaydi).
        query = query.Where(p => p.ProductType != ProductType.Good
            || p.Stocks.Where(s => branchId == null || s.BranchId == branchId).Sum(s => s.Quantity) > 0);

        // SQL proyeksiyasi: faqat kerakli ustunlar o'qiladi, Supplier/Branch JOIN qilinmaydi,
        // qoldiq esa subquery orqali hisoblanadi (BranchStocks indeksidan foydalanadi).
        // branchId ko'rsatilsa — shu filial qoldig'i (unikal indeks tufayli bitta yozuv),
        // aks holda (admin) — barcha filiallar yig'indisi.
        var list = await query
            .OrderBy(p => p.Name)
            .Select(p => new PosProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Barcode = p.Barcode,
                MxikCode = p.MxikCode,
                IsVat = p.IsVat,
                IsMarked = p.IsMarked,
                CommitentTin = p.CommitentTin,
                Price = p.Price,
                Quantity = p.Stocks
                    .Where(s => branchId == null || s.BranchId == branchId)
                    .Sum(s => s.Quantity),
                Unit = p.Unit,
                IsWeighed = p.IsWeighed,
                PluNumber = p.PluNumber,
                CategoryId = p.CategoryId,
                ImageUrl = p.ImageUrl,
                IsPinned = p.IsPinned
            })
            .ToListAsync(cancellationToken);

        // Ommaboplik: oxirgi 90 kunda har bir tovar nechta savdo qatorida uchragan.
        // Kassa ro'yxatni shu bo'yicha (ko'pi tepada) tartiblaydi.
        var since = DateTime.UtcNow.AddDays(-90);
        var sales = _db.SaleItems.AsNoTracking()
            .Where(i => i.Sale!.Type == SaleType.Sale && i.Sale.CreatedAt >= since);
        if (branchId is not null)
            sales = sales.Where(i => i.Sale!.BranchId == branchId);
        var counts = await sales
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count, cancellationToken);

        foreach (var p in list)
            if (counts.TryGetValue(p.Id, out var c)) p.SoldCount = c;

        // Qadalgan tovarlar eng tepada, keyin ommaboplik bo'yicha.
        return list
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.SoldCount)
            .ThenBy(p => p.Name)
            .ToList();
    }

    public async Task<PosCatalogStampDto> GetPosStampAsync(CancellationToken cancellationToken = default)
    {
        var branchId = _currentUser.BranchId;

        // Kassaga ko'rinadigan mahsulotlar — GetForPosAsync bilan bir xil filtr.
        IQueryable<Product> products = _db.Products.AsNoTracking().Where(p => p.ProductType != ProductType.Service);
        if (branchId is not null)
            products = products.Where(p => p.BranchId == branchId || p.BranchId == null);

        // Qoldiq alohida jadvalda o'zgaradi (savdo, kirim, spisaniye) — uni ham
        // tamg'aga qo'shmasak, boshqa kassadagi savdodan keyin qoldiq eskirib qolardi.
        IQueryable<BranchStock> stocks = _db.BranchStocks.AsNoTracking();
        if (branchId is not null)
            stocks = stocks.Where(s => s.BranchId == branchId);

        var categories = _db.Categories.AsNoTracking();

        // Beshta yengil agregat: COUNT va MAX — qatorlarning o'zi tortilmaydi.
        var productCount = await products.CountAsync(cancellationToken);
        var productsAt = await products.MaxAsync(p => (DateTime?)(p.UpdatedAt ?? p.CreatedAt), cancellationToken);
        var stocksAt = await stocks.MaxAsync(s => (DateTime?)(s.UpdatedAt ?? s.CreatedAt), cancellationToken);
        var categoryCount = await categories.CountAsync(cancellationToken);
        var categoriesAt = await categories.MaxAsync(c => (DateTime?)(c.UpdatedAt ?? c.CreatedAt), cancellationToken);

        return new PosCatalogStampDto
        {
            ProductCount = productCount,
            ProductsUpdatedAt = productsAt,
            StocksUpdatedAt = stocksAt,
            CategoryCount = categoryCount,
            CategoriesUpdatedAt = categoriesAt
        };
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
        // Xizmat turi faqat avtomatik ("Yetkazib berish xizmati") — qo'lda yaratilmaydi.
        if (dto.ProductType == ProductType.Service)
            throw new ValidationException("Xizmat turidagi mahsulot avtomatik yaratiladi — tovar tanlang.");
        // Tizim faqat do'konlar uchun — taom (Dish) turi yaratilmaydi.
        if (dto.ProductType == ProductType.Dish)
            throw new ValidationException("Taom turi qo'llab-quvvatlanmaydi — tovar tanlang.");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Kategoriya topilmadi: {dto.CategoryId}");
        }

        var productBranchId = dto.BranchId ?? _currentUser.BranchId;

        var product = new Product
        {
            Name = dto.Name.Trim(),
            ProductType = dto.ProductType,
            PrepMinutes = dto.PrepMinutes,
            MxikCode = Mxik.Require(dto.MxikCode, dto.Name.Trim()),
            Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
            CostPrice = dto.CostPrice,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
            ShowOnline = dto.ShowOnline ?? true,
            Description = dto.Description?.Trim(),
            IsVat = dto.IsVat,
            IsMarked = dto.IsMarked,
            MarkingCode = string.IsNullOrWhiteSpace(dto.MarkingCode) ? null : dto.MarkingCode.Trim(),
            PackageCode = string.IsNullOrWhiteSpace(dto.PackageCode) ? null : dto.PackageCode.Trim(),
            CommitentTin = string.IsNullOrWhiteSpace(dto.CommitentTin) ? null : dto.CommitentTin.Trim(),
            SupplierId = dto.SupplierId,
            Unit = dto.Unit,
            IsWeighed = dto.IsWeighed,
            PluNumber = dto.PluNumber,
            Tare = dto.Tare,
            CategoryId = dto.CategoryId,
            BranchId = productBranchId
        };

        _db.Products.Add(product);

        // Boshlang'ich qoldiq — faqat filial ko'rsatilgan bo'lsa (ombor harakati jurnaliga "Initial" bo'lib yoziladi).
        // Keyin qoldiq faqat hujjatlar (kirim, hisobdan chiqarish, inventarizatsiya...) orqali o'zgaradi.
        if (dto.Quantity > 0 && productBranchId.HasValue)
            AddInitialStock(product, productBranchId.Value, dto.Quantity);
        await _db.SaveChangesAsync(cancellationToken);

        return ProductDto.FromEntity(product, _currentUser.BranchId);
    }

    public async Task<BulkShowOnlineResult> SetShowOnlineAsync(BulkShowOnlineRequest request,
        CancellationToken cancellationToken = default)
    {
        var ids = request.ProductIds.Distinct().ToList();
        if (ids.Count == 0) return new BulkShowOnlineResult(0);
        if (ids.Count > 1000)
            throw new ValidationException("Bir so'rovda ko'pi bilan 1000 ta mahsulot.");

        // Tenant filtri ishlaydi — boshqa tashkilot mahsuloti topilmaydi. Faqat qiymati farq qiladiganlar yuklanadi
        // (ko'pi bilan 1000 ta); SaveChanges UpdatedAt'ni yangilaydi — kassa katalog tamg'asi o'zgarishni sezadi.
        var show = request.ShowOnline;
        var products = await _db.Products
            .Where(p => ids.Contains(p.Id) && p.ShowOnline != show)
            .ToListAsync(cancellationToken);
        foreach (var p in products)
            p.ShowOnline = show;

        if (products.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return new BulkShowOnlineResult(products.Count);
    }

    public async Task<BulkShowOnlineResult> SetPinnedAsync(BulkPinRequest request,
        CancellationToken cancellationToken = default)
    {
        var ids = request.ProductIds.Distinct().ToList();
        if (ids.Count == 0) return new BulkShowOnlineResult(0);
        if (ids.Count > 1000)
            throw new ValidationException("Bir so'rovda ko'pi bilan 1000 ta mahsulot.");

        // SaveChanges UpdatedAt'ni yangilaydi — kassa katalog tamg'asi o'zgarishni sezadi.
        var pin = request.IsPinned;
        var products = await _db.Products
            .Where(p => ids.Contains(p.Id) && p.IsPinned != pin)
            .ToListAsync(cancellationToken);
        foreach (var p in products)
            p.IsPinned = pin;

        if (products.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
        return new BulkShowOnlineResult(products.Count);
    }

    public async Task<BulkShowOnlineResult> SetCategoryAsync(BulkCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var ids = request.ProductIds.Distinct().ToList();
        if (ids.Count == 0) return new BulkShowOnlineResult(0);
        if (ids.Count > 1000)
            throw new ValidationException("Bir so'rovda ko'pi bilan 1000 ta mahsulot.");

        // Tenant filtri — boshqa tashkilot kategoriyasi/mahsuloti topilmaydi.
        if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
            throw new NotFoundException($"Kategoriya topilmadi: {request.CategoryId}");

        var categoryId = request.CategoryId;
        var products = await _db.Products
            .Where(p => ids.Contains(p.Id) && p.CategoryId != categoryId)
            .ToListAsync(cancellationToken);
        foreach (var p in products)
            p.CategoryId = categoryId;

        if (products.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return new BulkShowOnlineResult(products.Count);
    }

    public async Task<BulkShowOnlineResult> SetImagesAsync(BulkImageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count > 1000)
            throw new ValidationException("Bir so'rovda ko'pi bilan 1000 ta mahsulot.");
        var map = request.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.ImageUrl))
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Last().ImageUrl.Trim());
        if (map.Count == 0) return new BulkShowOnlineResult(0);

        var ids = map.Keys.ToList();
        var query = _db.Products.Where(p => ids.Contains(p.Id));
        if (request.OnlyIfEmpty)
            query = query.Where(p => p.ImageUrl == null || p.ImageUrl == "");
        var products = await query.ToListAsync(cancellationToken);

        var changed = 0;
        foreach (var p in products)
        {
            var url = map[p.Id];
            if (p.ImageUrl == url) continue;
            p.ImageUrl = url;
            changed++;
        }

        if (changed > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return new BulkShowOnlineResult(changed);
    }

    public async Task<BulkShowOnlineResult> SetMxikGuessesAsync(BulkMxikRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items.Count > 1000)
            throw new ValidationException("Bir so'rovda ko'pi bilan 1000 ta mahsulot.");
        var bad = request.Items.FirstOrDefault(i => !Mxik.IsValid(i.MxikCode?.Trim()));
        if (bad is not null)
            throw new ValidationException($"MXIK kodi {Mxik.Length} ta raqam bo'lishi shart: '{bad.MxikCode}'.");

        var map = request.Items.GroupBy(i => i.ProductId).ToDictionary(g => g.Key, g => g.Last());
        if (map.Count == 0) return new BulkShowOnlineResult(0);

        var ids = map.Keys.ToList();
        var products = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

        // Faqat TAKLIF yoziladi — MxikCode (chekka tushadigan) o'zgarmaydi.
        var changed = 0;
        foreach (var p in products)
        {
            if (request.OnlyIfInvalid && Mxik.IsValid(p.MxikCode)) continue;
            var item = map[p.Id];
            p.MxikSuggestion = item.MxikCode.Trim();
            p.MxikSuggestionSource = string.IsNullOrWhiteSpace(item.Source) ? null : item.Source.Trim();
            changed++;
        }

        if (changed > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return new BulkShowOnlineResult(changed);
    }

    public async Task<BulkShowOnlineResult> ApplyMxikSuggestionsAsync(ApplyMxikSuggestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Products.Where(p => p.MxikSuggestion != null);
        if (request.ProductIds.Count > 0)
        {
            var ids = request.ProductIds.Distinct().ToList();
            query = query.Where(p => ids.Contains(p.Id));
        }

        var products = await query.ToListAsync(cancellationToken);
        var changed = 0;
        foreach (var p in products)
        {
            // To'g'ri MXIK bor bo'lsa ustidan yozilmaydi; yaroqsiz taklif qo'llanmaydi.
            if (Mxik.IsValid(p.MxikCode) || !Mxik.IsValid(p.MxikSuggestion)) continue;
            p.MxikCode = p.MxikSuggestion!;
            p.MxikSuggestion = null;
            p.MxikSuggestionSource = null;
            changed++;
        }

        if (changed > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return new BulkShowOnlineResult(changed);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Mahsulot topilmadi: {id}");

        // Xizmat turi faqat avtomatik ("Yetkazib berish xizmati") — qo'lda yaratilmaydi.
        if (dto.ProductType == ProductType.Service)
            throw new ValidationException("Xizmat turidagi mahsulot avtomatik yaratiladi — tovar tanlang.");
        // Tizim faqat do'konlar uchun — taom (Dish) turi yaratilmaydi.
        if (dto.ProductType == ProductType.Dish)
            throw new ValidationException("Taom turi qo'llab-quvvatlanmaydi — tovar tanlang.");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new NotFoundException($"Kategoriya topilmadi: {dto.CategoryId}");
        }

        product.Name = dto.Name.Trim();
        product.ProductType = dto.ProductType;
        product.PrepMinutes = dto.PrepMinutes;
        product.MxikCode = Mxik.Require(dto.MxikCode, dto.Name.Trim());
        // To'g'ri MXIK saqlandi — taklif endi kerak emas.
        product.MxikSuggestion = null;
        product.MxikSuggestionSource = null;
        product.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
        product.CostPrice = dto.CostPrice;
        product.Price = dto.Price;
        product.ImageUrl = dto.ImageUrl;
        if (dto.ShowOnline is { } showOnline) product.ShowOnline = showOnline;
        product.Description = dto.Description?.Trim();
        product.IsVat = dto.IsVat;
        product.IsMarked = dto.IsMarked;
        product.MarkingCode = string.IsNullOrWhiteSpace(dto.MarkingCode) ? null : dto.MarkingCode.Trim();
        product.PackageCode = string.IsNullOrWhiteSpace(dto.PackageCode) ? null : dto.PackageCode.Trim();
        product.CommitentTin = string.IsNullOrWhiteSpace(dto.CommitentTin) ? null : dto.CommitentTin.Trim();
        product.SupplierId = dto.SupplierId;
        product.Unit = dto.Unit;
        product.IsWeighed = dto.IsWeighed;
        product.PluNumber = dto.PluNumber;
        product.Tare = dto.Tare;
        product.CategoryId = dto.CategoryId;

        product.BranchId = dto.BranchId;

        // Qoldiq bu yerda O'ZGARMAYDI (dto.Quantity e'tiborsiz): forma ochilgandan beri bo'lgan
        // sotuvlar ustidan yozib yuborilmasin. Qoldiq faqat kirim / hisobdan chiqarish /
        // inventarizatsiya hujjatlari orqali o'zgaradi va har biri harakat jurnaliga yoziladi.

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
        // MUHIM: "2" bilan boshlamaymiz — POS "2" + 13 xonali kodni tarozi vazn-kodi deb o'qiydi.
        // Ichki (restricted) "04" prefiksi bilan boshlaymiz: haqiqiy tovar EAN'lari bilan ham,
        // tarozi vazn-kodlari bilan ham to'qnashmaydi.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = "04" + Random.Shared.NextInt64(10_000_000_000L, 99_999_999_999L).ToString();
            var exists = await _db.Products.AnyAsync(p => p.Barcode == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }
        // Juda kam ehtimol — fallback sifatida vaqt asosidagi kod.
        return "04" + DateTime.UtcNow.Ticks.ToString()[^11..];
    }

    public async Task<int> GetNextPluAsync(CancellationToken cancellationToken = default)
    {
        var max = await _db.Products.MaxAsync(p => (int?)p.PluNumber, cancellationToken) ?? 0;
        return max + 1;
    }

    public async Task<ImportResultDto> ImportAsync(Stream stream, CancellationToken cancellationToken = default)
        => await ProcessImportAsync(_excel.Parse(stream), cancellationToken);

    public async Task<ImportResultDto> ImportTextAsync(Stream stream, CancellationToken cancellationToken = default)
        => await ProcessImportAsync(_excel.ParseText(stream), cancellationToken);

    private async Task<ImportResultDto> ProcessImportAsync(IReadOnlyList<ProductImportRow> rows, CancellationToken cancellationToken)
    {
        var result = new ImportResultDto();
        if (rows.Count == 0) return result;

        var categories = await _db.Categories.ToListAsync(cancellationToken);
        // Turli bo'limlarda bir xil nomli ichki bo'lim bo'lishi mumkin — mahsulot ichki bo'limga (bargga) tushsin.
        var byName = categories
            .GroupBy(c => c.Name.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.ParentId == null).First());

        // Filiallar — importda BranchName ustuni orqali topish uchun
        var branches = await _db.Branches.ToListAsync(cancellationToken);

        // Agar tashkilotda birorta ham filial bo'lmasa — Excel'dagi nom (yoki "Asosiy do'kon")
        // bilan avtomatik yaratamiz, aks holda qoldiq (soni) hech qaerga yozilmaydi.
        if (branches.Count == 0)
        {
            var firstBranchName = rows
                .Select(r => r.BranchName)
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))?.Trim();
            var autoBranch = new Branch
            {
                Name = string.IsNullOrWhiteSpace(firstBranchName) ? "Asosiy do'kon" : firstBranchName!
            };
            _db.Branches.Add(autoBranch); // OrganizationId SaveChanges'da avtomatik to'ldiriladi
            branches.Add(autoBranch);
        }

        var branchByName = new Dictionary<string, Branch>();
        foreach (var b in branches)
            branchByName.TryAdd(NormalizeBranchKey(b.Name), b);

        // Mavjud mahsulotlar — importda yangilash (upsert) uchun tracking bilan yuklanadi.
        // Moslik kaliti: FAQAT barcode → PLU. Nom bo'yicha moslik ATAYIN yo'q —
        // katalogda bir xil nomli, ammo har xil barcode'li tovarlar juda ko'p
        // (masalan "Garnier" x34), va nom bilan moslashtirilsa ular bir-biriga
        // qo'shilib ketardi (ba'zi tovarlar import'da "yo'qolardi"). Exceldagi
        // har bir qator o'z barcode/PLU'si bilan alohida tovar bo'lib qolsin.
        var existingProducts = await _db.Products
            .Include(p => p.Stocks)
            .ToListAsync(cancellationToken);

        var byBarcode = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        var byPlu = new Dictionary<int, Product>();
        foreach (var ep in existingProducts)
        {
            if (!string.IsNullOrWhiteSpace(ep.Barcode))
                byBarcode.TryAdd(ep.Barcode!.Trim(), ep);
            if (ep.PluNumber is > 0)
                byPlu.TryAdd(ep.PluNumber.Value, ep);
        }

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

                // Mavjud mahsulotni topish: FAQAT barcode → PLU.
                // Nom bo'yicha izlash yo'q — aks holda bir xil nomli tovarlar birlashib ketadi.
                // Barcode ham, PLU ham topilmasa — bu yangi tovar (har doim qo'shiladi).
                Product? existing = null;
                if (barcode != null) byBarcode.TryGetValue(barcode, out existing);
                if (existing is null && row.PluNumber is > 0) byPlu.TryGetValue(row.PluNumber.Value, out existing);

                // BranchId: 1) joriy foydalanuvchi filiali, 2) Excel J ustunidagi nomi, 3) birinchi filial
                Guid? importBranchId = _currentUser.BranchId;
                if (importBranchId is null && !string.IsNullOrWhiteSpace(row.BranchName))
                {
                    var bKey = NormalizeBranchKey(row.BranchName!);
                    if (branchByName.TryGetValue(bKey, out var foundBranch))
                        importBranchId = foundBranch.Id;
                }
                if (importBranchId is null && branches.Count > 0)
                    importBranchId = branches[0].Id;

                if (existing is not null)
                {
                    // --- YANGILASH (upsert): faqat importda mavjud qiymatlar ustiga yoziladi ---
                    existing.Name = row.Name!.Trim();

                    // Barcode faqat bo'sh yoki shu mahsulotning o'ziga tegishli bo'lsa yangilanadi
                    if (barcode != null && (!byBarcode.TryGetValue(barcode, out var owner) || ReferenceEquals(owner, existing)))
                    {
                        existing.Barcode = barcode;
                        byBarcode[barcode] = existing;
                    }
                    if (row.PluNumber is > 0)
                    {
                        existing.PluNumber = row.PluNumber;
                        byPlu[row.PluNumber.Value] = existing;
                    }
                    if (row.CostPrice.HasValue) existing.CostPrice = cost;
                    // Narx faqat import narx yoki ustama bergan bo'lsa qayta yoziladi (aks holda eskisi saqlanadi)
                    if (row.Price is > 0 || row.MarkupPercent is > 0) existing.Price = price;
                    if (!string.IsNullOrWhiteSpace(unitRaw)) existing.Unit = unit;
                    // Kilogramli mahsulot tarozida sotiladi — kassa tarozi shtrix-kodini shu belgi orqali taniydi.
                    if (unit == Unit.Kilogram) existing.IsWeighed = true;
                    if (!string.IsNullOrWhiteSpace(row.MxikCode)) existing.MxikCode = Mxik.Require(row.MxikCode, existing.Name);
                    if (row.IsVat.HasValue) existing.IsVat = row.IsVat.Value;
                    if (row.IsMarked.HasValue) existing.IsMarked = row.IsMarked.Value;
                    if (!string.IsNullOrWhiteSpace(row.PackageCode)) existing.PackageCode = row.PackageCode!.Trim();
                    existing.CategoryId = category.Id;

                    // Mavjud tovar qoldig'i importda ALMASHTIRILMAYDI (sotuvlar ustidan yozilib
                    // ketmasin) — kelgan tovar kirim hujjati bilan kiritiladi. Faqat shu filialda
                    // hali umuman qoldiq yozuvi bo'lmagan tovarga boshlang'ich qoldiq qo'yiladi.
                    if (row.Quantity is > 0 && importBranchId.HasValue
                        && existing.Stocks.All(s => s.BranchId != importBranchId.Value))
                    {
                        AddInitialStock(existing, importBranchId.Value, row.Quantity.Value);
                    }
                    else if (row.Quantity.HasValue && importBranchId.HasValue)
                    {
                        result.StockSkipped++;
                    }

                    result.Updated++;
                }
                else
                {
                    // --- YANGI mahsulot ---
                    var newBarcode = barcode;
                    if (newBarcode != null && byBarcode.ContainsKey(newBarcode))
                        newBarcode = null; // barcode boshqa mahsulotга tegishli — e'tiborsiz qoldiramiz

                    var importProduct = new Product
                    {
                        Name      = row.Name!.Trim(),
                        // MXIK bo'sh bo'lsa import to'xtamaydi (katalog keyin to'ldiriladi) — bunday tovar
                        // fiskal chekka tushmaydi: SaleService chekni navbatga qo'yishdan oldin MXIK'ni tekshiradi.
                        MxikCode  = string.IsNullOrWhiteSpace(row.MxikCode) ? string.Empty : Mxik.Require(row.MxikCode, row.Name!.Trim()),
                        Barcode   = newBarcode,
                        PluNumber = row.PluNumber,
                        CostPrice = cost,
                        Price     = price,
                        IsVat     = row.IsVat ?? false,
                        IsMarked  = row.IsMarked ?? false,
                        PackageCode = string.IsNullOrWhiteSpace(row.PackageCode) ? null : row.PackageCode!.Trim(),
                        Unit      = unit,
                        IsWeighed = unit == Unit.Kilogram,
                        CategoryId = category.Id
                    };

                    _db.Products.Add(importProduct);

                    var importQty = row.Quantity ?? 0;
                    if (importQty > 0 && importBranchId.HasValue)
                        AddInitialStock(importProduct, importBranchId.Value, importQty);
                    if (newBarcode != null) byBarcode[newBarcode] = importProduct;
                    if (importProduct.PluNumber is > 0) byPlu.TryAdd(importProduct.PluNumber.Value, importProduct);
                    result.Created++;
                }
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
        // Xizmat (yetkazish) mahsuloti yashirin — avtomatik boshqariladi.
        IQueryable<Product> query = _db.Products.Where(p => p.ProductType != ProductType.Service)
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
                PackageCode   = p.PackageCode
            };
        });

        return _excel.Export(rows);
    }

    public byte[] GetImportTemplate() => _excel.BuildTemplate();

    public byte[] GetTextImportTemplate() => _excel.BuildTextTemplate();

    // Faqat harf va raqamlarni qoldiradi: "Xo’ja Market" → "xojamarket"
    private static string NormalizeBranchKey(string s) =>
        new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    /// <summary>Yangi qoldiq yozuvi + "Initial" harakati (mahsulot yaratish / import).</summary>
    private void AddInitialStock(Product product, Guid branchId, decimal quantity)
    {
        var stock = new BranchStock { BranchId = branchId, ProductId = product.Id };
        product.Stocks.Add(stock);
        StockLedger.Apply(_db, stock, quantity, StockMovementType.Initial,
            new StockDoc(product.Id, null, _currentUser.UserId), product.CostPrice);
    }
}
