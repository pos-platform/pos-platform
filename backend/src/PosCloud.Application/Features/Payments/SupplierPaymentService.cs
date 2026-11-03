using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Payments.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Payments;

public class SupplierPaymentService : ISupplierPaymentService
{
    private readonly IApplicationDbContext _db;

    public SupplierPaymentService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SupplierPaymentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var payments = await _db.SupplierPayments
            .AsNoTracking()
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(SupplierPaymentDto.FromEntity).ToList();
    }

    public async Task<SupplierPaymentDto> CreateAsync(CreateSupplierPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken)
            ?? throw new NotFoundException($"Yetkazib beruvchi topilmadi: {request.SupplierId}");

        if (request.Amount <= 0)
        {
            throw new ValidationException("To'lov summasi 0 dan katta bo'lishi kerak.");
        }

        var payment = new SupplierPayment
        {
            SupplierId = supplier.Id,
            Amount = request.Amount,
            Comment = request.Comment?.Trim()
        };

        // To'lov yetkazib beruvchiga qarzni kamaytiradi.
        supplier.Balance -= request.Amount;

        _db.SupplierPayments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        payment.Supplier = supplier;
        return SupplierPaymentDto.FromEntity(payment);
    }
}
