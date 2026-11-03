using System.ComponentModel.DataAnnotations;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Shifts.Dtos;

public class OpenShiftRequest
{
    public Guid? BranchId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OpeningCash { get; set; }

    /// <summary>
    /// Smena ochilayotgan kassa stantsiyasi ID (desktop Sozlamalaridan). Foydalanuvchiga kassa
    /// biriktirilgan bo'lsa — mos kelmasa smena ochilmaydi.
    /// </summary>
    public string? TerminalId { get; set; }
}

public class CloseShiftRequest
{
    /// <summary>Yopishda sanab chiqilgan haqiqiy naqd.</summary>
    [Range(0, double.MaxValue)]
    public decimal CountedCash { get; set; }

    /// <summary>
    /// Kassa ilovasi Z-hisobotni o'zi (lokal printerda) chop etadi.
    /// true bo'lsa smena darhol "hisobot chop etilgan" deb belgilanadi va
    /// agent uni qayta chop etmaydi (bitta smenaga ikkita hisobot chiqmasligi uchun).
    /// </summary>
    public bool PrintedLocally { get; set; }
}

/// <summary>Bitta to'lov turi bo'yicha smena yakuni (fiskal/nofiskal ajratilgan).</summary>
public class ShiftPaymentLineDto
{
    /// <summary>To'lov turi: Cash, Card, Transfer, Debt, Mixed.</summary>
    public string Method { get; set; } = string.Empty;

    public int SaleCount { get; set; }
    public decimal SaleAmount { get; set; }

    public int RefundCount { get; set; }
    public decimal RefundAmount { get; set; }

    /// <summary>Fiskalizatsiya qilingan (soliq QR kodi bor) savdolar.</summary>
    public int FiscalCount { get; set; }
    public decimal FiscalAmount { get; set; }

    /// <summary>Fiskalizatsiya qilinmagan (QR kodsiz) savdolar.</summary>
    public int NonFiscalCount { get; set; }
    public decimal NonFiscalAmount { get; set; }

    public decimal NetAmount => SaleAmount - RefundAmount;
}

