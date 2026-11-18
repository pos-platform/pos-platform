using PosCloud.Application.Features.Stock.Dtos;

namespace PosCloud.Application.Features.Stock;

public interface IStockReceiptService
{
    Task<IReadOnlyList<StockReceiptDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<StockReceiptDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kirim yaratadi: ombor qoldig'ini oshiradi, tannarxni yangilaydi, yetkazib beruvchi balansiga qarz qo'shadi.</summary>
    Task<StockReceiptDto> CreateAsync(CreateStockReceiptRequest request, CancellationToken cancellationToken = default);

    /// <summary>Kirimni bekor qiladi (storno): qoldiq, partiya va yetkazib beruvchi qarzi teskari o'zgaradi.</summary>
    Task<StockReceiptDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
