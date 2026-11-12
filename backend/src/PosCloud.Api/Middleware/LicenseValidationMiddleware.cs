using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Enums;

namespace PosCloud.Api.Middleware;

/// <summary>
/// Har bir autentifikatsiya qilingan so'rovda obuna/hisob holatini qayta tekshiradi
/// (faqat login paytida emas). Muddati tugagan yoki bloklangan foydalanuvchi tokeni
/// hali amal qilsa ham, darhol 401 oladi. SuperAdmin tekshirilmaydi.
/// </summary>
public class LicenseValidationMiddleware
{
    private readonly RequestDelegate _next;

    public LicenseValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext db, ICurrentUser currentUser)
    {
        var principal = context.User;
        if (principal.Identity?.IsAuthenticated == true
            && !principal.IsInRole("SuperAdmin")
            // Mobil ilova mijozi xodim emas — Users jadvalida yo'q, obuna unga taalluqli emas.
            && !principal.IsInRole(PosCloud.Application.Features.ShopAuth.ShopRoles.Customer)
            && currentUser.UserId is Guid userId)
        {
            var today = DateTime.UtcNow.Date;
            var user = await db.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Obuna faqat Kassir/Menejer uchun tekshiriladi. Admin panel bepul.
            var needsLicense = user?.Role == UserRole.Cashier || user?.Role == UserRole.Manager;
            // Doimiy (IsLifetime) litsenziyali foydalanuvchilar obuna muddati tekshiruvidan o'tkazilmaydi.
            var invalid = user is null
                || !user.IsActive
                || user.Organization is { IsActive: false }
                || (needsLicense && !user.IsLifetime
                    && (user.LicensedUntil is null || user.LicensedUntil.Value.Date < today));

            if (invalid)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    status = 401,
                    error = "Obuna muddati tugagan yoki hisob bloklangan. Administrator bilan bog'laning."
                }));
                return;
            }
        }

        await _next(context);
    }
}
