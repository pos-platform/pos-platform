using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Stock.Dtos;

public class StockReceiptDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? BranchId { get; set; }
    public string? Comment { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StockReceiptItemDto> Items { get; set; } = new();

    public static StockReceiptDto FromEntity(StockReceipt r) => new()
    {
        Id = r.Id,
        Number = r.Number,
        SupplierId = r.SupplierId,
        SupplierName = r.Supplier?.Name,
        BranchId = r.BranchId,
        Comment = r.Comment,
        TotalAmount = r.TotalAmount,
        CreatedAt = r.CreatedAt,
        Items = r.Items.Select(StockReceiptItemDto.FromEntity).ToList()
    };
}

public class StockReceiptItemDto
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal LineTotal { get; set; }

    public static StockReceiptItemDto FromEntity(StockReceiptItem i) => new()
    {
        ProductId = i.ProductId,
        ProductName = i.Product?.Name,
        Quantity = i.Quantity,
        CostPrice = i.CostPrice,
        LineTotal = i.LineTotal
    };
}

public class CreateStockReceiptRequest
{
    [Required]
    public Guid SupplierId { get; set; }

    public Guid? BranchId { get; set; }

    public string? Comment { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateStockReceiptItemRequest> Items { get; set; } = new();
}

public class CreateStockReceiptItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }
}
