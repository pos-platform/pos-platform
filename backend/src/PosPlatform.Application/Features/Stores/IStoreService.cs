using PosPlatform.Application.Features.Stores.Dtos;

namespace PosPlatform.Application.Features.Stores;

public interface IStoreService
{
    Task<IReadOnlyList<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<StoreDto> CreateStoreAsync(CreateStoreRequest request, CancellationToken cancellationToken = default);

    Task<StoreDto> SetStoreActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<CashboxDto> AddCashboxAsync(CreateCashboxRequest request, CancellationToken cancellationToken = default);

    Task<CashboxDto> SetCashboxActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
