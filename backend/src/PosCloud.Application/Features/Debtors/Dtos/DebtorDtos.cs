using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Debtors.Dtos;

public class DebtorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Comment { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DebtorTransactionDto> Transactions { get; set; } = new();

    public static DebtorDto FromEntity(Debtor d, bool withTransactions = false) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Phone = d.Phone,
        Comment = d.Comment,
        Balance = d.Balance,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedAt,
        Transactions = withTransactions
            ? d.Transactions.OrderByDescending(t => t.CreatedAt).Select(DebtorTransactionDto.FromEntity).ToList()
            : new()
    };
}

public class DebtorTransactionDto
{
    public Guid Id { get; set; }
    public DebtTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public static DebtorTransactionDto FromEntity(DebtorTransaction t) => new()
    {
        Id = t.Id,
        Type = t.Type,
        Amount = t.Amount,
        BalanceAfter = t.BalanceAfter,
        Comment = t.Comment,
        CreatedAt = t.CreatedAt
    };
}

public class CreateDebtorRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Comment { get; set; }
    /// <summary>Boshlang'ich qarz (ixtiyoriy).</summary>
    public decimal InitialDebt { get; set; }
}

public class AddDebtorTransactionRequest
{
    [Required]
    public DebtTransactionType Type { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Comment { get; set; }
}
