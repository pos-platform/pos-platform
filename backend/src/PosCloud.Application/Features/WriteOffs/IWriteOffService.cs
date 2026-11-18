using PosCloud.Application.Features.WriteOffs.Dtos;

namespace PosCloud.Application.Features.WriteOffs;

public interface IWriteOffService
{
    Task<IReadOnlyList<WriteOffDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WriteOffDto> CreateAsync(CreateWriteOffRequest request, CancellationToken cancellationToken = default);

    /// <summary>Hisobdan chiqarishni bekor qiladi (storno): tovar omborga qaytadi.</summary>
    Task<WriteOffDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
