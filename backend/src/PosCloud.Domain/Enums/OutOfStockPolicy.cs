namespace PosCloud.Domain.Enums;

/// <summary>Mahsulot tugab qolsa nima qilinadi (mijoz tanlaydi).</summary>
public enum OutOfStockPolicy
{
    CallMe = 0,
    ReplaceSimilar = 1,
    Remove = 2
}
