using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Products.Dtos;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Products;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;

    public ProductService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return products.Select(ProductDto.FromEntity).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
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
            Barcode = dto.Barcode,
            CostPrice = dto.CostPrice,
            Price = dto.Price,
            ImageUrl = dto.ImageUrl,
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
}
