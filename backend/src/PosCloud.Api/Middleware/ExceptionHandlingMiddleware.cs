using System.Net;
using System.Security.Claims;
using System.Text.Json;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Enums;

namespace PosCloud.Api.Middleware;

/// <summary>
/// Application qatlami exceptionlarini mos HTTP status kodlariga aylantiradi.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var statusCode = ex switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                ConflictException => HttpStatusCode.Conflict,
                ValidationException => HttpStatusCode.BadRequest,
                UnauthorizedException => HttpStatusCode.Unauthorized,
                TooManyRequestsException => HttpStatusCode.TooManyRequests,
                // Bir vaqtda o'zgartirish (masalan, kuryer qabul qilayotganda buyurtma yangilandi) — 500 emas, 409.
                Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => HttpStatusCode.Conflict,
                _ => HttpStatusCode.InternalServerError
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Kutilmagan xatolik");
                await RecordAsync(context, ex);
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            if (ex is TooManyRequestsException { RetryAfterSeconds: { } retryAfter })
                context.Response.Headers.RetryAfter = retryAfter.ToString();

            var payload = JsonSerializer.Serialize(new
            {
                status = (int)statusCode,
                error = statusCode == HttpStatusCode.InternalServerError
                    ? "Serverda kutilmagan xatolik yuz berdi."
                    : ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException
                        ? "Ma'lumot hozirgina o'zgardi. Sahifani yangilab, qayta urinib ko'ring."
                        : ex.Message
            });

            await context.Response.WriteAsync(payload);
        }
    }

    /// <summary>500 xatoni SuperAdmin uchun yozadi (alohida scope; yozish xatosi javobni buzmaydi). Body/headers yozilmaydi.</summary>
    private static async Task RecordAsync(HttpContext context, Exception ex)
    {
        try
        {
            var recorder = context.RequestServices.GetService<ISystemErrorRecorder>();
            if (recorder is null) return;
            var user = context.User;
            var entry = SystemErrorEntry.FromException(SystemErrorSource.Api, ex) with
            {
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                StatusCode = StatusCodes.Status500InternalServerError,
                OrganizationId = Guid.TryParse(user.FindFirstValue("organizationId"), out var org) ? org : null,
                BranchId = Guid.TryParse(user.FindFirstValue("branchId"), out var branch) ? branch : null,
                UserId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : null,
                UserName = user.FindFirstValue(ClaimTypes.Name),
                RequestId = context.TraceIdentifier
            };
            await recorder.RecordAsync(entry, CancellationToken.None);
        }
        catch
        {
            // Xatolikni yozib bo'lmadi — asosiy javob baribir qaytadi.
        }
    }
}
