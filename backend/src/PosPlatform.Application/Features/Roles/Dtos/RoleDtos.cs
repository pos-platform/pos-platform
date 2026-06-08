using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Roles.Dtos;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public List<string> Permissions { get; set; } = [];

    public static RoleDto FromEntity(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        IsSystem = role.IsSystem,
        Permissions = role.RolePermissions
            .Select(rp => rp.Permission.Code)
            .OrderBy(c => c)
            .ToList()
    };
}

public class CreateRoleRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = [];
}

public class UpdateRoleRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public List<string> Permissions { get; set; } = [];
}
