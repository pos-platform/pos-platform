using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Stores.Dtos;

public class StoreDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public List<CashboxDto> Cashboxes { get; set; } = new();

    public static StoreDto FromEntity(Branch b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Address = b.Address,
        Balance = b.Balance,
        IsActive = b.IsActive,
        Cashboxes = b.Cashboxes
            .OrderBy(c => c.Name)
            .Select(CashboxDto.FromEntity)
            .ToList()
    };
}

public class CashboxDto
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal Balance { get; set; }

    public static CashboxDto FromEntity(Cashbox c) => new()
    {
        Id = c.Id,
        BranchId = c.BranchId,
        Name = c.Name,
        IsActive = c.IsActive,
        Balance = c.Balance
    };
}

public class CreateStoreRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }
}

public class CreateCashboxRequest
{
    [Required]
    public Guid BranchId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
}
