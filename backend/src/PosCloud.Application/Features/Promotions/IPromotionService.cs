using PosCloud.Application.Features.Promotions.Dtos;

namespace PosCloud.Application.Features.Promotions;

public interface IPromotionService
{
    Task<IReadOnlyList<PromotionDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Hozir amal qilayotgan aksiyalar (POS uchun).</summary>
    Task<IReadOnlyList<PromotionDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default);

    Task<PromotionDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
