using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Suppliers.Dtos;

public class SupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public static SupplierDto FromEntity(Supplier s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Phone = s.Phone,
        Address = s.Address,
        Balance = s.Balance,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt
    };
}

public class CreateSupplierRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class UpdateSupplierRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
