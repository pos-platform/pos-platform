using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PosPlatform.Desktop.Services;

/// <summary>
/// "Eslab qolish" uchun login ma'lumotlari. Parol ochiq matnda saqlanmaydi —
/// Windows DPAPI (CurrentUser) bilan shifrlanadi: faqat shu kompyuterdagi shu
/// Windows foydalanuvchisi ochib o'qiy oladi.
/// </summary>
public static class CredentialStore
{
    private sealed record Stored(string Username, string PasswordEnc);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    // DPAPI'ga qo'shimcha "tuz" — boshqa ilova shu foydalanuvchi nomidan blob'ni ochmasin.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PosPlatform.Desktop.Login.v1");

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PosPlatform");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "credentials.json");
        }
    }

    public static void Save(string username, string password)
    {
        var enc = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(password), Entropy, DataProtectionScope.CurrentUser);
        var stored = new Stored(username, Convert.ToBase64String(enc));
        File.WriteAllText(FilePath, JsonSerializer.Serialize(stored, Json));
    }

    /// <summary>Saqlangan login/parol; yo'q yoki ochib bo'lmasa null.</summary>
    public static (string Username, string Password)? TryLoad()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var stored = JsonSerializer.Deserialize<Stored>(File.ReadAllText(FilePath), Json);
            if (stored is null || string.IsNullOrEmpty(stored.PasswordEnc)) return null;
            var raw = ProtectedData.Unprotect(
                Convert.FromBase64String(stored.PasswordEnc), Entropy, DataProtectionScope.CurrentUser);
            return (stored.Username, Encoding.UTF8.GetString(raw));
        }
        catch
        {
            // Fayl buzilgan yoki boshqa foydalanuvchidan ko'chirilgan — e'tiborsiz qoldiramiz.
            return null;
        }
    }

    public static void Clear()
    {
        try { File.Delete(FilePath); } catch { /* fayl band bo'lsa keyingi Save ustidan yozadi */ }
    }
}
