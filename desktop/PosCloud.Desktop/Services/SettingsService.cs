using System.IO;
using System.Text.Json;
using PosCloud.Desktop.Models;

namespace PosCloud.Desktop.Services;

/// <summary>Foydalanuvchi sozlamalarini %APPDATA%\PosPlatform\settings.json da saqlaydi.</summary>
public static class SettingsService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static string FilePath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PosPlatform"); // eski papka nomi ataylab saqlangan (mavjud ma'lumotlar yo'qolmasin)
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    public static Settings? Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), Json);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(Settings settings)
        => File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Json));
}
