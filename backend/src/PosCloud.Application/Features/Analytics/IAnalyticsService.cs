using PosCloud.Application.Features.Analytics.Dtos;

namespace PosCloud.Application.Features.Analytics;

public interface IAnalyticsService
{
    Task<AnalyticsDto> GetAsync(DateTime? from, DateTime? to, Guid? branchId, CancellationToken cancellationToken = default);
}
