namespace PosCloud.Domain.Enums;

// Bosqich 10: online qaytarish, xatoliklar, kuryer taklifi.

/// <summary>Online qaytarish so'rovi holati.</summary>
public enum OnlineReturnStatus
{
    Requested = 0,
    Approved = 1,
    PickupAssigned = 2,
    PickedUp = 3,
    Received = 4,
    Refunded = 5,
    Rejected = 6,
    Cancelled = 7
}

/// <summary>Tovar do'konga qanday qaytadi.</summary>
public enum OnlineReturnMethod
{
    CourierPickup = 0,
    BringToStore = 1
}

/// <summary>Qaytarish sababi.</summary>
public enum OnlineReturnReason
{
    Damaged = 0,
    WrongItem = 1,
    Quality = 2,
    Expired = 3,
    Missing = 4,
    Other = 5
}

/// <summary>Pul qanday qaytariladi: naqd (do'konda), o'tkazma yoki bonus ball.</summary>
public enum ReturnRefundMethod
{
    Cash = 0,
    Transfer = 1,
    Bonus = 2
}

/// <summary>Xatolik manbai.</summary>
public enum SystemErrorSource
{
    Api = 0,
    Background = 1,
    Fiscal = 2,
    Desktop = 3,
    Mobile = 4,
    Agent = 5
}

/// <summary>Xatolik darajasi.</summary>
public enum SystemErrorLevel
{
    Warning = 0,
    Error = 1,
    Critical = 2
}

/// <summary>Kuryerga taklif yuborish rejimi.</summary>
public enum CourierOfferMode
{
    Off = 0,
    Broadcast = 1
}

/// <summary>Bitta kuryerga yuborilgan taklif holati.</summary>
public enum CourierOfferItemStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Expired = 3,
    Cancelled = 4
}

/// <summary>Buyurtma bo'yicha umumiy kuryer taklifi holati (xodimga ko'rsatiladi).</summary>
public enum CourierOfferStatus
{
    None = 0,
    Pending = 1,
    Accepted = 2,
    NoCourier = 3
}
