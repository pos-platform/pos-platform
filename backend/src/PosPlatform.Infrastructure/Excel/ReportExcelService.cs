using ClosedXML.Excel;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Reports.Dtos;

namespace PosPlatform.Infrastructure.Excel;

public class ReportExcelService : IReportExcelService
{
    public byte[] ExportSalesReport(SalesReportDto report)
    {
        using var wb = new XLWorkbook();

        var ws1 = wb.Worksheets.Add("Kunlik hisobot");
        AddHeader(ws1, new[] { "Sana", "Savdolar soni", "Tushum", "Tannarx", "Foyda" });
        var r = 2;
        foreach (var d in report.Days)
        {
            ws1.Cell(r, 1).Value = d.Date.ToString("yyyy-MM-dd");
            ws1.Cell(r, 2).Value = d.Count;
            ws1.Cell(r, 3).Value = (double)d.Revenue;
            ws1.Cell(r, 4).Value = (double)d.Cost;
            ws1.Cell(r, 5).Value = (double)d.Profit;
            r++;
        }
        ws1.Cell(r, 1).Value = "JAMI";
        ws1.Cell(r, 2).Value = report.TotalCount;
        ws1.Cell(r, 3).Value = (double)report.TotalRevenue;
        ws1.Cell(r, 4).Value = (double)report.TotalCost;
        ws1.Cell(r, 5).Value = (double)report.TotalProfit;
        ws1.Row(r).Style.Font.Bold = true;
        ws1.Columns(3, 5).Style.NumberFormat.Format = "#,##0";
        ws1.Columns().AdjustToContents();

        var ws2 = wb.Worksheets.Add("Top mahsulotlar");
        AddHeader(ws2, new[] { "Mahsulot", "Miqdor", "Tushum", "Tannarx", "Foyda" });
        r = 2;
        foreach (var p in report.TopProducts)
        {
            ws2.Cell(r, 1).Value = p.ProductName;
            ws2.Cell(r, 2).Value = (double)p.Quantity;
            ws2.Cell(r, 3).Value = (double)p.Revenue;
            ws2.Cell(r, 4).Value = (double)p.Cost;
            ws2.Cell(r, 5).Value = (double)p.Profit;
            r++;
        }
        ws2.Columns(3, 5).Style.NumberFormat.Format = "#,##0";
        ws2.Columns().AdjustToContents();

        return ToBytes(wb);
    }

    public byte[] ExportStockReport(StockReportDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Qoldiqlar");
        AddHeader(ws, new[] { "Mahsulot", "Kategoriya", "Barcode", "Birlik", "Qoldiq", "Tannarx", "Narx", "Tannarx qiymati", "Savdo qiymati" });
        var r = 2;
        foreach (var i in report.Items)
        {
            ws.Cell(r, 1).Value = i.Name;
            ws.Cell(r, 2).Value = i.Category;
            ws.Cell(r, 3).Value = i.Barcode ?? "";
            ws.Cell(r, 4).Value = i.Unit;
            ws.Cell(r, 5).Value = (double)i.Quantity;
            ws.Cell(r, 6).Value = (double)i.CostPrice;
            ws.Cell(r, 7).Value = (double)i.SalePrice;
            ws.Cell(r, 8).Value = (double)i.CostValue;
            ws.Cell(r, 9).Value = (double)i.SaleValue;
            r++;
        }
        ws.Cell(r, 1).Value = "JAMI";
        ws.Cell(r, 8).Value = (double)report.TotalCostValue;
        ws.Cell(r, 9).Value = (double)report.TotalSaleValue;
        ws.Row(r).Style.Font.Bold = true;
        ws.Columns(5, 9).Style.NumberFormat.Format = "#,##0";
        ws.Columns().AdjustToContents();
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 28);

        return ToBytes(wb);
    }

    private static void AddHeader(IXLWorksheet ws, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a8a");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.SheetView.FreezeRows(1);
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
