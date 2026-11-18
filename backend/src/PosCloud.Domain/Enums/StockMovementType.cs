namespace PosCloud.Domain.Enums;

/// <summary>Ombor harakati turi — qoldiq nima sababdan o'zgargani.</summary>
public enum StockMovementType
{
    /// <summary>Mahsulot yaratilganda (yoki import) kiritilgan boshlang'ich qoldiq.</summary>
    Initial = 0,
    /// <summary>Tovar kirimi (накладная).</summary>
    Receipt = 1,
    /// <summary>Sotuv (kassa yoki online buyurtma).</summary>
    Sale = 2,
    /// <summary>Mijoz qaytargan tovar (qaytarish cheki).</summary>
    Refund = 3,
    /// <summary>Hisobdan chiqarish (списание).</summary>
    WriteOff = 4,
    /// <summary>Boshqa filialga jo'natildi.</summary>
    TransferOut = 5,
    /// <summary>Boshqa filialdan qabul qilindi.</summary>
    TransferIn = 6,
    /// <summary>Jo'natilgan transfer bekor qilindi — tovar manbaga qaytdi.</summary>
    TransferCancel = 7,
    /// <summary>Inventarizatsiya farqi.</summary>
    Inventory = 8,
    /// <summary>Kirim bekor qilindi (storno).</summary>
    ReceiptCancel = 9,
    /// <summary>Hisobdan chiqarish bekor qilindi (storno).</summary>
    WriteOffCancel = 10,
    /// <summary>Online buyurtmada eshik oldida rad etilgan tovar do'konga qaytdi.</summary>
    OnlineReturn = 11
}

/// <summary>Ombor hujjati holati (kirim, hisobdan chiqarish).</summary>
public enum StockDocumentStatus
{
    Active = 0,
    Cancelled = 1
}
