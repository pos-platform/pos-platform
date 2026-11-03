using PosCloud.Application.Features.Products.Dtos;

namespace PosCloud.Application.Common.Interfaces;

/// <summary>Excel (.xlsx) o'qish/yozish — Infrastructure'da ClosedXML bilan amalga oshiriladi.</summary>
public interface IProductExcelService
{
    /// <summary>Excel oqimidan mahsulot qatorlarini o'qiydi.</summary>
    IReadOnlyList<ProductImportRow> Parse(Stream stream);

    /// <summary>Matnli (.txt/.csv) oqimdan mahsulot qatorlarini o'qiydi (TAB/;/, bilan ajratilgan, Excel ustun tartibi).</summary>
    IReadOnlyList<ProductImportRow> ParseText(Stream stream);

    /// <summary>Import uchun bo'sh shablon (.xlsx) yaratadi.</summary>
    byte[] BuildTemplate();

    /// <summary>Import uchun matnli shablon (.txt, TAB bilan, UTF-8 BOM).</summary>
    byte[] BuildTextTemplate();

    /// <summary>Mahsulotlarni .xlsx ga eksport qiladi.</summary>
    byte[] Export(IEnumerable<ProductExportRow> rows);
}
