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
    public ShiftStatus Status { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? CountedCash { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal CashSalesNet { get; set; }
    public decimal CardSalesNet { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal CashDifference { get; set; }

    public static CashShiftDto FromEntity(CashShift s) => new()
    {
        Id = s.Id,
        BranchId = s.BranchId,
        CashierId = s.CashierId,
        Status = s.Status,
        OpenedAt = s.OpenedAt,
        ClosedAt = s.ClosedAt,
        OpeningCash = s.OpeningCash,
        CountedCash = s.CountedCash,
        TotalSales = s.TotalSales,
        TotalRefunds = s.TotalRefunds,
        CashSalesNet = s.CashSalesNet,
        CardSalesNet = s.CardSalesNet,
        ExpectedCash = s.ExpectedCash,
        CashDifference = s.CashDifference
    };
}
