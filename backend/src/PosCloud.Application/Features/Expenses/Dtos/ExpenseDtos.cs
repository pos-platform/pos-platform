using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Expenses.Dtos;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ExpenseDto FromEntity(Expense e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Amount = e.Amount,
        Comment = e.Comment,
        BranchId = e.BranchId,
        CreatedAt = e.CreatedAt
    };
}

public class CreateExpenseRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public string? Comment { get; set; }

    public Guid? BranchId { get; set; }
}
