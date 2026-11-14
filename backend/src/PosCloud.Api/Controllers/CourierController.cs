using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PosCloud.Application.Features.Courier;
using PosCloud.Application.Features.Courier.Dtos;
using PosCloud.Application.Features.OnlineReturns;
using PosCloud.Application.Features.OnlineReturns.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Kuryer ilovasi (xodim JWT, Role = Courier) — faqat o'ziga biriktirilgan online buyurtmalar, takliflar, qaytarishlar.</summary>
[ApiController]
[Authorize(Roles = "Courier")]
[Route("api/courier")]
public class CourierController : ControllerBase
{
    private readonly ICourierService _service;
    private readonly ICourierOfferService _offers;
    private readonly IOnlineReturnService _returns;

    public CourierController(ICourierService service, ICourierOfferService offers, IOnlineReturnService returns)
    {
        _service = service;
        _offers = offers;
        _returns = returns;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CourierMeDto>> Me(CancellationToken ct) =>
        Ok(await _service.GetMeAsync(ct));

    /// <summary>
    /// history=false — yo'ldagi (OnTheWay) va qabul qilingan (Ready) yetkazishlar (kind: "delivery") hamda qaytarishni olib
    /// ketish vazifalari (kind: "return_pickup"); true — bugungi yetkazilgan/muvaffaqiyatsiz.
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<object>>> Orders([FromQuery] bool history = false,
        CancellationToken ct = default) =>
        Ok(await _service.GetOrdersAsync(history, ct));

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<CourierOrderDetailDto>> Order(Guid id, CancellationToken ct) =>
        Ok(await _service.GetOrderAsync(id, ct));

    /// <summary>Yetkazildi: { cashCollected?, rejectedItems?: [{ itemId, quantity, reason }] }.</summary>
    [HttpPost("orders/{id:guid}/delivered")]
    public async Task<ActionResult<CourierOrderDto>> Delivered(Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CourierDeliveredRequest? request, CancellationToken ct) =>
        Ok(await _service.DeliveredAsync(id, request?.CashCollected, request?.RejectedItems, ct));

    /// <summary>Qabul qilingan buyurtmani do'kondan oldi: Ready → OnTheWay.</summary>
    [HttpPost("orders/{id:guid}/picked-up")]
    public async Task<ActionResult<CourierOrderDto>> PickedUp(Guid id, CancellationToken ct) =>
        Ok(await _service.PickedUpAsync(id, ct));

    [HttpPost("orders/{id:guid}/failed")]
    public async Task<ActionResult<CourierOrderDto>> Failed(Guid id, CourierFailedRequest request, CancellationToken ct) =>
        Ok(await _service.FailedAsync(id, request.Reason, ct));

    // ---- Bosqich 10.6: takliflar ----

    [HttpGet("offers")]
    public async Task<ActionResult<IReadOnlyList<CourierOfferDto>>> Offers(CancellationToken ct) =>
        Ok(await _offers.GetMineAsync(ct));

    /// <summary>Atomik: birinchi qabul qilgan oladi; kech qolgan — 409 { error: "Buyurtmani boshqa kuryer oldi" }.</summary>
    [HttpPost("offers/{offerId:guid}/accept")]
    public async Task<ActionResult<CourierOrderDetailDto>> Accept(Guid offerId, CancellationToken ct)
    {
        var order = await _offers.AcceptAsync(offerId, ct);
        return Ok(await _service.GetOrderAsync(order.Id, ct));
    }

    [HttpPost("offers/{offerId:guid}/decline")]
    public async Task<IActionResult> Decline(Guid offerId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CourierDeclineOfferRequest? request, CancellationToken ct)
    {
        await _offers.DeclineAsync(offerId, request?.Reason, ct);
        return NoContent();
    }

    [HttpPut("device-token")]
    public async Task<IActionResult> RegisterDevice(CourierDeviceTokenRequest request, CancellationToken ct)
    {
        await _offers.RegisterDeviceAsync(request, ct);
        return NoContent();
    }

    [HttpDelete("device-token")]
    public async Task<IActionResult> UnregisterDevice([FromBody] CourierDeviceTokenRequest request, CancellationToken ct)
    {
        await _offers.UnregisterDeviceAsync(request.Token, ct);
        return NoContent();
    }

    // ---- Bosqich 10.5: qaytarishni olib ketish ----

    [HttpPost("returns/{id:guid}/picked-up")]
    public async Task<ActionResult<OnlineReturnDto>> ReturnPickedUp(Guid id, CancellationToken ct) =>
        Ok(await _returns.CourierPickedUpAsync(id, ct));
}
