namespace PosPlatform.Application.Features.Inventories;

public interface IInventoryService
{
    Task<IReadOnlyList<InventoryDto>> GetAllAsync(CancellationToken ct = default);
    Task<InventoryDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InventoryDto> CreateAsync(CreateInventoryRequest req, CancellationToken ct = default);
    Task<InventoryDto> UpdateActualQtyAsync(Guid id, UpdateInventoryRequest req, CancellationToken ct = default);
    Task<InventoryDto> ApplyAsync(Guid id, CancellationToken ct = default);
}
