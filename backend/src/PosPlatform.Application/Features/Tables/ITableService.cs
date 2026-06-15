using PosPlatform.Application.Features.Tables.Dtos;

namespace PosPlatform.Application.Features.Tables;

public interface ITableService
{
    Task<IReadOnlyList<TableDto>> GetAllAsync(Guid? branchId = null, CancellationToken cancellationToken = default);
    Task<TableDto> CreateAsync(CreateTableDto dto, CancellationToken cancellationToken = default);
    Task<TableDto> UpdateAsync(Guid id, UpdateTableDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
