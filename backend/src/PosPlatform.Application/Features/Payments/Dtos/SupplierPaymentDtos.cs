using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Payments.Dtos;

public class SupplierPaymentDto
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public static SupplierPaymentDto FromEntity(SupplierPayment p) => new()
    {
        Id = p.Id,
        SupplierId = p.SupplierId,
        SupplierName = p.Supplier?.Name,
        Amount = p.Amount,
        Comment = p.Comment,
        CreatedAt = p.CreatedAt
    };
}

public class CreateSupplierPaymentRequest
{
    [Required]
    public Guid SupplierId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Comment { get; set; }
}
