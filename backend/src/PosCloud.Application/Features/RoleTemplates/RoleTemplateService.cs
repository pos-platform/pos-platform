using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.RoleTemplates.Dtos;
using PosCloud.Application.Features.Roles;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.RoleTemplates;

public class RoleTemplateService : IRoleTemplateService
{
    private readonly IApplicationDbContext _db;
    private readonly IRoleService _roleService;

    public RoleTemplateService(IApplicationDbContext db, IRoleService roleService)
    {
        _db = db;
        _roleService = roleService;
    }

    public async Task<IReadOnlyList<RoleTemplateDto>> GetAllAsync(CancellationToken ct = default)
    {
        var templates = await _db.RoleTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        return templates.Select(RoleTemplateDto.FromEntity).ToList();
    }

    public async Task<RoleTemplateDto> UpdateAsync(Guid id, IReadOnlyList<string> permissions, CancellationToken ct = default)
    {
        var template = await _db.RoleTemplates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException($"Rol shabloni topilmadi: {id}");

        var permList = permissions.Distinct().ToList();
        template.PermissionsJson = JsonSerializer.Serialize(permList);
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Shablon nomi bilan mos barcha mavjud tizim rollarini yangilangan ruxsatlarga moslaymiz.
        await _roleService.SyncSystemRolesToTemplateAsync(template.Name, permList, ct);

        return RoleTemplateDto.FromEntity(template);
    }

    public async Task<RoleTemplateDto> CreateAsync(string name, IReadOnlyList<string> permissions, CancellationToken ct = default)
    {
        name = name.Trim();
        if (await _db.RoleTemplates.AnyAsync(t => t.Name == name, ct))
            throw new ConflictException($"'{name}' nomli shablon allaqachon mavjud.");

        var template = new RoleTemplate
        {
            Name = name,
            IsSystem = false,
            PermissionsJson = JsonSerializer.Serialize(permissions.Distinct().ToList()),
            UpdatedAt = DateTime.UtcNow
        };
        _db.RoleTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return RoleTemplateDto.FromEntity(template);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await _db.RoleTemplates.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException($"Rol shabloni topilmadi: {id}");

        if (template.IsSystem)
            throw new ValidationException("Tizim shablonlarini o'chirib bo'lmaydi.");

        _db.RoleTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }
}
