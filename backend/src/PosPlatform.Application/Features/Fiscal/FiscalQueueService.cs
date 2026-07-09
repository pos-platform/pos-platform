using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Fiscal.Dtos;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Fiscal;

public class FiscalQueueService : IFiscalQueueService
{
    private readonly IApplicationDbContext _db;

    public FiscalQueueService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PendingReceiptDto>> GetPendingAsync(int max, CancellationToken cancellationToken = default)
    {
        max = Math.Clamp(max, 1, 100);

        var sales = await _db.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Include(s => s.Receipt)
            .Where(s => s.Receipt != null && s.Receipt.FiscalStatus == FiscalStatus.Queued)
            .OrderBy(s => s.CreatedAt)
            .Take(max)
            .ToListAsync(cancellationToken);

        return sales.Select(PendingReceiptDto.FromEntity).ToList();
    }

    public async Task ApplyResultAsync(Guid saleId, FiscalResultDto result, CancellationToken cancellationToken = default)
    {
        var receipt = await _db.Receipts
            .FirstOrDefaultAsync(r => r.SaleId == saleId, cancellationToken);

        if (receipt is null)
        {
            throw new NotFoundException($"Chek topilmadi (SaleId): {saleId}");
        }

        if (result.Success)
        {
            receipt.FiscalStatus = FiscalStatus.Registered;
            receipt.FiscalSign = result.FiscalSign;
            receipt.FiscalModuleId = result.FiscalModuleId;
            receipt.FiscalRegistrationNumber = result.FiscalRegistrationNumber;
            receipt.FiscalReceiptNumber = result.FiscalReceiptNumber;
            receipt.QrCodeUrl = result.QrCodeUrl;
            receipt.FiscalizedAt = result.FiscalizedAt ?? DateTime.UtcNow;
            receipt.ErrorMessage = null;
        }
        else
        {
            receipt.FiscalStatus = FiscalStatus.Failed;
            receipt.ErrorMessage = result.ErrorMessage;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
