using PosPlatform.Application.Features.Reports.Dtos;

namespace PosPlatform.Application.Common.Interfaces;

public interface IReportExcelService
{
    byte[] ExportSalesReport(SalesReportDto report);
    byte[] ExportStockReport(StockReportDto report);
}
