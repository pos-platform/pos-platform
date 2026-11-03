using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PosCloud.Api.Authentication;

/// <summary>
/// Lokal fiskal agent endpointlarini oddiy API-kalit bilan himoyalaydi
/// (JWT emas — agent foydalanuvchi emas, qurilma). Kalit appsettings'dagi
/// "Agent:ApiKey" qiymati bilan solishtiriladi; "X-Agent-Key" sarlavhasida keladi.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AgentApiKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Agent-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuredKey = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Agent:ApiKey"];

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrWhiteSpace(configuredKey) ||
            !string.Equals(provided, configuredKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
