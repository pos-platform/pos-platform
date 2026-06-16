using PosPlatform.Application.Features.Stores.Dtos;

namespace PosPlatform.Application.Features.Stores;

public interface IStoreService
{
    Task<IReadOnlyList<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<StoreDto> CreateStoreAsync(CreateStoreRequest request, CancellationToken cancellationToken = default);

    Task<StoreDto> SetStoreActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Filial faoliyat turini (Do'kon/Restoran) o'zgartiradi.</summary>
    Task<StoreDto> SetBusinessTypeAsync(Guid id, Domain.Enums.BusinessType type, CancellationToken cancellationToken = default);

    Task<CashboxDto> AddCashboxAsync(CreateCashboxRequest request, CancellationToken cancellationToken = default);

    Task<CashboxDto> SetCashboxActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Foydalanuvchini do'konga biriktirish yoki uzish (branchId = null).</summary>
    Task AssignUserToBranchAsync(Guid userId, Guid? branchId, CancellationToken cancellationToken = default);

    /// <summary>Muayyan tashkilotga tegishli do'konlar (SuperAdmin panel uchun).</summary>
    Task<IReadOnlyList<StoreDto>> GetByOrgAsync(Guid orgId, CancellationToken cancellationToken = default);
}
