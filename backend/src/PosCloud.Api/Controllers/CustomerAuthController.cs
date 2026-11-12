using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PosCloud.Application.Features.ShopAuth;
using PosCloud.Application.Features.ShopAuth.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Mobil ilova mijozining kirishi: telefon + bir martalik kod.</summary>
[ApiController]
[Route("api/customer-auth")]
public class CustomerAuthController : ControllerBase
{
    private readonly IShopAuthService _service;

    public CustomerAuthController(IShopAuthService service)
    {
        _service = service;
    }

    /// <summary>Telefon raqamga kirish kodini yuboradi (Telegram, bo'lmasa SMS).</summary>
    [HttpPost("send-otp")]
    [EnableRateLimiting("otp")]
    [ProducesResponseType(typeof(SendOtpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SendOtpResponse>> SendOtp(SendOtpRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        return Ok(await _service.SendOtpAsync(request, ip, ct));
    }

    /// <summary>Kodni tekshiradi. Akkaunt bo'lmasa yaratadi va tokenlarni qaytaradi.</summary>
    [HttpPost("verify")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ShopAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShopAuthResponse>> Verify(VerifyOtpRequest request, CancellationToken ct)
    {
        return Ok(await _service.VerifyOtpAsync(request, ct));
    }

    /// <summary>
    /// Refresh token bilan yangi juftlik oladi. Eski refresh token bekor bo'ladi —
    /// ilova parallel refresh qilmasligi kerak.
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ShopAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShopAuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        return Ok(await _service.RefreshAsync(request, ct));
    }

    /// <summary>Joriy qurilmadagi sessiyani yopadi.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await _service.LogoutAsync(request, ct);
        return NoContent();
    }
}
