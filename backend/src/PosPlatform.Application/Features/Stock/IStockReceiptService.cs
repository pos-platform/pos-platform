using PosPlatform.Application.Features.Stock.Dtos;

namespace PosPlatform.Application.Features.Stock;

public interface IStockReceiptService
{
    Task<IReadOnlyList<StockReceiptDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<StockReceiptDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kirim yaratadi: ombor qoldig'ini oshiradi, tannarxni yangilaydi, yetkazib beruvchi balansiga qarz qo'shadi.</summary>
    Task<StockReceiptDto> CreateAsync(CreateStockReceiptRequest request, CancellationToken cancellationToken = default);
}
