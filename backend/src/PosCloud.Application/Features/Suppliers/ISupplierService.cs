using PosCloud.Application.Features.Suppliers.Dtos;

namespace PosCloud.Application.Features.Suppliers;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);

    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);

    Task<SupplierDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
