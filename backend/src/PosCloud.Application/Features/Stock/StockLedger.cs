using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Entities;
using PosCloud.Domain.Enums;

namespace PosCloud.Application.Features.Stock;

/// <summary>Harakat qaysi hujjat asosida va kim tomonidan bajarilgani.</summary>
public readonly record struct StockDoc(Guid? Id, string? Number, Guid? UserId);

/// <summary>
/// Filial qoldig'ini o'zgartirishning YAGONA yo'li: qoldiqni o'zgartiradi va ombor harakati
/// jurnaliga (StockMovement) yozuv qo'shadi. Qoldiqni to'g'ridan-to'g'ri o'zgartirmang —
/// aks holda tovar aylanmasi hisoboti va "qoldiq tarixi" haqiqatdan uzilib qoladi.
/// SaveChanges chaqirmaydi.
/// </summary>
public static class StockLedger
{
    public static StockMovement Apply(IApplicationDbContext db, BranchStock stock, decimal delta,
        StockMovementType type, StockDoc doc, decimal? costPrice = null)
    {
        if (stock.ProductId == Guid.Empty || stock.BranchId == Guid.Empty)
            throw new InvalidOperationException("Qoldiq yozuvida filial yoki mahsulot ko'rsatilmagan.");

        stock.Quantity += delta;

        var movement = new StockMovement
        {
            BranchId = stock.BranchId,
            ProductId = stock.ProductId,
            Type = type,
            Quantity = delta,
            BalanceAfter = stock.Quantity,
            CostPrice = costPrice,
            DocumentId = doc.Id,
            DocumentNumber = doc.Number,
            UserId = doc.UserId
        };
        db.StockMovements.Add(movement);
        return movement;
    }

    /// <summary>Filial+mahsulot qoldiq yozuvini lug'atdan oladi yoki yangisini yaratadi (tracked).</summary>
    public static BranchStock GetOrCreate(IApplicationDbContext db, IDictionary<Guid, BranchStock> stocks,
        Guid branchId, Guid productId)
    {
        if (stocks.TryGetValue(productId, out var stock)) return stock;
        stock = new BranchStock { BranchId = branchId, ProductId = productId };
        db.BranchStocks.Add(stock);
        stocks[productId] = stock;
        return stock;
    }

    /// <summary>Sotish/chiqarish mumkin bo'lgan miqdor (online buyurtmalarga band qilingani ayirilgan).</summary>
    public static decimal Available(BranchStock? stock) => stock is null ? 0m : stock.Quantity - stock.Reserved;
}
