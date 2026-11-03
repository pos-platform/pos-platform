using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.RoleTemplates.Dtos;

public class RoleTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public List<string> Permissions { get; set; } = new();

    public static RoleTemplateDto FromEntity(RoleTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        IsSystem = t.IsSystem,
        Permissions = JsonSerializer.Deserialize<List<string>>(t.PermissionsJson) ?? new()
    };
}

public class UpdateRoleTemplateRequest
{
    [Required]
    public List<string> Permissions { get; set; } = new();
}

public class CreateRoleTemplateRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = new();
}
