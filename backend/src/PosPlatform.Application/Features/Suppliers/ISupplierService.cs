using PosPlatform.Application.Features.Suppliers.Dtos;

namespace PosPlatform.Application.Features.Suppliers;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);

    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);

    Task<SupplierDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
