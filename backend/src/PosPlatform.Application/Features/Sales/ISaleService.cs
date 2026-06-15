using PosPlatform.Application.Features.Sales.Dtos;

namespace PosPlatform.Application.Features.Sales;

public interface ISaleService
{
    Task<IReadOnlyList<SaleDto>> GetAllAsync(Guid? branchId = null, CancellationToken cancellationToken = default);

    Task<SaleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SaleDto> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Savdoni to'liq qaytaradi (refund) — fiskal return cheki yaratiladi.</summary>
    Task<SaleDto> RefundAsync(Guid saleId, CancellationToken cancellationToken = default);

    // --- Restoran: ochiq buyurtma (Pending Sale) hayot davri ---

    /// <summary>Stolda yangi ochiq buyurtma ochadi (Pending Sale), stolni band qiladi.</summary>
    Task<SaleDto> OpenOrderAsync(OpenOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ochiq (Pending) buyurtmalar ro'yxati.</summary>
    Task<IReadOnlyList<SaleDto>> GetOpenOrdersAsync(Guid? branchId = null, CancellationToken cancellationToken = default);

    /// <summary>Stolning ochiq buyurtmasi (bo'lsa).</summary>
    Task<SaleDto?> GetOpenOrderByTableAsync(Guid tableId, CancellationToken cancellationToken = default);

    /// <summary>Ochiq buyurtma qatorlarini to'liq yangilaydi (oshxonaga yuborilgan qatorlar saqlanadi).</summary>
    Task<SaleDto> SaveOrderItemsAsync(Guid saleId, SaveOrderItemsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Yangi (yuborilmagan) qatorlarni oshxona cheki sifatida navbatga qo'yadi. Yuborilgan qatorlar sonini qaytaradi.</summary>
    Task<int> SendToKitchenAsync(Guid saleId, CancellationToken cancellationToken = default);

    /// <summary>Ochiq buyurtmani to'laydi (yopadi) — Pending→Completed, fiskal chek, stol bo'shaydi.</summary>
    Task<SaleDto> PayOrderAsync(Guid saleId, PayOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ochiq buyurtmani bekor qiladi (Cancelled), stol bo'shaydi.</summary>
    Task CancelOrderAsync(Guid saleId, CancellationToken cancellationToken = default);
}
