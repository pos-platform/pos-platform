using PosCloud.Application.Features.KitchenIssues.Dtos;

namespace PosCloud.Application.Features.KitchenIssues;

public interface IKitchenIssueService
{
    Task<IReadOnlyList<KitchenIssueDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<KitchenIssueDto> CreateAsync(CreateKitchenIssueRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ovqat retsepti asosida N porsiya uchun taklif qilingan tovarlar.</summary>
    Task<IReadOnlyList<SuggestedIssueItemDto>> GetSuggestedItemsAsync(Guid dishId, decimal portions, CancellationToken cancellationToken = default);

    /// <summary>Oshxona sarfi hisoboti — sana oralig'ida ovqat bo'yicha sarflangan tovarlar.</summary>
    Task<IReadOnlyList<KitchenConsumptionRow>> GetConsumptionReportAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
