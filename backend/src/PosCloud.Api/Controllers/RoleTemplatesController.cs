using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.RoleTemplates;
using PosCloud.Application.Features.RoleTemplates.Dtos;

namespace PosCloud.Api.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/role-templates")]
public class RoleTemplatesController : ControllerBase
{
    private readonly IRoleTemplateService _service;

    public RoleTemplatesController(IRoleTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleTemplateDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleTemplateDto>> Update(Guid id, UpdateRoleTemplateRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request.Permissions, ct));

    [HttpPost]
    public async Task<ActionResult<RoleTemplateDto>> Create(CreateRoleTemplateRequest request, CancellationToken ct)
        => Ok(await _service.CreateAsync(request.Name, request.Permissions, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
