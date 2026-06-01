using System.Security.Claims;
using PosPlatform.Application.Common.Interfaces;

namespace PosPlatform.Api.Services;

/// <summary>JWT claim'laridan joriy foydalanuvchini o'qiydi.</summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Username => Principal?.FindFirstValue(ClaimTypes.Name);

    public Guid? BranchId =>
        Guid.TryParse(Principal?.FindFirstValue("branchId"), out var id) ? id : null;
}
