using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authentication;
using PosCloud.Application.Features.ShopProfile;
using PosCloud.Application.Features.ShopProfile.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Mobil ilova mijozining profili va manzillari.</summary>
[ApiController]
[Authorize(Policy = ShopAuthDefaults.Policy)]
[Route("api/me")]
public class ShopMeController : ControllerBase
{
    private readonly IShopProfileService _service;

    public ShopMeController(IShopProfileService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ShopProfileDto>> Get(CancellationToken ct) =>
        Ok(await _service.GetAsync(User.GetShopUserId(), ct));

    [HttpPut]
    public async Task<ActionResult<ShopProfileDto>> Update(UpdateShopProfileRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(User.GetShopUserId(), request, ct));

    /// <summary>Akkauntni o'chirish. Shaxsiy ma'lumotlar tozalanadi, barcha sessiyalar yopiladi.</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        await _service.DeleteAccountAsync(User.GetShopUserId(), ct);
        return NoContent();
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<ShopAddressDto>>> GetAddresses(CancellationToken ct) =>
        Ok(await _service.GetAddressesAsync(User.GetShopUserId(), ct));

    [HttpPost("addresses")]
    public async Task<ActionResult<ShopAddressDto>> CreateAddress(SaveShopAddressRequest request, CancellationToken ct) =>
        Ok(await _service.CreateAddressAsync(User.GetShopUserId(), request, ct));

    [HttpPut("addresses/{id:guid}")]
    public async Task<ActionResult<ShopAddressDto>> UpdateAddress(Guid id, SaveShopAddressRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAddressAsync(User.GetShopUserId(), id, request, ct));

    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        await _service.DeleteAddressAsync(User.GetShopUserId(), id, ct);
        return NoContent();
    }
}
