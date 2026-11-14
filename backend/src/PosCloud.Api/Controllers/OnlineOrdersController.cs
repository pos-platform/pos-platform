using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PosCloud.Api.Authorization;
using PosCloud.Application.Common.Authorization;
using PosCloud.Application.Features.OnlineOrders;
using PosCloud.Application.Features.OnlineOrders.Dtos;
using PosCloud.Domain.Enums;

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

    /// <summary>
    /// active=true (standart) — doska uchun faol buyurtmalar; false — from'dan (standart 7 kun) to'gacha (shu kun ham kiradi) barchasi.
    /// status berilsa — faqat shu holat, sana oralig'i bo'yicha (active e'tiborga olinmaydi).
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<OnlineOrderDto>>> GetList(
        [FromQuery] Guid? branchId, [FromQuery] bool active = true, [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null, [FromQuery] OnlineOrderStatus? status = null,
        CancellationToken ct = default) =>
        Ok(await _service.GetListAsync(branchId, active, from, to, status, ct));

    /// <summary>Mijoz baholari: ?from=&amp;to=&amp;branchId= (YYYY-MM-DD, "to" kuni ham kiradi; standart — oxirgi 30 kun).</summary>
    [HttpGet("ratings")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<OnlineOrderRatingsDto>> GetRatings([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId, CancellationToken ct) =>
        Ok(await _service.GetRatingsAsync(from, to, branchId, ct));

    /// <summary>Eshik oldida rad etilgan, hali do'konga qabul qilinmagan buyurtmalar ("Kuryer qaytargan tovarlar").</summary>
    [HttpGet("rejected-pending")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<OnlineOrderDto>>> GetRejectedPending([FromQuery] Guid? branchId,
        CancellationToken ct) =>
        Ok(await _service.GetRejectedPendingAsync(branchId, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<OnlineOrderDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await _service.GetAsync(id, ct));

    /// <summary>Rad etilgan tovarlar do'konga keldi — ombor qoldig'i qaytadi.</summary>
    [HttpPost("{id:guid}/rejected-received")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> RejectedReceived(Guid id, CancellationToken ct) =>
        Ok(await _service.RejectedReceivedAsync(id, ct));

    /// <summary>Kuryerlarga taklifni (qayta) yuborish — faqat tayyor, kuryersiz yetkazish buyurtmasi.</summary>
    [HttpPost("{id:guid}/offer-couriers")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> OfferCouriers(Guid id, CancellationToken ct) =>
        Ok(await _service.OfferCouriersAsync(id, ct));

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
    public async Task<ActionResult<OnlineOrderDto>> Dispatch(Guid id, DispatchOnlineOrderRequest? request, CancellationToken ct) =>
        Ok(await _service.DispatchAsync(id, request, ct));

    /// <summary>Do'kon tomonidan bekor qilish (Ready, AwaitingTransfer, ReadyForPickup, OnTheWay): savdo qaytariladi. Ochiq smena kerak.</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.SalesRefund)]
    public async Task<ActionResult<OnlineOrderDto>> Cancel(Guid id, CancelOnlineOrderRequest request, CancellationToken ct) =>
        Ok(await _service.CancelAsync(id, request.Reason, ct));

    /// <summary>Faol kuryerlar (xodim filiali yoki branchId bo'yicha) va yo'ldagi buyurtmalari soni.</summary>
    [HttpGet("couriers")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<CourierListItemDto>>> GetCouriers([FromQuery] Guid? branchId, CancellationToken ct) =>
        Ok(await _service.GetCouriersAsync(branchId, ct));

    /// <summary>Kuryerda turgan (topshirilmagan) naqdlar. branchId bo'lmasa — xodim filiali yoki butun tashkilot.</summary>
    [HttpGet("courier-cash")]
    [HasPermission(Permissions.SalesView)]
    public async Task<ActionResult<IReadOnlyList<CourierCashItemDto>>> GetCourierCash([FromQuery] Guid? branchId, CancellationToken ct) =>
        Ok(await _service.GetCourierCashAsync(branchId, ct));

    /// <summary>Kuryer naqdni topshirdi: kassa kirimi (ochiq smena kerak).</summary>
    [HttpPost("{id:guid}/cash-handover")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> CashHandover(Guid id, CancellationToken ct) =>
        Ok(await _service.CashHandoverAsync(id, ct));

    [HttpPost("{id:guid}/delivered")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<ActionResult<OnlineOrderDto>> Delivered(Guid id, CancellationToken ct) =>
        Ok(await _service.DeliveredAsync(id, ct));
}
