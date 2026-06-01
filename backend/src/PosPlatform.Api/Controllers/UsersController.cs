using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosPlatform.Application.Features.Users;
using PosPlatform.Application.Features.Users.Dtos;

namespace PosPlatform.Api.Controllers;

/// <summary>Foydalanuvchilar boshqaruvi — faqat Admin uchun.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>Yangi foydalanuvchi (kassir/menejer/admin) yaratadi.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);
        return Ok(user);
    }

    /// <summary>Foydalanuvchi parolini tiklaydi.</summary>
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _userService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Hisobni faollashtirish.</summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.SetActiveAsync(id, true, cancellationToken);
        return Ok(user);
    }

    /// <summary>Hisobni faolsizlantirish (bloklash).</summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.SetActiveAsync(id, false, cancellationToken);
        return Ok(user);
    }
}