public class CashShiftDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CashierId { get; set; }
    public string? CashierName { get; set; }

    /// <summary>Smena ochilgan kassa stantsiyasi ("KASSA-1").</summary>
    public string? TerminalId { get; set; }
    public ShiftStatus Status { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal CashSalesNet { get; set; }
    public decimal CardSalesNet { get; set; }
    public decimal TransferSalesNet { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal CashDifference { get; set; }

    // X-Z hisobot maydonlari (faqat CloseAsync va GetByIdAsync da to'ldiriladi)
    public int SaleCount { get; set; }
    public int RefundCount { get; set; }
    public decimal TotalItemsSold { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DebtSalesNet { get; set; }

    /// <summary>To'lov turlari kesimi (har biri fiskal/nofiskal ajratilgan).</summary>
    public List<ShiftPaymentLineDto> Payments { get; set; } = new();

    /// <summary>Fiskalizatsiya qilingan (QR kodli) savdolar jami.</summary>
    public int FiscalSaleCount { get; set; }
    public decimal FiscalSalesTotal { get; set; }

    /// <summary>Fiskalizatsiya qilinmagan (QR kodsiz) savdolar jami.</summary>
    public int NonFiscalSaleCount { get; set; }
    public decimal NonFiscalSalesTotal { get; set; }

    public static CashShiftDto FromEntity(CashShift s, IEnumerable<Sale>? sales = null, string? cashierName = null)
    {
        var dto = new CashShiftDto
        {
            Id = s.Id,
            BranchId = s.BranchId,
            CashierId = s.CashierId,
            CashierName = cashierName,
            TerminalId = s.TerminalId,
            Status = s.Status,
            OpenedAt = s.OpenedAt,
            ClosedAt = s.ClosedAt,
            OpeningCash = s.OpeningCash,
            CountedCash = s.CountedCash,
            TotalSales = s.TotalSales,
            TotalRefunds = s.TotalRefunds,
            CashSalesNet = s.CashSalesNet,
            CardSalesNet = s.CardSalesNet,
            TransferSalesNet = s.TransferSalesNet,
            ExpectedCash = s.ExpectedCash,
            CashDifference = s.CashDifference
        };

        if (sales is not null)
        {
            var saleList = sales.ToList();
            dto.SaleCount   = saleList.Count(s2 => s2.Type == SaleType.Sale);
            dto.RefundCount = saleList.Count(s2 => s2.Type == SaleType.Refund);
            dto.TotalItemsSold = saleList
                .Where(s2 => s2.Type == SaleType.Sale)
                .SelectMany(s2 => s2.Items)
                .Sum(i => i.Quantity);
            dto.VatAmount = saleList
                .Where(s2 => s2.Type == SaleType.Sale)
                .Sum(s2 => s2.VatAmount);
            dto.DebtSalesNet = saleList
                .Where(s2 => s2.Type == SaleType.Sale && s2.PaymentMethod == PaymentMethod.Debt)
                .Sum(s2 => s2.TotalAmount)
                - saleList
                .Where(s2 => s2.Type == SaleType.Refund && s2.PaymentMethod == PaymentMethod.Debt)
                .Sum(s2 => s2.TotalAmount);

            dto.Payments = BuildPaymentLines(saleList);
            dto.FiscalSaleCount     = dto.Payments.Sum(p => p.FiscalCount);
            dto.FiscalSalesTotal    = dto.Payments.Sum(p => p.FiscalAmount);
            dto.NonFiscalSaleCount  = dto.Payments.Sum(p => p.NonFiscalCount);
            dto.NonFiscalSalesTotal = dto.Payments.Sum(p => p.NonFiscalAmount);
        }

        return dto;
    }

    /// <summary>
    /// To'lov turlari kesimini quradi. Savdo "fiskal" deb hisoblanadi, agar unga tegishli
    /// chek soliq tizimida ro'yxatdan o'tib QR kod olgan bo'lsa; aks holda "QR kodsiz".
    /// Sale.Receipt yuklanmagan bo'lsa hammasi nofiskal ko'rinadi, shuning uchun
    /// so'rovlarda Include(s => s.Receipt) bo'lishi shart.
    /// </summary>
    private static List<ShiftPaymentLineDto> BuildPaymentLines(IReadOnlyCollection<Sale> sales)
    {
        // Turlar doim bir xil tartibda va nol bo'lsa ham chiqsin — chekda ustun barqaror bo'ladi.
        var methods = new[]
        {
            PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.Transfer,
            PaymentMethod.Debt, PaymentMethod.Mixed
        };

        var lines = new List<ShiftPaymentLineDto>();
        foreach (var method in methods)
        {
            var ofMethod = sales.Where(s => s.PaymentMethod == method).ToList();
            var salesOfMethod = ofMethod.Where(s => s.Type == SaleType.Sale).ToList();
            var refundsOfMethod = ofMethod.Where(s => s.Type == SaleType.Refund).ToList();

            if (salesOfMethod.Count == 0 && refundsOfMethod.Count == 0)
                continue;

            // Nasiya ataylab fiskalizatsiya qilinmaydi (pul olinmagan) — uni "QR kodsiz"
            // buzilish sifatida sanamaymiz, aks holda hisobotdagi ogohlantirish ma'nosini
            // yo'qotadi. Qarz to'langanda fiskal chek alohida chiqariladi.
            var fiscalTracked = method != PaymentMethod.Debt;

            var fiscal = fiscalTracked ? salesOfMethod.Where(IsFiscal).ToList() : new List<Sale>();
            var nonFiscal = fiscalTracked ? salesOfMethod.Where(s => !IsFiscal(s)).ToList() : new List<Sale>();

            lines.Add(new ShiftPaymentLineDto
            {
                Method = method.ToString(),
                SaleCount = salesOfMethod.Count,
                SaleAmount = salesOfMethod.Sum(s => s.TotalAmount),
                RefundCount = refundsOfMethod.Count,
                RefundAmount = refundsOfMethod.Sum(s => s.TotalAmount),
                FiscalCount = fiscal.Count,
                FiscalAmount = fiscal.Sum(s => s.TotalAmount),
                NonFiscalCount = nonFiscal.Count,
                NonFiscalAmount = nonFiscal.Sum(s => s.TotalAmount)
            });
        }

        return lines;
    }

    private static bool IsFiscal(Sale sale) =>
        sale.Receipt is not null && !string.IsNullOrWhiteSpace(sale.Receipt.QrCodeUrl);
}
