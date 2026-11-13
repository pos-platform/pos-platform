using System.Linq.Expressions;
using System.Text;

namespace PosCloud.Application.Features.ShopCatalog;

/// <summary>
/// Ilova qidiruvi: lotin↔kirill transliteratsiyasi, o'/g' apostrof variantlari va so'zlar bo'yicha (har so'z nomda bo'lishi kerak).
/// Nom ham, so'rov ham bir xil normallashtiriladi: kichik harf, apostroflar olib tashlanadi, ў→о, ғ→г, қ→к, ҳ→х, ё/э→е.
/// Bazada nom PostgreSQL replace() zanjiri bilan normallashtiriladi (EF string.Replace'ni tarjima qiladi).
/// </summary>
public static class ShopSearch
{
    public const int MaxWords = 5;
    private const int MaxVariantsPerWord = 6;

    private static readonly string[] Apostrophes = ["'", "ʻ", "‘", "’", "`", "ʼ", "´"];

    private static readonly (string From, string To)[] CyrillicFold =
        [("ў", "о"), ("ғ", "г"), ("қ", "к"), ("ҳ", "х"), ("ё", "е"), ("э", "е"), ("ъ", ""), ("ь", "")];

    // Kirill → lotin (normallashtirilgan kirilldan). Apostroflar yo'q, chunki o'/g' allaqachon o/g ga tushgan.
    private static readonly Dictionary<char, string> CyrToLat = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ж'] = "j", ['з'] = "z",
        ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n", ['о'] = "o", ['п'] = "p",
        ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u", ['ф'] = "f", ['х'] = "x", ['ц'] = "ts", ['ч'] = "ch",
        ['ш'] = "sh", ['щ'] = "sh", ['ы'] = "i", ['ю'] = "yu", ['я'] = "ya"
    };

    // Lotin → kirill: avval ikki harfli birikmalar (ko'p variantli), keyin bitta harflar.
    private static readonly (string Lat, string[] Cyr)[] LatDigraphs =
    [
        ("sh", ["ш"]), ("ch", ["ч"]), ("yo", ["е", "йо"]), ("yu", ["ю", "йу"]), ("ya", ["я", "йа"]),
        ("ye", ["е", "йе"]), ("ts", ["ц", "тс"])
    ];

    private static readonly Dictionary<char, string> LatToCyr = new()
    {
        ['a'] = "а", ['b'] = "б", ['c'] = "к", ['d'] = "д", ['e'] = "е", ['f'] = "ф", ['g'] = "г", ['h'] = "х",
        ['i'] = "и", ['j'] = "ж", ['k'] = "к", ['l'] = "л", ['m'] = "м", ['n'] = "н", ['o'] = "о", ['p'] = "п",
        ['q'] = "к", ['r'] = "р", ['s'] = "с", ['t'] = "т", ['u'] = "у", ['v'] = "в", ['w'] = "в", ['x'] = "х",
        ['y'] = "й", ['z'] = "з"
    };

    /// <summary>Matnni normallashtiradi (bazadagi <see cref="NormalizedName{T}"/> bilan bir xil qoidalar).</summary>
    public static string Normalize(string text)
    {
        var s = text.Trim().ToLowerInvariant();
        foreach (var a in Apostrophes) s = s.Replace(a, string.Empty);
        foreach (var (from, to) in CyrillicFold) s = s.Replace(from, to);
        return s;
    }

    /// <summary>So'rovni so'zlarga bo'lib, har so'z uchun variantlar (o'zi, kirill/lotin shakli).</summary>
    public static List<List<string>> Terms(string? query)
    {
        var result = new List<List<string>>();
        if (string.IsNullOrWhiteSpace(query)) return result;

        var words = Normalize(query)
            .Split([' ', ',', '.', ';', '-', '/', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Take(MaxWords);

        foreach (var w in words)
        {
            var variants = new List<string> { w };
            if (w.Any(IsCyrillic))
                variants.Add(ToLatin(w));
            if (w.Any(c => c is >= 'a' and <= 'z'))
                variants.AddRange(ToCyrillic(w));
            result.Add(variants.Where(v => v.Length > 0).Distinct().Take(MaxVariantsPerWord).ToList());
        }
        return result.Where(v => v.Count > 0).ToList();
    }

    private static bool IsCyrillic(char c) => c is >= 'а' and <= 'я' or 'ё' or 'ў' or 'қ' or 'ғ' or 'ҳ';

    private static string ToLatin(string w)
    {
        var sb = new StringBuilder(w.Length + 4);
        foreach (var c in w)
            sb.Append(CyrToLat.TryGetValue(c, out var l) ? l : c.ToString());
        return sb.ToString();
    }

    /// <summary>Lotin → kirill variantlari (ikki ma'noli birikmalar uchun bir nechta).</summary>
    private static List<string> ToCyrillic(string w)
    {
        var results = new List<string> { string.Empty };
        var i = 0;
        while (i < w.Length)
        {
            string[] options;
            var dig = i + 1 < w.Length ? LatDigraphs.FirstOrDefault(d => d.Lat == w.Substring(i, 2)) : default;
            if (dig.Lat is not null)
            {
                options = dig.Cyr;
                i += 2;
            }
            else
            {
                options = [LatToCyr.TryGetValue(w[i], out var c) ? c : w[i].ToString()];
                i++;
            }

            results = results.SelectMany(r => options.Select(o => r + o)).Take(MaxVariantsPerWord).ToList();
        }
        return results;
    }

    /// <summary>Bazada normallashtirilgan nom ifodasi: lower + apostroflarni olib tashlash + kirill harflarini birlashtirish.</summary>
    public static Expression<Func<T, string>> NormalizedName<T>(Expression<Func<T, string>> name)
    {
        Expression body = Expression.Call(name.Body, typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!);
        var replace = typeof(string).GetMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;
        foreach (var a in Apostrophes)
            body = Expression.Call(body, replace, Expression.Constant(a), Expression.Constant(string.Empty));
        foreach (var (from, to) in CyrillicFold)
            body = Expression.Call(body, replace, Expression.Constant(from), Expression.Constant(to));
        return Expression.Lambda<Func<T, string>>(body, name.Parameters);
    }

    /// <summary>
    /// Har so'z (variantlaridan biri) normallashtirilgan nomda bo'lishi kerak: (v1 OR v2 ...) AND (...).
    /// Qo'shimcha <paramref name="orElse"/> (masalan aniq shtrix-kod) — butun shartga OR bilan qo'shiladi.
    /// </summary>
    public static Expression<Func<T, bool>> Match<T>(Expression<Func<T, string>> name, List<List<string>> terms,
        Expression<Func<T, bool>>? orElse = null)
    {
        var normalized = NormalizedName(name);
        var param = normalized.Parameters[0];
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        Expression? all = null;
        foreach (var variants in terms)
        {
            Expression? any = null;
            foreach (var v in variants)
            {
                var call = Expression.Call(normalized.Body, contains, Expression.Constant(v));
                any = any is null ? call : Expression.OrElse(any, call);
            }
            if (any is not null) all = all is null ? any : Expression.AndAlso(all, any);
        }
        all ??= Expression.Constant(true);

        if (orElse is not null)
        {
            var other = new ParameterReplacer(orElse.Parameters[0], param).Visit(orElse.Body);
            all = Expression.OrElse(all, other);
        }
        return Expression.Lambda<Func<T, bool>>(all, param);
    }

    private sealed class ParameterReplacer(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
