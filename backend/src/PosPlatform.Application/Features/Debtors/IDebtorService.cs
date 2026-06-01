using PosPlatform.Application.Features.Debtors.Dtos;

namespace PosPlatform.Application.Features.Debtors;

public interface IDebtorService
{
    Task<IReadOnlyList<DebtorDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DebtorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DebtorDto> CreateAsync(CreateDebtorRequest request, CancellationToken cancellationToken = default);

    /// <summary>Qarz qo'shish yoki to'lov qabul qilish — balansni yangilaydi.</summary>
    Task<DebtorDto> AddTransactionAsync(Guid id, AddDebtorTransactionRequest request, CancellationToken cancellationToken = default);
}
