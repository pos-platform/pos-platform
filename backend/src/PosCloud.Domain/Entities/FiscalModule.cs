using PosCloud.Domain.Common;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Fiskal modul (fleshka) kartochkasi — chekdagi modul ID siga inson o'qiy oladigan
/// nom biriktiradi: kimga ro'yxatdan o'tgani (ism-sharif/tashkilot), INN, RS raqami,
/// qaysi kassada turgani. Hisobotlarda modul ID o'rniga shu nom ko'rsatiladi.
/// </summary>
public class FiscalModule : BaseEntity
{
    /// <summary>Modulning fiskal ID si (Receipt.FiscalModuleId bilan bir xil qiymat).</summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>Modul kimga tegishli — ism-sharif yoki tashkilot nomi.</summary>
    public string? OwnerName { get; set; }

    /// <summary>Soliq to'lovchi INN/STIR (ixtiyoriy).</summary>
    public string? Inn { get; set; }

    /// <summary>Kassa apparatining ro'yxatga olish raqami (RS).</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>Modul qaysi kassa stantsiyasida (PC) turgani — masalan "KASSA-1".</summary>
    public string? TerminalId { get; set; }

    /// <summary>Qo'shimcha izoh.</summary>
    public string? Note { get; set; }
}
