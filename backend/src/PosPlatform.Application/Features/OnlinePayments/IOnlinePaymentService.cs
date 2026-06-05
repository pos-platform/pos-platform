using PosPlatform.Application.Features.OnlinePayments.Dtos;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.OnlinePayments;

public interface IOnlinePaymentService
{
    Task<OnlinePaymentDto> CreateAsync(CreateOnlinePaymentRequest request, CancellationToken cancellationToken = default);
    Task<OnlinePaymentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OnlinePaymentDto>> GetBySaleAsync(Guid saleId, CancellationToken cancellationToken = default);
    Task<OnlinePaymentDto> MarkPaidAsync(Guid id, string? externalId = null, CancellationToken cancellationToken = default);
    Task<OnlinePaymentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    // Payme webhook RPC handler
    Task<object> HandlePaymeRpcAsync(PaymeRpcRequest rpc, CancellationToken cancellationToken = default);
    // Click webhook handlers
    Task<ClickPrepareResponse> HandleClickPrepareAsync(ClickPrepareRequest request, CancellationToken cancellationToken = default);
    Task<ClickCompleteResponse> HandleClickCompleteAsync(ClickCompleteRequest request, CancellationToken cancellationToken = default);
}
