using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Suppliers.Dtos;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Suppliers;

public class SupplierService : ISupplierService
{
    private readonly IApplicationDbContext _db;

    public SupplierService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var suppliers = await _db.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return suppliers.Select(SupplierDto.FromEntity).ToList();
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var exists = await _db.Suppliers.AnyAsync(s => s.Name == name, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"'{name}' yetkazib beruvchisi allaqachon mavjud.");
        }

        var supplier = new Supplier
        {
            Name = name,
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            IsActive = true
        };

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(cancellationToken);

        return SupplierDto.FromEntity(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await GetAsync(id, cancellationToken);
        supplier.Name = request.Name.Trim();
        supplier.Phone = request.Phone?.Trim();
        supplier.Address = request.Address?.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        return SupplierDto.FromEntity(supplier);
    }

    public async Task<SupplierDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var supplier = await GetAsync(id, cancellationToken);
        supplier.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);

        return SupplierDto.FromEntity(supplier);
    }

    private async Task<Supplier> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Yetkazib beruvchi topilmadi: {id}");
}
