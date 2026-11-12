using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PosCloud.Api.Authentication;
using PosCloud.Api.Background;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopAuth;
using PosCloud.Application.Features.ShopPush.Dtos;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Api.Hubs;

/// <summary>
/// Online buyurtmalar hub'i (<c>/hubs/orders</c>). Token query'da: <c>?access_token=...</c>.
/// Ulanishda guruh avtomatik tanlanadi (klient guruh so'ray olmaydi — boshqa filial/mijoz xabarini eshitib bo'lmaydi):
/// xodim filialga biriktirilgan bo'lsa <c>branch:{id}</c>, aks holda <c>org:{id}</c>; mijoz — <c>shop-user:{id}</c>.
/// Xodimga: <c>OrderCreated</c>, <c>OrderUpdated</c>; mijozga: <c>OrderStatusChanged</c>.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + ShopAuthDefaults.Scheme)]
public class OrdersHub : Hub
{
    public const string Path = "/hubs/orders";

    public static string BranchGroup(Guid id) => $"branch:{id}";
    public static string OrgGroup(Guid id) => $"org:{id}";
    public static string ShopUserGroup(Guid id) => $"shop-user:{id}";
    public static string CourierGroup(Guid id) => $"courier:{id}";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.IsInRole(ShopRoles.Customer) == true)
        {
            if (Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var shopUserId))
                await Groups.AddToGroupAsync(Context.ConnectionId, ShopUserGroup(shopUserId));
        }
        else if (user?.IsInRole(nameof(UserRole.Courier)) == true)
        {
            // Kuryer faqat o'z guruhida — filial xodimlari xabarlarini eshitmaydi.
            if (Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var courierId))
                await Groups.AddToGroupAsync(Context.ConnectionId, CourierGroup(courierId));
        }
        else if (user is not null)
        {
            if (Guid.TryParse(user.FindFirstValue("branchId"), out var branchId))
                await Groups.AddToGroupAsync(Context.ConnectionId, BranchGroup(branchId));
            else if (Guid.TryParse(user.FindFirstValue("organizationId"), out var orgId))
                await Groups.AddToGroupAsync(Context.ConnectionId, OrgGroup(orgId));
        }

        await base.OnConnectedAsync();
    }
}

/// <summary>Xabar tarkibi (shaxsiy ma'lumotsiz).</summary>
public record OrderEvent(Guid Id, int Number, Guid BranchId, OnlineOrderStatus Status, OrderPaymentStatus PaymentStatus,
    FulfillmentType FulfillmentType, decimal TotalAmount);

/// <summary>
/// SignalR xabari + (do'kon holatni o'zgartirganda) mijozga push bildirishnoma.
/// Push navbatga qo'yiladi va fon vazifasida yuboriladi — so'rov kutmaydi va xato so'rovga o'tmaydi.
/// </summary>
public class SignalROnlineOrderNotifier : IOnlineOrderNotifier
{
    private readonly IHubContext<OrdersHub> _hub;
    private readonly OrderPushQueue _pushQueue;
    private readonly ILogger<SignalROnlineOrderNotifier> _logger;

    public SignalROnlineOrderNotifier(IHubContext<OrdersHub> hub, OrderPushQueue pushQueue,
        ILogger<SignalROnlineOrderNotifier> logger)
    {
        _hub = hub;
        _pushQueue = pushQueue;
        _logger = logger;
    }

    public async Task OrderChangedAsync(OnlineOrder order, bool isNew, bool pushToCustomer, CancellationToken ct = default)
    {
        // Yangi buyurtma (mijozning o'zi yaratgan) uchun push yo'q.
        if (pushToCustomer && !isNew && order.Status != OnlineOrderStatus.New)
        {
            try
            {
                _pushQueue.Enqueue(new OrderStatusPush(order.Id, order.ShopUserId, order.Number, order.Status,
                    order.FulfillmentType, order.CancelReason, order.CourierName, order.CourierPhone));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push navbatga qo'yilmadi: {OrderId}", order.Id);
            }
        }

        try
        {
            var evt = new OrderEvent(order.Id, order.Number, order.BranchId, order.Status,
                order.PaymentStatus, order.FulfillmentType, order.TotalAmount);

            var staffGroups = new List<string> { OrdersHub.BranchGroup(order.BranchId) };
            if (order.OrganizationId is { } orgId) staffGroups.Add(OrdersHub.OrgGroup(orgId));

            await _hub.Clients.Groups(staffGroups).SendAsync(isNew ? "OrderCreated" : "OrderUpdated", evt, ct);
            await _hub.Clients.Group(OrdersHub.ShopUserGroup(order.ShopUserId)).SendAsync("OrderStatusChanged", evt, ct);
            if (order.CourierUserId is { } courierId)
                await _hub.Clients.Group(OrdersHub.CourierGroup(courierId)).SendAsync("OrderUpdated", evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Online buyurtma xabari yuborilmadi: {OrderId}", order.Id);
        }
    }

    public async Task CourierAssignedAsync(OnlineOrder order, CancellationToken ct = default)
    {
        if (order.CourierUserId is not { } courierId) return;
        try
        {
            var evt = new OrderEvent(order.Id, order.Number, order.BranchId, order.Status,
                order.PaymentStatus, order.FulfillmentType, order.TotalAmount);
            await _hub.Clients.Group(OrdersHub.CourierGroup(courierId)).SendAsync("CourierOrderAssigned", evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kuryerga xabar yuborilmadi: {OrderId}", order.Id);
        }
    }
}
