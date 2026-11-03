using Microsoft.AspNetCore.Authorization;

namespace PosCloud.Api.Authorization;

/// <summary>
/// Endpointga kirish uchun ma'lum permission code talab qiladi.
/// SuperAdmin barcha permission tekshiruvlarini chetlab o'tadi.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute, IAuthorizationRequirement, IAuthorizationRequirementData
{
    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }

    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [this];
}
