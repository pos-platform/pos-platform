using PosPlatform.Application.Features.Products.Dtos;

namespace PosPlatform.Application.Common.Interfaces;

/// <summary>Excel (.xlsx) o'qish/yozish — Infrastructure'da ClosedXML bilan amalga oshiriladi.</summary>
public interface IProductExcelService
{
    /// <summary>Excel oqimidan mahsulot qatorlarini o'qiydi.</summary>
    IReadOnlyList<ProductImportRow> Parse(Stream stream);

    /// <summary>Import uchun bo'sh shablon (.xlsx) yaratadi.</summary>
    byte[] BuildTemplate();

    /// <summary>Mahsulotlarni .xlsx ga eksport qiladi.</summary>
    byte[] Export(IEnumerable<ProductExportRow> rows);
}
