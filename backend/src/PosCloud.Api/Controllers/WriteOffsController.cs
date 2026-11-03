using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.WriteOffs;
using PosCloud.Application.Features.WriteOffs.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Hisobdan chiqarish (списание).</summary>
[ApiController]
[Authorize(Roles = "Admin,Manager")]
[Route("api/write-offs")]
public class WriteOffsController : ControllerBase
{
    private readonly IWriteOffService _writeOffs;

    public WriteOffsController(IWriteOffService writeOffs)
    {
        _writeOffs = writeOffs;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WriteOffDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WriteOffDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _writeOffs.GetAllAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(WriteOffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WriteOffDto>> Create(CreateWriteOffRequest request, CancellationToken cancellationToken)
        => Ok(await _writeOffs.CreateAsync(request, cancellationToken));
}
