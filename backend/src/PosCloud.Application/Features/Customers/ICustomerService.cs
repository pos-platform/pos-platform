using PosCloud.Application.Features.Customers.Dtos;

namespace PosCloud.Application.Features.Customers;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto?> FindByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto> AdjustPointsAsync(Guid id, AdjustPointsRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
