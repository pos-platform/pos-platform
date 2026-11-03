using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;
using PosCloud.Application.Features.Labels.Dtos;
using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Labels;

public class LabelService : ILabelService
{
    private readonly IApplicationDbContext _db;

    public LabelService(IApplicationDbContext db) => _db = db;

    public async Task<LabelPrintJobDto> CreateJobAsync(CreateLabelJobRequest request, CancellationToken cancellationToken = default)
    {
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var job = new LabelPrintJob();

        foreach (var line in request.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new NotFoundException($"Mahsulot topilmadi: {line.ProductId}");

            job.Items.Add(new LabelPrintJobItem
            {
                ProductName = product.Name,
                Price = product.Price,
                Barcode = product.Barcode,
                Copies = line.Copies
            });
        }

        _db.LabelPrintJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        return LabelPrintJobDto.FromEntity(job);
    }

    public async Task<LabelPrintJobDto?> GetPendingJobAsync(CancellationToken cancellationToken = default)
    {
        var job = await _db.LabelPrintJobs
            .AsNoTracking()
            .Include(j => j.Items)
            .Where(j => !j.Printed)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return job is null ? null : LabelPrintJobDto.FromEntity(job);
    }

    public async Task MarkPrintedAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _db.LabelPrintJobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken)
            ?? throw new NotFoundException($"Yorliq vazifasi topilmadi: {jobId}");

        job.Printed = true;
        job.PrintedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
