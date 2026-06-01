using System.Globalization;
using ClosedXML.Excel;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Products.Dtos;

namespace PosPlatform.Infrastructure.Excel;

/// <summary>ClosedXML yordamida mahsulot Excel import/eksport.</summary>
public class ProductExcelService : IProductExcelService
{
    private static readonly string[] Headers =
    {
        "Nomi", "MXIK", "Shtrix-kod", "Tannarx", "Sotish narxi", "Qoldiq", "Birlik", "Kategoriya", "QQS"
    };

    public IReadOnlyList<ProductImportRow> Parse(Stream stream)
    {
        var rows = new List<ProductImportRow>();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.FirstOrDefault();
        if (ws is null) return rows;

        foreach (var row in ws.RowsUsed().Skip(1)) // sarlavhani o'tkazib yuboramiz
        {
            var name = row.Cell(1).GetString().Trim();
            var mxik = row.Cell(2).GetString().Trim();

            // To'liq bo'sh qatorni o'tkazib yuboramiz.
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(mxik)) continue;

            rows.Add(new ProductImportRow
            {
                RowNumber = row.RowNumber(),
                Name = name,
                MxikCode = mxik,
                Barcode = row.Cell(3).GetString().Trim(),
                CostPrice = ParseDecimal(row.Cell(4)),
                Price = ParseDecimal(row.Cell(5)),
                Quantity = ParseDecimal(row.Cell(6)),
                Unit = row.Cell(7).GetString().Trim(),
                CategoryName = row.Cell(8).GetString().Trim(),
                IsVat = ParseBool(row.Cell(9))
            });
        }
        return rows;
    }

    public byte[] BuildTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mahsulotlar");
        WriteHeader(ws);
        // Namuna qator.
        ws.Cell(2, 1).Value = "Coca-Cola 0.5L";
        ws.Cell(2, 2).Value = "12345678";
        ws.Cell(2, 3).Value = "4780001112223";
        ws.Cell(2, 4).Value = 6000;
        ws.Cell(2, 5).Value = 8000;
        ws.Cell(2, 6).Value = 100;
        ws.Cell(2, 7).Value = "Dona";
        ws.Cell(2, 8).Value = "Ichimliklar";
        ws.Cell(2, 9).Value = "ha";
        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] Export(IEnumerable<ProductExportRow> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mahsulotlar");
        WriteHeader(ws);
        var r = 2;
        foreach (var p in rows)
        {
            ws.Cell(r, 1).Value = p.Name;
            ws.Cell(r, 2).Value = p.MxikCode;
            ws.Cell(r, 3).Value = p.Barcode;
            ws.Cell(r, 4).Value = p.CostPrice;
            ws.Cell(r, 5).Value = p.Price;
            ws.Cell(r, 6).Value = p.Quantity;
            ws.Cell(r, 7).Value = p.Unit;
            ws.Cell(r, 8).Value = p.Category;
            r++;
        }
        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    private static void WriteHeader(IXLWorksheet ws)
    {
        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static decimal? ParseDecimal(IXLCell cell)
    {
        if (cell.TryGetValue<double>(out var d)) return (decimal)d;
        var s = cell.GetString().Trim().Replace(" ", "").Replace(",", ".");
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
        return null;
    }

    private static bool? ParseBool(IXLCell cell)
    {
        var s = cell.GetString().Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(s)) return null;
        return s is "ha" or "yes" or "true" or "1" or "да" or "+";
    }
}
