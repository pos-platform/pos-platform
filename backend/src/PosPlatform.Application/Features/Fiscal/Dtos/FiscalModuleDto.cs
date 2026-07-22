using PosPlatform.Domain.Entities;

namespace PosPlatform.Application.Features.Fiscal.Dtos;

/// <summary>Fiskal modul (fleshka) kartochkasi.</summary>
public class FiscalModuleDto
{
    public Guid Id { get; set; }
    public string ModuleId { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? Inn { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TerminalId { get; set; }
    public string? Note { get; set; }

    public static FiscalModuleDto FromEntity(FiscalModule m) => new()
    {
        Id = m.Id,
        ModuleId = m.ModuleId,
        OwnerName = m.OwnerName,
        Inn = m.Inn,
        RegistrationNumber = m.RegistrationNumber,
        TerminalId = m.TerminalId,
        Note = m.Note
    };
}

/// <summary>Modul kartochkasini yaratish/yangilash (ModuleId bo'yicha upsert).</summary>
public class SaveFiscalModuleRequest
{
    public string ModuleId { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? Inn { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TerminalId { get; set; }
    public string? Note { get; set; }
}
