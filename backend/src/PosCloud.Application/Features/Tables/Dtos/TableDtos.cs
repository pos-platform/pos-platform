using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Tables.Dtos;

public class TableDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Hall { get; set; }
    public int Seats { get; set; }
    public Guid? BranchId { get; set; }
    public TableStatus Status { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Ushbu stoldagi ochiq buyurtma (Pending Sale) — bo'lsa.</summary>
    public Guid? OpenOrderId { get; set; }
    public decimal? OpenOrderTotal { get; set; }

    public static TableDto FromEntity(RestaurantTable t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Hall = t.Hall,
        Seats = t.Seats,
        BranchId = t.BranchId,
        Status = t.Status,
        IsActive = t.IsActive
    };
}

public class CreateTableDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Hall { get; set; }

    [Range(0, 1000)]
    public int Seats { get; set; }
    public Guid? BranchId { get; set; }
}

public class UpdateTableDto : CreateTableDto
{
    public bool IsActive { get; set; } = true;
}
