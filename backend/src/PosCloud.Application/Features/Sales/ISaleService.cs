using PosCloud.Application.Features.Sales.Dtos;

namespace PosCloud.Application.Features.Sales;

public interface ISaleService
{
    /// <summary>Savdolar ro'yxati. from/to (UTC) berilsa shu oraliq bilan filtrlanadi (kunlik ro'yxat uchun).</summary>
    Task<IReadOnlyList<SaleDto>> GetAllAsync(Guid? branchId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    Task<SaleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SaleDto> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Savdoni to'liq qaytaradi (refund) — fiskal return cheki yaratiladi.</summary>
    Task<SaleDto> RefundAsync(Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// To'liq yoki qisman qaytarish. request null yoki items bo'sh — qolgan hamma miqdor.
    /// Keshbek mutanosib ayiriladi, balans yetmasa — puldan ushlab qolinadi (CONTRACT-10, 10.5).
    /// </summary>
    Task<SaleDto> RefundAsync(Guid saleId, RefundRequest? request, RefundOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Qaytarishdan oldin ko'rish: summa, ayiriladigan keshbek, ushlab qolinadigan pul, beriladigan pul.</summary>
    Task<RefundPreviewDto> PreviewRefundAsync(Guid saleId, RefundRequest? request, CancellationToken cancellationToken = default);

    /// <summary>Tovarni filial qoldig'iga va eng yangi FIFO qatlamiga qaytaradi (SaveChanges chaqirmaydi).</summary>
    Task ReturnToStockAsync(Guid branchId, IReadOnlyCollection<(Guid ProductId, decimal Quantity)> lines,
        CancellationToken cancellationToken = default);
}
