using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Customers.Dtos;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal LoyaltyPoints { get; set; }
    public decimal DiscountPercent { get; set; }
    public bool IsActive { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public static CustomerDto FromEntity(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        LoyaltyPoints = c.LoyaltyPoints,
        DiscountPercent = c.DiscountPercent,
        IsActive = c.IsActive,
        Comment = c.Comment,
        CreatedAt = c.CreatedAt
    };
}

public class LoyaltyTransactionDto
{
    public Guid Id { get; set; }
    public decimal Points { get; set; }
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    public Guid? SaleId { get; set; }
    public DateTime CreatedAt { get; set; }

    public static LoyaltyTransactionDto FromEntity(LoyaltyTransaction t) => new()
    {
        Id = t.Id,
        Points = t.Points,
        Amount = t.Amount,
        Comment = t.Comment,
        SaleId = t.SaleId,
        CreatedAt = t.CreatedAt
    };
}

public class CreateCustomerRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(512)]
    public string? Address { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    public string? Comment { get; set; }
}

public class UpdateCustomerRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(512)]
    public string? Address { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    public string? Comment { get; set; }

    public bool IsActive { get; set; }
}

public class AdjustPointsRequest
{
    public decimal Points { get; set; }
    public string? Comment { get; set; }
}
