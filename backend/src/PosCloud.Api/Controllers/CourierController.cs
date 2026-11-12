using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Application.Features.Courier;
using PosCloud.Application.Features.Courier.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Kuryer ilovasi (xodim JWT, Role = Courier) — faqat o'ziga biriktirilgan online buyurtmalar.</summary>
[ApiController]
[Authorize(Roles = "Courier")]
[Route("api/courier")]
public class CourierController : ControllerBase
{
    private readonly ICourierService _service;

    public CourierController(ICourierService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CourierMeDto>> Me(CancellationToken ct) =>
        Ok(await _service.GetMeAsync(ct));

    /// <summary>history=false — yo'ldagi (OnTheWay) buyurtmalar; true — bugungi yetkazilgan/muvaffaqiyatsiz.</summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<CourierOrderDto>>> Orders([FromQuery] bool history = false,
        CancellationToken ct = default) =>
        Ok(await _service.GetOrdersAsync(history, ct));

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<CourierOrderDetailDto>> Order(Guid id, CancellationToken ct) =>
        Ok(await _service.GetOrderAsync(id, ct));

    [HttpPost("orders/{id:guid}/delivered")]
    public async Task<ActionResult<CourierOrderDto>> Delivered(Guid id, CourierDeliveredRequest? request, CancellationToken ct) =>
        Ok(await _service.DeliveredAsync(id, request?.CashCollected, ct));

    [HttpPost("orders/{id:guid}/failed")]
    public async Task<ActionResult<CourierOrderDto>> Failed(Guid id, CourierFailedRequest request, CancellationToken ct) =>
        Ok(await _service.FailedAsync(id, request.Reason, ct));
}
