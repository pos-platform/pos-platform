using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.ShopAuth.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.ShopAuth;

/// <summary>Mobil ilova mijozi JWT'sidagi rol. Xodim rollari bilan kesishmaydi.</summary>
public static class ShopRoles
{
    public const string Customer = "ShopCustomer";
}

public interface IShopAuthService
{
    Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request, string? ipAddress, CancellationToken ct = default);
    Task<ShopAuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);
    Task<ShopAuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);
}

/// <summary>
/// Telefon + bir martalik kod orqali kirish. ShopUser global (OrganizationId = null),
/// shuning uchun barcha so'rovlar tenant filtridan mustaqil (IgnoreQueryFilters).
/// </summary>
public class ShopAuthService : IShopAuthService
{
    public const int CodeLength = 5;
    public const int CodeTtlSeconds = 300;
    public const int ResendCooldownSeconds = 120;
    public const int MaxAttempts = 5;
    public const int MaxCodesPerPhonePerHour = 5;
    public const int MaxCodesPerIpPerHour = 20;

    /// <summary>
    /// Bekor qilingan refresh token shu vaqt ichida qayta kelsa — parallel so'rov deb hisoblaymiz
    /// (o'g'irlik emas) va boshqa sessiyalarni yopmaymiz.
    /// </summary>
    private static readonly TimeSpan RefreshReuseGrace = TimeSpan.FromSeconds(30);

    private readonly IApplicationDbContext _db;
    private readonly IOtpSender _otpSender;
    private readonly IShopTokenGenerator _tokens;

    public ShopAuthService(IApplicationDbContext db, IOtpSender otpSender, IShopTokenGenerator tokens)
    {
        _db = db;
        _otpSender = otpSender;
        _tokens = tokens;
    }

    public async Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var phone = NormalizePhone(request.Phone);
        var now = DateTime.UtcNow;
        var hourAgo = now.AddHours(-1);

