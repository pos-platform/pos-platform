using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Debtors.Dtos;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Debtors;

public class DebtorService : IDebtorService
{
    private readonly IApplicationDbContext _db;

    public DebtorService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DebtorDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var debtors = await _db.Debtors
            .AsNoTracking()
            .OrderByDescending(d => d.Balance)
            .ToListAsync(cancellationToken);

        return debtors.Select(d => DebtorDto.FromEntity(d)).ToList();
    }

    public async Task<DebtorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var debtor = await _db.Debtors
            .AsNoTracking()
            .Include(d => d.Transactions)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Qarzdor topilmadi: {id}");

        return DebtorDto.FromEntity(debtor, withTransactions: true);
    }

    public async Task<DebtorDto> CreateAsync(CreateDebtorRequest request, CancellationToken cancellationToken = default)
    {
        var debtor = new Debtor
        {
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Comment = request.Comment?.Trim(),
            Balance = 0,
            IsActive = true
        };

        if (request.InitialDebt > 0)
        {
            debtor.Balance = request.InitialDebt;
            debtor.Transactions.Add(new DebtorTransaction
            {
                Type = DebtTransactionType.Debt,
                Amount = request.InitialDebt,
                BalanceAfter = request.InitialDebt,
                Comment = "Boshlang'ich qarz"
            });
        }

        _db.Debtors.Add(debtor);
        await _db.SaveChangesAsync(cancellationToken);

        return DebtorDto.FromEntity(debtor);
    }

    public async Task<DebtorDto> AddTransactionAsync(Guid id, AddDebtorTransactionRequest request, CancellationToken cancellationToken = default)
    {
        var debtor = await _db.Debtors.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Qarzdor topilmadi: {id}");

        if (request.Amount <= 0)
        {
            throw new ValidationException("Summa 0 dan katta bo'lishi kerak.");
        }

        debtor.Balance += request.Type == DebtTransactionType.Debt ? request.Amount : -request.Amount;

        _db.DebtorTransactions.Add(new DebtorTransaction
        {
            DebtorId = debtor.Id,
            Type = request.Type,
            Amount = request.Amount,
            BalanceAfter = debtor.Balance,
            Comment = request.Comment?.Trim()
        });

        await _db.SaveChangesAsync(cancellationToken);

        return DebtorDto.FromEntity(debtor);
    }
}
