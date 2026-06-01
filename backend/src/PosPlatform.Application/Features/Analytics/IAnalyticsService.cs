using PosPlatform.Application.Features.Analytics.Dtos;

namespace PosPlatform.Application.Features.Analytics;

public interface IAnalyticsService
{
    Task<AnalyticsDto> GetAsync(DateTime? from, DateTime? to, Guid? branchId, CancellationToken cancellationToken = default);
}
