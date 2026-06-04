using PosPlatform.Application.Features.Reports.Dtos;

namespace PosPlatform.Application.Features.Reports;

public interface IReportService
{
    Task<SalesReportDto> GetSalesReportAsync(DateTime from, DateTime to, Guid? branchId, CancellationToken ct = default);
    Task<byte[]> ExportSalesReportAsync(DateTime from, DateTime to, Guid? branchId, CancellationToken ct = default);
    Task<StockReportDto> GetStockReportAsync(CancellationToken ct = default);
    Task<byte[]> ExportStockReportAsync(CancellationToken ct = default);
}
