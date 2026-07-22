using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.Fiscal;
using PosPlatform.Application.Features.Fiscal.Dtos;

namespace PosPlatform.Api.Controllers;

/// <summary>Fiskal modullar (fleshkalar) kartochkalari — modul ID ga nom/egasi biriktiriladi.</summary>
[ApiController]
[Authorize]
[Route("api/fiscal-modules")]
public class FiscalModulesController : ControllerBase
{
    private readonly IFiscalModuleService _modules;

    public FiscalModulesController(IFiscalModuleService modules)
    {
        _modules = modules;
    }

    /// <summary>Barcha kartochkalar — hisobotlarda modul nomini ko'rsatish uchun.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FiscalModuleDto>>> GetAll(CancellationToken ct)
        => Ok(await _modules.GetAllAsync(ct));

    /// <summary>Kartochkani saqlash (ModuleId bo'yicha upsert) — Admin.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<FiscalModuleDto>> Save([FromBody] SaveFiscalModuleRequest request, CancellationToken ct)
        => Ok(await _modules.SaveAsync(request, ct));

    /// <summary>Kartochkani o'chirish — Admin.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _modules.DeleteAsync(id, ct);
        return NoContent();
    }
}
