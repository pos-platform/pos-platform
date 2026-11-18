using PosCloud.Domain.Entities;

namespace PosCloud.Application.Features.Stock.Dtos;

/// <summary>Tovar aylanmasi hisoboti filtri. Sanalar Toshkent vaqti bo'yicha kun (yyyy-MM-dd).</summary>
public class StockTurnoverQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ProductId { get; set; }
    /// <summary>Nom yoki shtrix-kod bo'yicha qidiruv.</summary>
    public string? Search { get; set; }
    /// <summary>true — davrda harakati bo'lmagan tovarlar ham (qoldig'i bor bo'lsa) chiqadi.</summary>
    public bool IncludeIdle { get; set; }
}

/// <summary>Bir tovar bo'yicha davr aylanmasi: boshlang'ich → kirim / chiqim → oxirgi qoldiq.</summary>
public class StockTurnoverRowDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Unit { get; set; } = string.Empty;

    public decimal Opening { get; set; }

    // Kirim (musbat)
    public decimal Receipt { get; set; }
    public decimal Refund { get; set; }
    public decimal TransferIn { get; set; }
    /// <summary>Boshqa kirimlar: boshlang'ich qoldiq, bekor qilingan chiqim/transfer, online qaytgan tovar.</summary>
    public decimal OtherIn { get; set; }

    // Chiqim (musbat son sifatida ko'rsatiladi)
    public decimal Sale { get; set; }
    public decimal WriteOff { get; set; }
    public decimal TransferOut { get; set; }
    /// <summary>Boshqa chiqimlar: bekor qilingan kirim.</summary>
    public decimal OtherOut { get; set; }

    /// <summary>Inventarizatsiya farqi (+ ortiqcha / − kamomad).</summary>
    public decimal Inventory { get; set; }

    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal Closing { get; set; }
}

public class StockTurnoverReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? BranchId { get; set; }
    public List<StockTurnoverRowDto> Rows { get; set; } = new();
}

/// <summary>Ombor harakati (qoldiq tarixi) qatori.</summary>
public class StockMovementDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal? CostPrice { get; set; }
    public Guid? DocumentId { get; set; }
    public string? DocumentNumber { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }

    public static StockMovementDto FromEntity(StockMovement m, string? userName = null) => new()
    {
        Id = m.Id,
        CreatedAt = m.CreatedAt,
        BranchId = m.BranchId,
        BranchName = m.Branch?.Name,
        ProductId = m.ProductId,
        ProductName = m.Product?.Name,
        Type = m.Type.ToString(),
        Quantity = m.Quantity,
        BalanceAfter = m.BalanceAfter,
        CostPrice = m.CostPrice,
        DocumentId = m.DocumentId,
        DocumentNumber = m.DocumentNumber,
        UserId = m.UserId,
        UserName = userName
    };
}
