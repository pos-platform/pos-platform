using PosCloud.Application.Features.Payments.Dtos;

namespace PosCloud.Application.Features.Payments;

public interface ISupplierPaymentService
{
    Task<IReadOnlyList<SupplierPaymentDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Yetkazib beruvchiga to'lov — uning balansini (qarzni) kamaytiradi.</summary>
    Task<SupplierPaymentDto> CreateAsync(CreateSupplierPaymentRequest request, CancellationToken cancellationToken = default);
}
