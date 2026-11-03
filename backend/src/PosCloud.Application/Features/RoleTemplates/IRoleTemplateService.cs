using PosCloud.Application.Features.RoleTemplates.Dtos;

namespace PosCloud.Application.Features.RoleTemplates;

public interface IRoleTemplateService
{
    Task<IReadOnlyList<RoleTemplateDto>> GetAllAsync(CancellationToken ct = default);
    Task<RoleTemplateDto> UpdateAsync(Guid id, IReadOnlyList<string> permissions, CancellationToken ct = default);
    Task<RoleTemplateDto> CreateAsync(string name, IReadOnlyList<string> permissions, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
