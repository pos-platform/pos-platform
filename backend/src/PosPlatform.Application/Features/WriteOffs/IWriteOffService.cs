using PosPlatform.Application.Features.WriteOffs.Dtos;

namespace PosPlatform.Application.Features.WriteOffs;

public interface IWriteOffService
{
    Task<IReadOnlyList<WriteOffDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WriteOffDto> CreateAsync(CreateWriteOffRequest request, CancellationToken cancellationToken = default);
}
