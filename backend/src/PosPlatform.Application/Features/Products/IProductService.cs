using PosPlatform.Application.Features.Products.Dtos;

namespace PosPlatform.Application.Features.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Kassa (POS) uchun yengil ro'yxat — faqat zarur maydonlar, SQL proyeksiyasi bilan.</summary>
    Task<IReadOnlyList<PosProductDto>> GetForPosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kassa katalogining tamg'asi — mahsulot/kategoriya soni va oxirgi o'zgarish vaqti.
    /// Kassa buni davriy so'rab turadi va faqat farq bo'lganda to'liq ro'yxatni tortadi.
    /// </summary>
    Task<PosCatalogStampDto> GetPosStampAsync(CancellationToken cancellationToken = default);

    Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Noyob shtrix-kod (kod tovara) generatsiya qiladi.</summary>
    Task<string> GenerateBarcodeAsync(CancellationToken cancellationToken = default);

    /// <summary>Keyingi bo'sh PLU raqami (mavjud maksimal + 1).</summary>
    Task<int> GetNextPluAsync(CancellationToken cancellationToken = default);

    /// <summary>Excel (.xlsx) dan mahsulotlarni import qiladi.</summary>
    Task<ImportResultDto> ImportAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>Matnli fayl (.txt/.csv) dan mahsulotlarni import qiladi (TAB bilan ajratilgan).</summary>
    Task<ImportResultDto> ImportTextAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>Mahsulotlarni .xlsx ga eksport qiladi.</summary>
    Task<byte[]> ExportAsync(CancellationToken cancellationToken = default);

    /// <summary>Import shabloni (.xlsx).</summary>
    byte[] GetImportTemplate();

    /// <summary>Import shabloni (.txt, TAB bilan).</summary>
    byte[] GetTextImportTemplate();
}
