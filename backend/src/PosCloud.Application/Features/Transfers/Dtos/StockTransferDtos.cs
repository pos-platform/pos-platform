using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Transfers.Dtos;

public class StockTransferDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid FromBranchId { get; set; }
    public string? FromBranchName { get; set; }
    public Guid ToBranchId { get; set; }
    public string? ToBranchName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public List<StockTransferItemDto> Items { get; set; } = new();

    public static StockTransferDto FromEntity(StockTransfer t) => new()
    {
        Id = t.Id,
        Number = t.Number,
        FromBranchId = t.FromBranchId,
        FromBranchName = t.FromBranch?.Name,
        ToBranchId = t.ToBranchId,
        ToBranchName = t.ToBranch?.Name,
        Status = t.Status.ToString(),
        Comment = t.Comment,
        TotalCost = t.TotalCost,
        CreatedAt = t.CreatedAt,
        SentAt = t.SentAt,
        ReceivedAt = t.ReceivedAt,
        Items = t.Items.Select(StockTransferItemDto.FromEntity).ToList()
    };
}

public class StockTransferItemDto
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal LineTotal { get; set; }

    public static StockTransferItemDto FromEntity(StockTransferItem i) => new()
    {
        ProductId = i.ProductId,
        ProductName = i.Product?.Name,
        Barcode = i.Product?.Barcode,
        Quantity = i.Quantity,
        CostPrice = i.CostPrice,
        LineTotal = i.LineTotal
    };
}

public class CreateStockTransferRequest
{
    [Required]
    public Guid FromBranchId { get; set; }

    [Required]
    public Guid ToBranchId { get; set; }

    public string? Comment { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateStockTransferItemRequest> Items { get; set; } = new();
}

public class CreateStockTransferItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }
}
