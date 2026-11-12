using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Application.Features.OnlineOrders.Dtos;

namespace PosCloud.Api.Controllers;

/// <summary>Online (mobil ilova) buyurtmalari — kassa/desktop va admin panel uchun.</summary>
[ApiController]
[Authorize]
[Route("api/online-orders")]
public class OnlineOrdersController : ControllerBase
{
    private readonly IOnlineOrderService _service;

    public OnlineOrdersController(IOnlineOrderService service)
    {
        _service = service;
    }

    /// <summary>active=true (standart) — doska uchun faol buyurtmalar; false — from'dan (standart 7 kun) barchasi.</summary>
    [HttpGet]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<OnlineOrderDto>>> GetList(
        [FromQuery] Guid? branchId, [FromQuery] bool active = true, [FromQuery] DateTime? from = null,
        CancellationToken ct = default) =>
        Ok(await _service.GetListAsync(branchId, active, from, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<OnlineOrderDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _service.GetAsync(id, ct));

    [HttpPost("{id:guid}/accept")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> Accept(Guid id, CancellationToken ct) =>
        Ok(await _service.AcceptAsync(id, ct));

    [HttpPost("{id:guid}/reject")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> Reject(Guid id, RejectOnlineOrderRequest request, CancellationToken ct) =>
        Ok(await _service.RejectAsync(id, request.Reason, ct));

    [HttpPost("{id:guid}/start-picking")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> StartPicking(Guid id, CancellationToken ct) =>
        Ok(await _service.StartPickingAsync(id, ct));

    /// <summary>Yig'ish: olingan miqdor, olib tashlash, almashtirish/qo'shish.</summary>
    [HttpPut("{id:guid}/items")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> UpdateItems(Guid id, UpdatePickingRequest request, CancellationToken ct) =>
        Ok(await _service.UpdatePickingAsync(id, request, ct));

    /// <summary>Tayyor: oddiy savdo (fiskal chek, ombordan ayirish) yaratiladi. Ochiq smena kerak.</summary>
    [HttpPost("{id:guid}/ready")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> Ready(Guid id, ReadyOnlineOrderRequest? request, CancellationToken ct) =>
        Ok(await _service.ReadyAsync(id, request ?? new ReadyOnlineOrderRequest(), ct));

    [HttpPost("{id:guid}/payment-received")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> PaymentReceived(Guid id, CancellationToken ct) =>
        Ok(await _service.PaymentReceivedAsync(id, ct));

    [HttpPost("{id:guid}/dispatch")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> Dispatch(Guid id, CancellationToken ct) =>
        Ok(await _service.DispatchAsync(id, ct));

    [HttpPost("{id:guid}/delivered")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> Delivered(Guid id, CancellationToken ct) =>
        Ok(await _service.DeliveredAsync(id, ct));
}
