namespace PosCloud.Domain.Enums;

/// <summary>Bonus ball harakati turi.</summary>
public enum LoyaltyTransactionType
{
    /// <summary>Xariddan topilgan ball (cashback).</summary>
    Earn = 0,

    /// <summary>Ball bilan to'lov.</summary>
    Redeem = 1,

    /// <summary>Faolsizlik tufayli muddati o'tgan ball.</summary>
    Expire = 2,

    /// <summary>Xodim tomonidan qo'lda tuzatish.</summary>
    Adjust = 3,

    /// <summary>Ro'yxatdan o'tish bonusi (bir marta).</summary>
    Signup = 4,

    /// <summary>Savdo qaytarilganda ball qaytarib olinishi yoki sarflangan ball qaytarilishi.</summary>
    Refund = 5
}
