using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.WriteOffs.Dtos;

public class WriteOffDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WriteOffItemDto> Items { get; set; } = new();

    public static WriteOffDto FromEntity(WriteOff w) => new()
    {
        Id = w.Id,
        Number = w.Number,
        Reason = w.Reason,
        TotalCost = w.TotalCost,
        CreatedAt = w.CreatedAt,
        Items = w.Items.Select(WriteOffItemDto.FromEntity).ToList()
    };
}

public class WriteOffItemDto
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal LineTotal { get; set; }

    public static WriteOffItemDto FromEntity(WriteOffItem i) => new()
    {
        ProductId = i.ProductId,
        ProductName = i.Product?.Name,
        Quantity = i.Quantity,
        CostPrice = i.CostPrice,
        LineTotal = i.LineTotal
    };
}

public class CreateWriteOffRequest
{
    public Guid? BranchId { get; set; }
    public string? Reason { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateWriteOffItemRequest> Items { get; set; } = new();
}

public class CreateWriteOffItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }
}
