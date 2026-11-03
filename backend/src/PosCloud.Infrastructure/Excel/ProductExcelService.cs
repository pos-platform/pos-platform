using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Products.Dtos;

namespace PosCloud.Infrastructure.Excel;

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

    // ------- IMPORT (matn: .txt / .csv) -------

    /// <summary>
    /// TAB (yoki ; yoki ,) bilan ajratilgan matn faylidan o'qiydi.
    /// Ustun tartibi Excel bilan bir xil:
    /// 1(№) 2(Nomi) 3(Miqdor) 4(Barcode) 5(PLU) 6(шт/кг) 7(Tannarx) 8(Narx)
    /// 9(Naценка) 10(Do'kon) 11(Kategoriya) 12(Chegirma) 13(Chegirmali narx)
    /// 14(НДС) 15(Birlik) 16(MXIK) 17(Маркировка) 18(Kod upakovki).
    /// Birinchi qator sarlavha bo'lsa avtomatik tashlab yuboriladi.
    /// </summary>
    public IReadOnlyList<ProductImportRow> ParseText(Stream stream)
    {
        var rows = new List<ProductImportRow>();
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        // Ajratuvchini aniqlash: TAB ustuvor, keyin ; va ,
        char delimiter = '\t';
        var firstData = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
        if (!firstData.Contains('\t'))
            delimiter = firstData.Contains(';') ? ';' : (firstData.Contains(',') ? ',' : '\t');

        // FENIX eksport formatini avtomatik aniqlash:
        // "kod;nomi;;narx;0;0;0;kod;0;0;;01.01.01;0;0;0;0;01.01.01"
        // Belgisi: ';' bilan, kamida 12 ustun, 1- va 8-ustun bir xil son (kod takrori).
        if (delimiter == ';' && IsFenixRow(firstData.Split(';')))
            return ParseFenix(lines);

        var lineNo = 0;
        var headerSkipped = false;
        foreach (var raw in lines)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var f = raw.Split(delimiter);

            // Birinchi mazmunli qator sarlavha bo'lsa — tashlab yuboramiz
            if (!headerSkipped)
            {
                headerSkipped = true;
                if (LooksLikeHeader(f)) continue;
            }

            var name = Field(f, 1);
            if (string.IsNullOrWhiteSpace(name)) continue;

            rows.Add(new ProductImportRow
            {
                RowNumber     = lineNo,
                Name          = name,
                Quantity      = Dec(Field(f, 2)),
                Barcode       = Field(f, 3),
                PluNumber     = Int(Field(f, 4)),
                UnitType      = Field(f, 5),
                CostPrice     = Dec(Field(f, 6)),
                Price         = Dec(Field(f, 7)),
                MarkupPercent = Dec(Field(f, 8)),
                BranchName    = Field(f, 9),
                CategoryName  = Field(f, 10),
                HasDiscount   = YesNo(Field(f, 11)),
                DiscountPrice = Dec(Field(f, 12)),
                IsVat         = YesNo(Field(f, 13)),
                Unit          = Field(f, 14),
                MxikCode      = Field(f, 15),
                IsMarked      = YesNo(Field(f, 16)),
                PackageCode   = Field(f, 17)
            });
        }
        return rows;
    }

    /// <summary>
    /// FENIX/tarozi eksport formati: kod;nomi;barcode;narx;0;0;0;kod;...;01.01.01
    /// Faqat Kod(1), Nomi(2), Narx(4) ma'noli; birlik nomdagi "kg" bo'yicha aniqlanadi.
    /// </summary>
    private static IReadOnlyList<ProductImportRow> ParseFenix(string[] lines)
    {
        var rows = new List<ProductImportRow>();
        var lineNo = 0;
        foreach (var raw in lines)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var f = raw.Split(';');
            if (!IsFenixRow(f)) continue; // sarlavha/buzuq qatorlarni tashlab yuboramiz

            var name = Field(f, 1);
            if (string.IsNullOrWhiteSpace(name)) continue;

            var isKg = IsWeightedName(name);
            rows.Add(new ProductImportRow
            {
                RowNumber    = lineNo,
                Name         = name,
                Barcode      = Field(f, 2),
                PluNumber    = Int(Field(f, 0)),   // kod = PLU
                Price        = Dec(Field(f, 3)),
                UnitType     = isKg ? "кг" : "шт",
                Unit         = isKg ? "Kilogramm" : "Dona",
                Quantity     = null,
                CostPrice    = null,
                CategoryName = null,
            });
        }
        return rows;
    }

    /// <summary>FENIX qatori belgisi: ≥8 ustun, 1- va 8-ustun bir xil musbat son.</summary>
    private static bool IsFenixRow(string[] f)
    {
        if (f.Length < 8) return false;
        var c0 = f[0].Trim();
        var c7 = f[7].Trim();
        return c0.Length > 0 && c0 == c7
            && int.TryParse(c0, out var n) && n > 0;
    }

    /// <summary>Nomida "kg"/"кг" bo'lsa — kilogram tovar.</summary>
    private static bool IsWeightedName(string name)
    {
        var n = name.ToLowerInvariant();
        return System.Text.RegularExpressions.Regex.IsMatch(n, @"(^|\s)(kg|кг)(\s|$|\.)");
    }

    private static bool LooksLikeHeader(string[] fields)
    {
        var first = fields.Length > 0 ? fields[0].Trim() : "";
        var name = fields.Length > 1 ? fields[1].Trim().ToLowerInvariant() : "";
        if (first == "№" || first.Equals("N", StringComparison.OrdinalIgnoreCase)) return true;
        return name.Contains("назван") || name.Contains("nomi") || name.Contains("product") || name.Contains("название");
    }

    /// <summary>Matn ustunini xavfsiz oladi (0-bazali indeks). Bo'sh bo'lsa null.</summary>
    private static string? Field(string[] f, int index)
    {
        if (index < 0 || index >= f.Length) return null;
        var s = f[index].Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static decimal? Dec(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Replace(" ", "").Replace(" ", "").Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static int? Int(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return int.TryParse(s.Trim(), out var v) && v > 0 ? v : null;
    }

    private static bool? YesNo(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().ToLowerInvariant();
        return s is "есть" or "ha" or "yes" or "true" or "1" or "да" or "+";
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

    // ------- TEMPLATE (matn: .txt) -------

    /// <summary>
    /// TAB bilan ajratilgan matn shabloni (.txt). UTF-8 BOM bilan yoziladi —
    /// Excel/Notepad'da kirill harflari to'g'ri ko'rinishi uchun.
    /// </summary>
    public byte[] BuildTextTemplate()
    {
        var examples = new[]
        {
            new[]{ "1","Coca-Cola 0.5L","100","4780001112223","","шт","6000","8000","33.33","Asosiy do'kon","Ichimliklar","нет","","нет","Dona","01905012001533005","","" },
            new[]{ "2","Guruch Lazer","50","","888","кг","15000","20000","33.33","Asosiy do'kon","Kilogram","нет","","нет","Kilogramm","","","" },
        };

        var sb = new StringBuilder();
        sb.Append(string.Join('\t', Headers)).Append("\r\n");
        foreach (var ex in examples)
            sb.Append(string.Join('\t', ex)).Append("\r\n");

        // UTF-8 BOM — kirill harflari buzilmasligi uchun
        return new UTF8Encoding(true).GetBytes(sb.ToString());
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
