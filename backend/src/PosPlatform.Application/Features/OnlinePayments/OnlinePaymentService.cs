using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.OnlinePayments.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.OnlinePayments;

public class OnlinePaymentService : IOnlinePaymentService
{
    private readonly IApplicationDbContext _db;
    private readonly IPaymentGatewaySettings _settings;

    public OnlinePaymentService(IApplicationDbContext db, IPaymentGatewaySettings settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task<OnlinePaymentDto> CreateAsync(CreateOnlinePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var sale = await _db.Sales.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken)
            ?? throw new NotFoundException($"Savdo topilmadi: {request.SaleId}");

        // Eski kutilayotgan to'lovni bekor qilamiz.
        var pending = await _db.OnlinePayments
            .Where(p => p.SaleId == request.SaleId && p.Status == OnlinePaymentStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var old in pending)
            old.Status = OnlinePaymentStatus.Cancelled;

        var payment = new OnlinePayment
        {
            SaleId = request.SaleId,
            Provider = request.Provider,
            Amount = sale.TotalAmount,
            Status = OnlinePaymentStatus.Pending
        };

        payment.PaymentUrl = request.Provider switch
        {
            PaymentProvider.Payme => BuildPaymeUrl(payment, sale),
            PaymentProvider.Click => BuildClickUrl(payment, sale),
            _ => null
        };

        _db.OnlinePayments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        return OnlinePaymentDto.FromEntity(payment);
    }

