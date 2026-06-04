using System.Globalization;
using ClosedXML.Excel;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Products.Dtos;

namespace PosPlatform.Infrastructure.Excel;

/// <summary>
/// ClosedXML orqali mahsulot Excel import/eksport.
/// Format skrenshottagi ustun tartibiga mos:
/// A(№) B(Nomi) C(Miqdor) D(Barcode) E(PLU) F(шт/кг)
/// G(Tannarx) H(Narx) I(Naценka%) J(Do'kon) K(Kategoriya)
/// L(Chegirma) M(Chegirmali narx) N(НДС) O(Birlik) P(MXIK)
/// Q(Маркировка) R(Kod upakovki)
/// </summary>
public class ProductExcelService : IProductExcelService
{
    private static readonly string[] Headers =
    {
        "№",
        "Название продукта",
        "Количество",
        "штрих код",
        "ПЛУ",
        "Тип продукта",
        "Себестоимость",
        "Цена",
        "Наценка",
        "Магазин",
        "Категории",
        "Скидки",
        "Цена со скидки",
        "НДС",
        "Единицы",
        "Икпу",
        "Код маркировки",
        "Код упаковки"
    };

    // ------- IMPORT -------

    public IReadOnlyList<ProductImportRow> Parse(Stream stream)
    {
        var rows = new List<ProductImportRow>();
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.FirstOrDefault();
        if (ws is null) return rows;

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var name = row.Cell(2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            rows.Add(new ProductImportRow
            {
                RowNumber     = row.RowNumber(),
                Name          = name,
                Quantity      = ParseDecimal(row.Cell(3)),
                Barcode       = Str(row.Cell(4)),
                PluNumber     = ParseInt(row.Cell(5)),
                UnitType      = Str(row.Cell(6)),
                CostPrice     = ParseDecimal(row.Cell(7)),
                Price         = ParseDecimal(row.Cell(8)),
                MarkupPercent = ParseDecimal(row.Cell(9)),
                BranchName    = Str(row.Cell(10)),
                CategoryName  = Str(row.Cell(11)),
                HasDiscount   = ParseYesNo(row.Cell(12)),
                DiscountPrice = ParseDecimal(row.Cell(13)),
                IsVat         = ParseYesNo(row.Cell(14)),
                Unit          = Str(row.Cell(15)),
                MxikCode      = Str(row.Cell(16)),
                IsMarked      = ParseYesNo(row.Cell(17)),
                PackageCode   = Str(row.Cell(18))
            });
        }
        return rows;
    }

    // ------- EXPORT -------

    public byte[] Export(IEnumerable<ProductExportRow> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mahsulotlar");
        WriteHeader(ws);

        var r = 2;
        foreach (var p in rows)
        {
            ws.Cell(r,  1).Value = r - 1;
            ws.Cell(r,  2).Value = p.Name;
            ws.Cell(r,  3).Value = (double)p.Quantity;
            ws.Cell(r,  4).Value = p.Barcode ?? "";
            ws.Cell(r,  5).Value = p.PluNumber.HasValue ? (XLCellValue)(double)p.PluNumber.Value : "";
            ws.Cell(r,  6).Value = p.UnitType;
            ws.Cell(r,  7).Value = (double)p.CostPrice;
            ws.Cell(r,  8).Value = (double)p.Price;
            ws.Cell(r,  9).Value = (double)p.MarkupPercent;
            ws.Cell(r, 10).Value = p.BranchName ?? "";
            ws.Cell(r, 11).Value = p.Category;
            ws.Cell(r, 12).Value = p.HasDiscount;
            ws.Cell(r, 13).Value = p.DiscountPrice.HasValue ? (XLCellValue)(double)p.DiscountPrice.Value : "";
            ws.Cell(r, 14).Value = p.IsVat;
            ws.Cell(r, 15).Value = p.Unit;
            ws.Cell(r, 16).Value = p.MxikCode;
            ws.Cell(r, 17).Value = p.IsMarked;
            ws.Cell(r, 18).Value = p.PackageCode ?? "";
            r++;
        }

        if (r > 2)
        {
            ws.Columns(7, 8).Style.NumberFormat.Format = "#,##0";
            ws.Column(9).Style.NumberFormat.Format = "0.##";
            ws.Column(3).Style.NumberFormat.Format = "#,##0.###";
        }

        ws.Columns().AdjustToContents();
        ws.Column(2).Width = Math.Max(ws.Column(2).Width, 28);
        ws.Column(16).Width = Math.Max(ws.Column(16).Width, 22);
        return ToBytes(wb);
    }

    // ------- TEMPLATE -------

    public byte[] BuildTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mahsulotlar");
        WriteHeader(ws);

        var examples = new[]
        {
            new[]{ "1","Coca-Cola 0.5L","100","4780001112223","","шт","6000","8000","33.33","Xo'ja Market","Ichimliklar","нет","","нет","Донна","01905012001533005","нет","" },
            new[]{ "2","Praynik nach","875","4810.2618","","кг","18000","24000","33.33","Xo'ja Market","Kilogram","нет","","нет","Kilogramm","","нет","" },
            new[]{ "3","BEst","500","4780018880165","","шт","8500","10000","17.65","Xo'ja Market","Product","нет","","нет","Донна","1310796 03305001001100017","нет","" },
        };

        for (var i = 0; i < examples.Length; i++)
        {
            var ex = examples[i];
            for (var c = 0; c < ex.Length; c++)
                ws.Cell(i + 2, c + 1).Value = ex[c];
        }

        ws.Column(6).SetDataValidation().List("\"шт,кг\"", true);

        ws.Columns().AdjustToContents();
        ws.Column(2).Width = 28;
        ws.Column(16).Width = 22;
        return ToBytes(wb);
    }

    // ------- Sarlavha -------

    private static void WriteHeader(IXLWorksheet ws)
    {
        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a8a");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        ws.SheetView.FreezeRows(1);
    }

    // ------- Yordamchilar -------

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string? Str(IXLCell cell)
    {
        var s = cell.GetString().Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static decimal? ParseDecimal(IXLCell cell)
    {
        if (cell.TryGetValue<double>(out var d)) return (decimal)d;
        var s = cell.GetString().Trim().Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static int? ParseInt(IXLCell cell)
    {
        if (cell.TryGetValue<double>(out var d) && d > 0) return (int)d;
        var s = cell.GetString().Trim();
        return int.TryParse(s, out var v) && v > 0 ? v : null;
    }

    private static bool? ParseYesNo(IXLCell cell)
    {
        var s = cell.GetString().Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(s)) return null;
        return s is "есть" or "ha" or "yes" or "true" or "1" or "да" or "+";
    }
}
