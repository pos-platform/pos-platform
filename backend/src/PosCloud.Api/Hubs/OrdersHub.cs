using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PosCloud.Api.Authentication;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopAuth;
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

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.IsInRole(ShopRoles.Customer) == true)
        {
            if (Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var shopUserId))
                await Groups.AddToGroupAsync(Context.ConnectionId, ShopUserGroup(shopUserId));
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

public class SignalROnlineOrderNotifier : IOnlineOrderNotifier
{
    private readonly IHubContext<OrdersHub> _hub;
    private readonly ILogger<SignalROnlineOrderNotifier> _logger;

    public SignalROnlineOrderNotifier(IHubContext<OrdersHub> hub, ILogger<SignalROnlineOrderNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task OrderChangedAsync(OnlineOrder order, bool isNew, CancellationToken ct = default)
    {
        try
        {
            var evt = new OrderEvent(order.Id, order.Number, order.BranchId, order.Status,
                order.PaymentStatus, order.FulfillmentType, order.TotalAmount);

            var staffGroups = new List<string> { OrdersHub.BranchGroup(order.BranchId) };
            if (order.OrganizationId is { } orgId) staffGroups.Add(OrdersHub.OrgGroup(orgId));

            await _hub.Clients.Groups(staffGroups).SendAsync(isNew ? "OrderCreated" : "OrderUpdated", evt, ct);
            await _hub.Clients.Group(OrdersHub.ShopUserGroup(order.ShopUserId)).SendAsync("OrderStatusChanged", evt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Online buyurtma xabari yuborilmadi: {OrderId}", order.Id);
        }
    }
}
