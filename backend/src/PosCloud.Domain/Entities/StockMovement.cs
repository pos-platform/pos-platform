using PosCloud.Domain.Common;
using PosCloud.Domain.Enums;

namespace PosCloud.Domain.Entities;

/// <summary>
/// Ombor harakati jurnali: filial qoldig'i (BranchStock.Quantity) har o'zgarganda bitta
/// yozuv qo'shiladi. Shu jurnal orqali "qoldiq nega shuncha?" savoliga javob beriladi
/// va tovar aylanmasi (boshlang'ich → kirim/chiqim → oxirgi) hisoboti quriladi.
/// Yozuvlar o'zgartirilmaydi va o'chirilmaydi — xato hujjat bekor qilinsa, teskari yozuv qo'shiladi.
/// </summary>
public class StockMovement : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public StockMovementType Type { get; set; }

    /// <summary>Qoldiq o'zgarishi: kirim musbat, chiqim manfiy.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Harakatdan keyingi filial qoldig'i.</summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>Birlik tannarxi (ma'lum bo'lsa).</summary>
    public decimal? CostPrice { get; set; }

    /// <summary>Asos hujjat (kirim, savdo, transfer...) ID'si.</summary>
    public Guid? DocumentId { get; set; }

    /// <summary>Asos hujjat raqami (ko'rsatish uchun).</summary>
    public string? DocumentNumber { get; set; }

    /// <summary>Amalni bajargan foydalanuvchi.</summary>
    public Guid? UserId { get; set; }
}
