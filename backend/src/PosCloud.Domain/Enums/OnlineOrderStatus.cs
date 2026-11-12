namespace PosCloud.Domain.Enums;

/// <summary>Online buyurtma holati (mobil ilova / sayt).</summary>
public enum OnlineOrderStatus
{
    New = 0,
    Accepted = 1,
    Picking = 2,
    Ready = 3,
    AwaitingTransfer = 4,
    OnTheWay = 5,
    ReadyForPickup = 6,
    Delivered = 7,
    Rejected = 8,
    Cancelled = 9
}
