using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Stores.Dtos;

public class StoreUserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public bool IsActive { get; set; }

    public static StoreUserDto FromEntity(User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        RoleName = u.AppRole?.Name,
        IsActive = u.IsActive
    };
}

public class StoreDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public BusinessType BusinessType { get; set; }
    public List<CashboxDto> Cashboxes { get; set; } = new();
    public List<StoreUserDto> Users { get; set; } = new();

    public static StoreDto FromEntity(Branch b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Address = b.Address,
        Balance = b.Balance,
        IsActive = b.IsActive,
        BusinessType = b.BusinessType,
        Cashboxes = b.Cashboxes
            .OrderBy(c => c.Name)
            .Select(CashboxDto.FromEntity)
            .ToList(),
        Users = b.Users
            .OrderBy(u => u.Username)
            .Select(StoreUserDto.FromEntity)
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

    /// <summary>Filial turi — Do'kon (0) yoki Restoran (1).</summary>
    public BusinessType BusinessType { get; set; } = BusinessType.Store;
}

public class SetBusinessTypeRequest
{
    public BusinessType BusinessType { get; set; }
}

public class CreateCashboxRequest
{
    [Required]
    public Guid BranchId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
}

/// <summary>Foydalanuvchini do'konga biriktirish yoki uzish (branchId = null → uzish).</summary>
public class AssignUserToBranchRequest
{
    public Guid? BranchId { get; set; }
}
