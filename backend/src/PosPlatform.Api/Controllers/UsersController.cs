using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Api.Authorization;
using PosPlatform.Application.Common.Authorization;
using PosPlatform.Application.Features.Users;
using PosPlatform.Application.Features.Users.Dtos;

namespace PosPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [HasPermission(Permissions.UsersView)]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost]
    [HasPermission(Permissions.UsersCreate)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);
        return Ok(user);
    }

    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(Permissions.UsersEdit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _userService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [HasPermission(Permissions.UsersActivate)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.SetActiveAsync(id, true, cancellationToken);
        return Ok(user);
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.UsersActivate)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.SetActiveAsync(id, false, cancellationToken);
        return Ok(user);
    }

    /// <summary>Foydalanuvchiga rol belgilaydi.</summary>
    [HttpPut("{id:guid}/role")]
    [HasPermission(Permissions.UsersEdit)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> AssignRole(Guid id, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.AssignRoleAsync(id, request, cancellationToken);
        return Ok(user);
    }
}
