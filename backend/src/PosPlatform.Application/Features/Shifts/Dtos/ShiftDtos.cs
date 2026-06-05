using System.ComponentModel.DataAnnotations;
using PosPlatform.Domain.Entities;
using PosPlatform.Domain.Enums;

namespace PosPlatform.Application.Features.Shifts.Dtos;

public class OpenShiftRequest
{
    public Guid? BranchId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OpeningCash { get; set; }
}

public class CloseShiftRequest
{
    /// <summary>Yopishda sanab chiqilgan haqiqiy naqd.</summary>
    [Range(0, double.MaxValue)]
    public decimal CountedCash { get; set; }
}

public class CashShiftDto
{
    public Guid Id { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CashierId { get; set; }
    public string? CashierName { get; set; }
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

    public static CashShiftDto FromEntity(CashShift s, IEnumerable<Sale>? sales = null, string? cashierName = null)
    {
        var dto = new CashShiftDto
        {
            Id = s.Id,
            BranchId = s.BranchId,
            CashierId = s.CashierId,
            CashierName = cashierName,
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
        }

        return dto;
    }
}
