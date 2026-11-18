using Microsoft.EntityFrameworkCore;
using PosCloud.Application.Common.Exceptions;
using PosCloud.Application.Common.Interfaces;

namespace PosCloud.Application.Common;

/// <summary>
/// Filialga biriktirilgan foydalanuvchi faqat o'z filiali bilan ishlaydi: hisobot/ro'yxatlarda
/// so'ralgan filial e'tiborga olinmaydi, hujjat yaratishda boshqa filial rad etiladi.
/// </summary>
public static class BranchScope
{
    /// <summary>O'qish uchun: filialga biriktirilgan bo'lsa — o'sha filial, aks holda so'ralgani (null = hammasi).</summary>
    public static Guid? ForRead(this ICurrentUser user, Guid? requested) => user.BranchId ?? requested;

    /// <summary>
    /// Hujjat yaratish uchun filialni aniqlaydi va tekshiradi: biriktirilgan foydalanuvchi boshqa
    /// filialga yoza olmaydi; filial shu tashkilotga tegishli bo'lishi shart (tenant filtri orqali).
    /// Filial aniqlanmasa null qaytaradi — chaqiruvchi o'z xabari bilan rad etadi.
    /// </summary>
    public static async Task<Guid?> ResolveForWriteAsync(this ICurrentUser user, IApplicationDbContext db,
        Guid? requested, CancellationToken ct)
    {
        if (user.BranchId is { } own && requested is { } req && req != own)
            throw new ValidationException("Siz faqat o'z filialingiz bo'yicha hujjat yarata olasiz.");

        var branchId = requested ?? user.BranchId;
        if (branchId is { } id && !await db.Branches.AnyAsync(b => b.Id == id, ct))
            throw new ValidationException("Filial topilmadi.");

        return branchId;
    }
}
