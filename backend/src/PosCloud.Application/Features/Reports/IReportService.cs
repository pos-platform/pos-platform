using PosCloud.Application.Features.Reports.Dtos;

namespace PosCloud.Application.Features.Reports;

public interface IReportService
{
    Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, Guid? branchId, CancellationToken ct = default);
    Task<byte[]> ExportSalesReportAsync(DateTime from, DateTime to, Guid? branchId, CancellationToken ct = default);
    Task<StockReportDto> GetStockReportAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<byte[]> ExportStockReportAsync(Guid? branchId = null, CancellationToken ct = default);
    Task<SoldItemsPageDto> GetSoldItemsAsync(SoldItemsQuery query, CancellationToken ct = default);
    Task<byte[]> ExportSoldItemsAsync(SoldItemsQuery query, CancellationToken ct = default);
}
