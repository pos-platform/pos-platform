using PosCloud.Application.Features.Reports.Dtos;

namespace PosCloud.Application.Common.Interfaces;

public interface IReportExcelService
{
    byte[] ExportSalesReport(SalesReportDto report);
    byte[] ExportStockReport(StockReportDto report);
    byte[] ExportSoldItems(IReadOnlyList<SoldItemDto> items);
}
