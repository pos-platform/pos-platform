using PosPlatform.Application.Features.Sales.Dtos;

namespace PosPlatform.Application.Features.Sales;

public interface ISaleService
{
    Task<IReadOnlyList<SaleDto>> GetAllAsync(Guid? branchId = null, CancellationToken cancellationToken = default);

    Task<SaleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SaleDto> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Savdoni to'liq qaytaradi (refund) — fiskal return cheki yaratiladi.</summary>
    Task<SaleDto> RefundAsync(Guid saleId, CancellationToken cancellationToken = default);
}
