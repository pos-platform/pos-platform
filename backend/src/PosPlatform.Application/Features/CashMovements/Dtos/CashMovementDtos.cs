using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.CashMovements.Dtos;

public class CashMovementDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CashShiftId { get; set; }
    public DateTime CreatedAt { get; set; }

    public static CashMovementDto FromEntity(CashMovement m) => new()
    {
        Id = m.Id,
        Type = m.Type.ToString(),
        Amount = m.Amount,
        Comment = m.Comment,
        BranchId = m.BranchId,
        CashShiftId = m.CashShiftId,
        CreatedAt = m.CreatedAt
    };
}

public class CreateCashMovementRequest
{
    [Required]
    public CashMovementType Type { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Comment { get; set; }

    public Guid? BranchId { get; set; }

    /// <summary>Joriy ochiq smenaga avtomatik bog'lanadi agar null bo'lsa.</summary>
    public Guid? CashShiftId { get; set; }
}
