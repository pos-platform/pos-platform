namespace PosCloud.Application.Common;

/// <summary>
/// Biznes vaqti — Toshkent (UTC+5, yozgi vaqt yo'q). Bazada vaqtlar UTC'da saqlanadi,
/// lekin "kun" (hisobot, dashboard, chek raqami) mahalliy kun bo'yicha hisoblanadi:
/// aks holda 00:00–05:00 oralig'idagi savdolar oldingi kunga tushib qoladi.
/// </summary>
public static class BusinessTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(5);

    /// <summary>Hozirgi mahalliy sana (Toshkent).</summary>
    public static DateTime TodayLocal => (DateTime.UtcNow + Offset).Date;

    /// <summary>UTC vaqtning mahalliy sanasi.</summary>
    public static DateTime LocalDate(DateTime utc) => (utc + Offset).Date;

    /// <summary>Mahalliy kun boshlanishining UTC vaqti (sana qismi olinadi).</summary>
    public static DateTime LocalDayStartUtc(DateTime localDate) =>
        DateTime.SpecifyKind(localDate.Date - Offset, DateTimeKind.Utc);

    /// <summary>
    /// [from, to] mahalliy sanalar oralig'ini (ikkalasi ham kiradi) UTC yarim-ochiq
    /// oraliqqa aylantiradi: [fromUtc, toUtcExclusive).
    /// </summary>
    public static (DateTime FromUtc, DateTime ToUtcExclusive) LocalRangeToUtc(DateTime fromLocal, DateTime toLocal) =>
        (LocalDayStartUtc(fromLocal), LocalDayStartUtc(toLocal.Date.AddDays(1)));
}
