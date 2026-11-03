using Microsoft.AspNetCore.Authorization;

namespace PosCloud.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<HasPermissionAttribute>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasPermissionAttribute requirement)
    {
        // SuperAdmin barcha ruxsat tekshiruvlarini chetlab o'tadi.
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var hasPermission = context.User
            .FindAll("perm")
            .Any(c => c.Value == requirement.Permission);

        if (hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
