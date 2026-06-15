using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Organizations;
using PosPlatform.Application.Features.Organizations.Dtos;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Api.Controllers;

/// <summary>Joriy tashkilot sozlamalari — org admin uchun (faoliyat turi: Do'kon/Restoran).</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/my-organization")]
public class OrganizationSettingsController : ControllerBase
{
    private readonly IOrganizationService _service;
    private readonly ICurrentUser _currentUser;

    public OrganizationSettingsController(IOrganizationService service, ICurrentUser currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>Joriy tashkilot ma'lumoti (faoliyat turi bilan).</summary>
    [HttpGet]
    public async Task<ActionResult<OrganizationDto>> Get(CancellationToken ct)
    {
        if (_currentUser.OrganizationId is not { } orgId)
            return NotFound();
        return Ok(await _service.GetSummaryAsync(orgId, ct));
    }

    /// <summary>Faoliyat turini o'zgartirish (Do'kon/Restoran).</summary>
    [HttpPut("business-type")]
    public async Task<ActionResult<OrganizationDto>> SetBusinessType([FromBody] SetBusinessTypeRequest request, CancellationToken ct)
    {
        if (_currentUser.OrganizationId is not { } orgId)
            return NotFound();
        return Ok(await _service.SetBusinessTypeAsync(orgId, request.BusinessType, ct));
    }
}

public class SetBusinessTypeRequest
{
    public BusinessType BusinessType { get; set; }
}