        var recent = await _db.ShopOtpCodes.IgnoreQueryFilters()
            .Where(o => o.Phone == phone && o.CreatedAt > hourAgo)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => o.CreatedAt)
            .ToListAsync(ct);

        if (recent.Count > 0)
        {
            var wait = ResendCooldownSeconds - (int)(now - recent[0]).TotalSeconds;
            if (wait > 0)
                throw new TooManyRequestsException($"Yangi kodni {wait} soniyadan keyin so'rash mumkin.", wait);
        }

        if (recent.Count >= MaxCodesPerPhonePerHour)
        {
            var wait = (int)Math.Ceiling((recent[^1].AddHours(1) - now).TotalSeconds);
            throw new TooManyRequestsException("Bu raqamga juda ko'p kod so'raldi. Birozdan keyin urinib ko'ring.", wait);
        }

        if (!string.IsNullOrEmpty(ipAddress))
        {
            var fromIp = await _db.ShopOtpCodes.IgnoreQueryFilters()
                .CountAsync(o => o.IpAddress == ipAddress && o.CreatedAt > hourAgo, ct);
            if (fromIp >= MaxCodesPerIpPerHour)
                throw new TooManyRequestsException("Juda ko'p so'rov. Birozdan keyin urinib ko'ring.", 3600);
        }

        var code = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, CodeLength)).ToString($"D{CodeLength}");
        var otp = new ShopOtpCode
        {
            Phone = phone,
            ExpiresAt = now.AddSeconds(CodeTtlSeconds),
            IpAddress = ipAddress
        };
        otp.CodeHash = HashCode(otp.Id, code);

        // Avval yuboramiz: yuborilmagan kod limitni band qilmasin.
        otp.Channel = await _otpSender.SendAsync(phone, code, CodeTtlSeconds, request.PreferSms, ct);

        _db.ShopOtpCodes.Add(otp);
        await _db.SaveChangesAsync(ct);

        return new SendOtpResponse
        {
            Phone = phone,
            Channel = otp.Channel,
            CodeLength = CodeLength,
            ExpiresInSeconds = CodeTtlSeconds,
            ResendAfterSeconds = ResendCooldownSeconds
        };
    }

    public async Task<ShopAuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
    {
        var phone = NormalizePhone(request.Phone);
        var code = new string(request.Code.Where(char.IsDigit).ToArray());
        var now = DateTime.UtcNow;

        var otp = await _db.ShopOtpCodes.IgnoreQueryFilters()
            .Where(o => o.Phone == phone && o.ConsumedAt == null && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (otp is null)
            throw new ValidationException("Kod eskirgan yoki topilmadi. Yangi kod so'rang.");

        if (otp.Attempts >= MaxAttempts)
            throw new ValidationException("Urinishlar soni tugadi. Yangi kod so'rang.");

        var expected = Convert.FromHexString(otp.CodeHash);
        var actual = Convert.FromHexString(HashCode(otp.Id, code));
        if (code.Length != CodeLength || !CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            otp.Attempts++;
            await _db.SaveChangesAsync(ct);
            var left = MaxAttempts - otp.Attempts;
            throw new ValidationException(left > 0
                ? $"Kod noto'g'ri. Yana {left} ta urinish qoldi."
                : "Kod noto'g'ri. Urinishlar soni tugadi, yangi kod so'rang.");
        }

        otp.ConsumedAt = now;

        var user = await _db.ShopUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Phone == phone, ct);

        if (user is null)
        {
            user = new ShopUser { Phone = phone, PhoneVerifiedAt = now };
            _db.ShopUsers.Add(user);
        }
        else
        {
            if (!user.IsActive || user.DeletedAt is not null)
            {
                await _db.SaveChangesAsync(ct);
                throw new UnauthorizedException("Akkaunt bloklangan. Qo'llab-quvvatlash xizmatiga murojaat qiling.");
            }

            user.PhoneVerifiedAt ??= now;
        }

        var response = IssueTokens(user, now);
        await _db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<ShopAuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var hash = HashToken(request.RefreshToken);

        var token = await _db.ShopRefreshTokens.IgnoreQueryFilters()
            .Include(t => t.ShopUser)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token?.ShopUser is null)
            throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");

        if (token.RevokedAt is { } revokedAt)
        {
            // Allaqachon almashtirilgan token qayta kelsa — o'g'irlangan bo'lishi mumkin:
            // mijozning barcha sessiyalarini yopamiz.
            if (now - revokedAt > RefreshReuseGrace)
            {
                var active = await _db.ShopRefreshTokens.IgnoreQueryFilters()
                    .Where(t => t.ShopUserId == token.ShopUserId && t.RevokedAt == null)
                    .ToListAsync(ct);
                foreach (var t in active) t.RevokedAt = now;
                await _db.SaveChangesAsync(ct);
            }

            throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");
        }

        if (token.ExpiresAt <= now)
            throw new UnauthorizedException("Sessiya tugagan. Qayta kiring.");

        var user = token.ShopUser;
        if (!user.IsActive || user.DeletedAt is not null)
            throw new UnauthorizedException("Akkaunt bloklangan. Qo'llab-quvvatlash xizmatiga murojaat qiling.");

        token.RevokedAt = now;
        var response = IssueTokens(user, now);
        await _db.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var hash = HashToken(request.RefreshToken);
        var token = await _db.ShopRefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct);
        if (token is null) return;

        token.RevokedAt = DateTime.UtcNow;

        // Ixtiyoriy: shu qurilmaning push tokeni (faqat shu mijozniki bo'lsa) o'chiriladi.
        if (!string.IsNullOrWhiteSpace(request.DeviceToken))
        {
            var deviceToken = request.DeviceToken.Trim();
            var devices = await _db.ShopDeviceTokens.IgnoreQueryFilters()
                .Where(d => d.ShopUserId == token.ShopUserId && d.Token == deviceToken).ToListAsync(ct);
            _db.ShopDeviceTokens.RemoveRange(devices);
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>"+998 90 123-45-67", "998901234567", "901234567" → "+998901234567".</summary>
    public static string NormalizePhone(string? input)
    {
        var digits = new string((input ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 9) digits = "998" + digits;
        if (digits.Length != 12 || !digits.StartsWith("998"))
            throw new ValidationException("Telefon raqam noto'g'ri. Namuna: +998 90 123 45 67");
        return "+" + digits;
    }

    private ShopAuthResponse IssueTokens(ShopUser user, DateTime now)
    {
        var (access, accessExpires) = _tokens.GenerateToken(user);

        var refresh = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var refreshExpires = now.AddDays(_tokens.RefreshTokenDays);
        _db.ShopRefreshTokens.Add(new ShopRefreshToken
        {
            ShopUserId = user.Id,
            TokenHash = HashToken(refresh),
            ExpiresAt = refreshExpires
        });

        return new ShopAuthResponse
        {
            AccessToken = access,
            AccessTokenExpiresAt = accessExpires,
            RefreshToken = refresh,
            RefreshTokenExpiresAt = refreshExpires,
            User = new ShopUserDto
            {
                Id = user.Id,
                Phone = user.Phone,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileCompleted = !string.IsNullOrWhiteSpace(user.FirstName)
            }
        };
    }

    private static string HashCode(Guid otpId, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{otpId:N}:{code}")));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
