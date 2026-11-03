using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.Roles;
using PosCloud.Application.Features.Roles.Dtos;

namespace PosCloud.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [HasPermission(Permissions.RolesView)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll(CancellationToken ct)
    {
        var roles = await _roleService.GetAllAsync(ct);
        return Ok(roles);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.RolesView)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> GetById(Guid id, CancellationToken ct)
    {
        var role = await _roleService.GetByIdAsync(id, ct);
        return Ok(role);
    }

    [HttpPost]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var role = await _roleService.CreateAsync(request, ct);
        return Ok(role);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> Update(Guid id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await _roleService.UpdateAsync(id, request, ct);
        return Ok(role);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _roleService.DeleteAsync(id, ct);
        return NoContent();
    }
}