    public async Task<OnlinePaymentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _db.OnlinePayments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"To'lov topilmadi: {id}");
        return OnlinePaymentDto.FromEntity(p);
    }

    public async Task<IReadOnlyList<OnlinePaymentDto>> GetBySaleAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        var list = await _db.OnlinePayments.AsNoTracking()
            .Where(p => p.SaleId == saleId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return list.Select(OnlinePaymentDto.FromEntity).ToList();
    }

    public async Task<OnlinePaymentDto> MarkPaidAsync(Guid id, string? externalId = null, CancellationToken cancellationToken = default)
    {
        var p = await _db.OnlinePayments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"To'lov topilmadi: {id}");

        p.Status = OnlinePaymentStatus.Paid;
        p.PaidAt = DateTime.UtcNow;
        if (externalId != null) p.ExternalId = externalId;
        await _db.SaveChangesAsync(cancellationToken);

        return OnlinePaymentDto.FromEntity(p);
    }

    public async Task<OnlinePaymentDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _db.OnlinePayments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"To'lov topilmadi: {id}");

        p.Status = OnlinePaymentStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);

        return OnlinePaymentDto.FromEntity(p);
    }

    // --- Payme Subscribe API ---
    public async Task<object> HandlePaymeRpcAsync(PaymeRpcRequest rpc, CancellationToken cancellationToken = default)
    {
        return rpc.Method switch
        {
            "CheckPerformTransaction" => await PaymeCheckPerformAsync(rpc, cancellationToken),
            "CreateTransaction" => await PaymeCreateTransactionAsync(rpc, cancellationToken),
            "PerformTransaction" => await PaymePerformTransactionAsync(rpc, cancellationToken),
            "CancelTransaction" => await PaymeCancelTransactionAsync(rpc, cancellationToken),
            "CheckTransaction" => await PaymeCheckTransactionAsync(rpc, cancellationToken),
            _ => PaymeError(-32601, "Method not found", rpc.Id)
        };
    }

    private async Task<object> PaymeCheckPerformAsync(PaymeRpcRequest rpc, CancellationToken ct)
    {
        var orderId = GetParam<string>(rpc.Params, "account.order_id");
        if (orderId is null)
            return PaymeError(-31050, "Order not found", rpc.Id);

        if (!Guid.TryParse(orderId, out var paymentId))
            return PaymeError(-31050, "Invalid order_id", rpc.Id);

        var payment = await _db.OnlinePayments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

        if (payment is null || payment.Status == OnlinePaymentStatus.Cancelled)
            return PaymeError(-31050, "Order not found", rpc.Id);

        if (payment.Status == OnlinePaymentStatus.Paid)
            return PaymeError(-31060, "Already paid", rpc.Id);

        var amount = GetParam<long?>(rpc.Params, "amount");
        if (amount is null || amount != (long)(payment.Amount * 100))
            return PaymeError(-31001, "Wrong amount", rpc.Id);

        return new { id = rpc.Id, result = new { allow = true } };
    }

    private async Task<object> PaymeCreateTransactionAsync(PaymeRpcRequest rpc, CancellationToken ct)
    {
        var orderId = GetParam<string>(rpc.Params, "account.order_id");
        if (!Guid.TryParse(orderId, out var paymentId))
            return PaymeError(-31050, "Invalid order_id", rpc.Id);

        var payment = await _db.OnlinePayments
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

        if (payment is null || payment.Status == OnlinePaymentStatus.Cancelled)
            return PaymeError(-31050, "Order not found", rpc.Id);

        var transId = GetParam<string>(rpc.Params, "id")!;
        var time = GetParam<long?>(rpc.Params, "time") ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        payment.ExternalId = transId;
        await _db.SaveChangesAsync(ct);

        return new
        {
            id = rpc.Id,
            result = new
            {
                create_time = time,
                transaction = transId,
                state = 1
            }
        };
    }

    private async Task<object> PaymePerformTransactionAsync(PaymeRpcRequest rpc, CancellationToken ct)
    {
        var transId = GetParam<string>(rpc.Params, "id");
        var payment = await _db.OnlinePayments
            .FirstOrDefaultAsync(p => p.ExternalId == transId, ct);

        if (payment is null)
            return PaymeError(-31003, "Transaction not found", rpc.Id);

        payment.Status = OnlinePaymentStatus.Paid;
        payment.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new { id = rpc.Id, result = new { perform_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), transaction = transId, state = 2 } };
    }

    private async Task<object> PaymeCancelTransactionAsync(PaymeRpcRequest rpc, CancellationToken ct)
    {
        var transId = GetParam<string>(rpc.Params, "id");
        var payment = await _db.OnlinePayments
            .FirstOrDefaultAsync(p => p.ExternalId == transId, ct);

        if (payment is null)
            return PaymeError(-31003, "Transaction not found", rpc.Id);

        payment.Status = OnlinePaymentStatus.Cancelled;
        await _db.SaveChangesAsync(ct);

        return new { id = rpc.Id, result = new { cancel_time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), transaction = transId, state = -1 } };
    }

    private async Task<object> PaymeCheckTransactionAsync(PaymeRpcRequest rpc, CancellationToken ct)
    {
        var transId = GetParam<string>(rpc.Params, "id");
        var payment = await _db.OnlinePayments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ExternalId == transId, ct);

        if (payment is null)
            return PaymeError(-31003, "Transaction not found", rpc.Id);

        var state = payment.Status switch
        {
            OnlinePaymentStatus.Paid => 2,
            OnlinePaymentStatus.Cancelled => -1,
            _ => 1
        };

        return new
        {
            id = rpc.Id,
            result = new
            {
                create_time = new DateTimeOffset(payment.CreatedAt).ToUnixTimeMilliseconds(),
                perform_time = payment.PaidAt.HasValue ? new DateTimeOffset(payment.PaidAt.Value).ToUnixTimeMilliseconds() : (long?)null,
                transaction = transId,
                state,
                reason = (int?)null
            }
        };
    }

    private static object PaymeError(int code, string message, object? id) =>
        new { id, error = new { code, message = new { ru = message, uz = message, en = message } } };

    // --- Click ---
    public async Task<ClickPrepareResponse> HandleClickPrepareAsync(ClickPrepareRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(req.MerchantTransId, out var paymentId))
            return new ClickPrepareResponse { ClickTransId = req.ClickTransId, MerchantTransId = req.MerchantTransId, Error = -5, ErrorNote = "User not found" };

        var payment = await _db.OnlinePayments.FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment is null)
            return new ClickPrepareResponse { ClickTransId = req.ClickTransId, MerchantTransId = req.MerchantTransId, Error = -5, ErrorNote = "User not found" };

        if ((long)(payment.Amount * 100) != (long)(req.Amount * 100))
            return new ClickPrepareResponse { ClickTransId = req.ClickTransId, MerchantTransId = req.MerchantTransId, Error = -2, ErrorNote = "Incorrect parameter amount" };

        if (!VerifyClickSign(req))
            return new ClickPrepareResponse { ClickTransId = req.ClickTransId, MerchantTransId = req.MerchantTransId, Error = -1, ErrorNote = "SIGN CHECK FAILED!" };

        return new ClickPrepareResponse
        {
            ClickTransId = req.ClickTransId,
            MerchantTransId = req.MerchantTransId,
            MerchantPrepareId = req.ClickTransId,
            Error = 0,
            ErrorNote = "Success"
        };
    }

    public async Task<ClickCompleteResponse> HandleClickCompleteAsync(ClickCompleteRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(req.MerchantTransId, out var paymentId))
            return new ClickCompleteResponse { ClickTransId = req.ClickTransId, MerchantTransId = req.MerchantTransId, Error = -5, ErrorNote = "User not found" };

        if (!VerifyClickSign(req))
            return new ClickCompleteResponse { ClickTransId = req.ClickTransId, MerchantTransId = req.MerchantTransId, Error = -1, ErrorNote = "SIGN CHECK FAILED!" };

        var payment = await _db.OnlinePayments.FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment is null)
            return new ClickCompleteResponse { ClickTransId = req.ClickTransId, MerchantTransId = req.MerchantTransId, Error = -5, ErrorNote = "User not found" };

        if (req.Error == 0)
        {
            payment.Status = OnlinePaymentStatus.Paid;
            payment.PaidAt = DateTime.UtcNow;
            payment.ExternalId = req.ClickTransId.ToString();
        }
        else
        {
            payment.Status = OnlinePaymentStatus.Cancelled;
        }

        await _db.SaveChangesAsync(ct);

        return new ClickCompleteResponse
        {
            ClickTransId = req.ClickTransId,
            MerchantTransId = req.MerchantTransId,
            MerchantConfirmId = req.ClickTransId,
            Error = 0,
            ErrorNote = "Success"
        };
    }

    // --- Helpers ---

    private string BuildPaymeUrl(OnlinePayment payment, Sale sale)
    {
        var merchantId = _settings.PaymeMerchantId ?? "";
        var amount = (long)(payment.Amount * 100); // tiyin
        var raw = $"m={merchantId};ac.order_id={payment.Id};a={amount}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        return $"https://checkout.paycom.uz/{encoded}";
    }

    private string BuildClickUrl(OnlinePayment payment, Sale sale)
    {
        var serviceId = _settings.ClickServiceId ?? "";
        var merchantId = _settings.ClickMerchantId ?? "";
        var amount = (long)(payment.Amount * 100) / 100m;
        return $"https://my.click.uz/services/pay?service_id={serviceId}&merchant_id={merchantId}&amount={amount}&transaction_param={payment.Id}&return_url=";
    }

    private bool VerifyClickSign(ClickPrepareRequest req)
    {
        var secretKey = _settings.ClickSecretKey ?? "";
        var raw = $"{req.ClickTransId}{req.ServiceId}{secretKey}{req.MerchantTransId}{req.Amount}{req.Action}{req.SignTime}";
        var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(raw))).ToLower();
        return hash == req.SignString;
    }

    private static T? GetParam<T>(Dictionary<string, object>? dict, string key)
    {
        if (dict is null) return default;
        var parts = key.Split('.');
        object? current = dict;

        foreach (var part in parts)
        {
            if (current is not Dictionary<string, object> d || !d.TryGetValue(part, out current))
                return default;
        }

        if (current is T typed) return typed;
        try { return (T)Convert.ChangeType(current?.ToString() ?? "", typeof(T)); }
        catch { return default; }
    }
}
