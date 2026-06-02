using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Audit.Dtos;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public bool Success { get; set; }
    public string? ActorUsername { get; set; }
    public string? IpAddress { get; set; }
    public Guid? OrganizationId { get; set; }

    public static AuditLogDto FromEntity(AuditLog a) => new()
    {
        Id = a.Id,
        CreatedAt = a.CreatedAt,
        Action = a.Action,
        Detail = a.Detail,
        Success = a.Success,
        ActorUsername = a.ActorUsername,
        IpAddress = a.IpAddress,
        OrganizationId = a.OrganizationId
    };
}
