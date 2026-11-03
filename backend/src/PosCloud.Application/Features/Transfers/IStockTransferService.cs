using PosCloud.Application.Features.Transfers.Dtos;

namespace PosCloud.Application.Features.Transfers;

public interface IStockTransferService
{
    Task<IReadOnlyList<StockTransferDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<StockTransferDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockTransferDto> CreateAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default);
    Task<StockTransferDto> ReceiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StockTransferDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
