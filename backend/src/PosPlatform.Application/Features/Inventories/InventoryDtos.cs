using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Inventories;

public record InventoryDto(
    Guid Id,
    string Number,
    string? Notes,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<InventoryItemDto> Items)
{
    public static InventoryDto FromEntity(Inventory inv) => new(
        inv.Id,
        inv.Number,
        inv.Notes,
        inv.Status.ToString(),
        inv.CreatedAt,
        inv.Items.Select(InventoryItemDto.FromEntity).ToList());
}

public record InventoryItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductBarcode,
    decimal ExpectedQty,
    decimal ActualQty,
    decimal Discrepancy)
{
    public static InventoryItemDto FromEntity(InventoryItem i) => new(
        i.Id,
        i.ProductId,
        i.Product?.Name ?? string.Empty,
        i.Product?.Barcode ?? string.Empty,
        i.ExpectedQty,
        i.ActualQty,
        i.ActualQty - i.ExpectedQty);
}

/// <summary>ProductIds=null yoki bo'sh — barcha mahsulotlarni qo'shadi.</summary>
public record CreateInventoryRequest(
    string? Notes,
    Guid? BranchId,
    List<Guid>? ProductIds);

public record UpdateInventoryRequest(
    [Required, MinLength(1)] List<UpdateInventoryItemRequest> Items);

public record UpdateInventoryItemRequest(
    [Required] Guid ItemId,
    [Range(0, double.MaxValue)] decimal ActualQty);
