using System.Net;
using System.Text.Json;
using PosCloud.Application.Common.Exceptions;

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
                _ => HttpStatusCode.InternalServerError
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Kutilmagan xatolik");
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
                    : ex.Message
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
