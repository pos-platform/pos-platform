using PosCloud.Application.Features.Dashboard.Dtos;

namespace PosCloud.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(
        DateTime? from,
        DateTime? to,
        Guid? branchId,
        CancellationToken cancellationToken = default);
}
