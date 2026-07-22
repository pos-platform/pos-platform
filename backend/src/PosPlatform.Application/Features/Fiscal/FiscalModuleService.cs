using Microsoft.EntityFrameworkCore;
using PosPlatform.Application.Common.Exceptions;
using PosPlatform.Application.Common.Interfaces;
using PosPlatform.Application.Features.Fiscal.Dtos;
using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Fiscal;

/// <summary>Fiskal modul kartochkalari — modul ID ga nom/egasi biriktiradi.</summary>
public interface IFiscalModuleService
{
    Task<IReadOnlyList<FiscalModuleDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>ModuleId bo'yicha upsert: kartochka bo'lmasa yaratadi, bo'lsa yangilaydi.</summary>
    Task<FiscalModuleDto> SaveAsync(SaveFiscalModuleRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class FiscalModuleService : IFiscalModuleService
{
    private readonly IApplicationDbContext _db;

    public FiscalModuleService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FiscalModuleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var modules = await _db.FiscalModules
            .AsNoTracking()
            .OrderBy(m => m.ModuleId)
            .ToListAsync(ct);
        return modules.Select(FiscalModuleDto.FromEntity).ToList();
    }

    public async Task<FiscalModuleDto> SaveAsync(SaveFiscalModuleRequest request, CancellationToken ct = default)
    {
        var moduleId = (request.ModuleId ?? string.Empty).Trim();
        if (moduleId.Length == 0)
            throw new ValidationException("Fiskal modul ID si bo'sh bo'lmasligi kerak.");

        var entity = await _db.FiscalModules.FirstOrDefaultAsync(m => m.ModuleId == moduleId, ct);
        if (entity is null)
        {
            entity = new FiscalModule { ModuleId = moduleId };
            _db.FiscalModules.Add(entity);
        }

        entity.OwnerName = Trim(request.OwnerName);
        entity.Inn = Trim(request.Inn);
        entity.RegistrationNumber = Trim(request.RegistrationNumber);
        entity.TerminalId = Trim(request.TerminalId);
        entity.Note = Trim(request.Note);

        await _db.SaveChangesAsync(ct);
        return FiscalModuleDto.FromEntity(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.FiscalModules.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Fiskal modul kartochkasi topilmadi.");
        _db.FiscalModules.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private static string? Trim(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
