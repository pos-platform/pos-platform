using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Stores.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Stores;

public class StoreService : IStoreService
{
    private readonly IApplicationDbContext _db;

    public StoreService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var stores = await _db.Branches
            .AsNoTracking()
            .Include(b => b.Cashboxes)
            .Include(b => b.Users).ThenInclude(u => u.AppRole)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        return stores.Select(StoreDto.FromEntity).ToList();
    }

    public async Task<StoreDto> CreateStoreAsync(CreateStoreRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        var exists = await _db.Branches.AnyAsync(b => b.Name == name, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"'{name}' do'koni allaqachon mavjud.");
        }

        var store = new Branch
        {
            Name = name,
            Address = request.Address?.Trim(),
            BusinessType = Domain.Enums.BusinessType.Store, // faqat do'kon
            IsActive = true
        };

        _db.Branches.Add(store);
        await _db.SaveChangesAsync(cancellationToken);

        return StoreDto.FromEntity(store);
    }

    public async Task<StoreDto> SetBusinessTypeAsync(Guid id, Domain.Enums.BusinessType type, CancellationToken cancellationToken = default)
    {
        var store = await _db.Branches
            .Include(b => b.Cashboxes)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Do'kon topilmadi: {id}");

        store.BusinessType = Domain.Enums.BusinessType.Store; // faqat do'kon
        await _db.SaveChangesAsync(cancellationToken);

        return StoreDto.FromEntity(store);
    }

    public async Task<StoreDto> SetStoreActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var store = await _db.Branches
            .Include(b => b.Cashboxes)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Do'kon topilmadi: {id}");

        store.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);

        return StoreDto.FromEntity(store);
    }

    public async Task<CashboxDto> AddCashboxAsync(CreateCashboxRequest request, CancellationToken cancellationToken = default)
    {
        var storeExists = await _db.Branches.AnyAsync(b => b.Id == request.BranchId, cancellationToken);
        if (!storeExists)
        {
            throw new NotFoundException($"Do'kon topilmadi: {request.BranchId}");
        }

        var name = request.Name.Trim();
        var dup = await _db.Cashboxes
            .AnyAsync(c => c.BranchId == request.BranchId && c.Name == name, cancellationToken);
        if (dup)
        {
            throw new ConflictException($"Bu do'konda '{name}' kassasi allaqachon mavjud.");
        }

        var cashbox = new Cashbox
        {
            BranchId = request.BranchId,
            Name = name,
            IsActive = true
        };

        _db.Cashboxes.Add(cashbox);
        await _db.SaveChangesAsync(cancellationToken);

        return CashboxDto.FromEntity(cashbox);
    }

    public async Task<CashboxDto> SetCashboxActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var cashbox = await _db.Cashboxes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Kassa topilmadi: {id}");

        cashbox.IsActive = isActive;
        await _db.SaveChangesAsync(cancellationToken);

        return CashboxDto.FromEntity(cashbox);
    }

    public async Task<IReadOnlyList<StoreDto>> GetByOrgAsync(Guid orgId, CancellationToken cancellationToken = default)
    {
        var stores = await _db.Branches
            .AsNoTracking()
            .Where(b => b.OrganizationId == orgId)
            .Include(b => b.Cashboxes)
            .Include(b => b.Users).ThenInclude(u => u.AppRole)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        return stores.Select(StoreDto.FromEntity).ToList();
    }

    public async Task AssignUserToBranchAsync(Guid userId, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException($"Foydalanuvchi topilmadi: {userId}");

        if (branchId is not null)
        {
            var branchExists = await _db.Branches.AnyAsync(b => b.Id == branchId, cancellationToken);
            if (!branchExists)
                throw new NotFoundException($"Do'kon topilmadi: {branchId}");
        }

        user.BranchId = branchId;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
