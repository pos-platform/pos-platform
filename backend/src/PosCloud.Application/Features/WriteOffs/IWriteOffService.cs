using PosCloud.Application.Features.WriteOffs.Dtos;

namespace PosCloud.Application.Features.WriteOffs;

public interface IWriteOffService
{
    Task<IReadOnlyList<WriteOffDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WriteOffDto> CreateAsync(CreateWriteOffRequest request, CancellationToken cancellationToken = default);
}
