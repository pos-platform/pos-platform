using System.IO;
using Microsoft.Win32;

namespace PosCloud.Desktop.Services;

/// <summary>
/// Kassa kompyuterining barqaror barmoq izi. Login shu ID ga bog'lanadi — bir xil login/parol
/// bilan boshqa kompyuterdan kirib bo'lmaydi (backend AuthService tekshiradi).
/// </summary>
public static class DeviceIdentity
{
    private const string MachineGuidPath = @"SOFTWARE\Microsoft\Cryptography";

    private static string? _cached;

    /// <summary>Kompyuter nomi — admin panelda "qaysi qurilma" ko'rinishi uchun.</summary>
    public static string MachineName => Environment.MachineName;

    /// <summary>
    /// Qurilma ID. Asosiy manba — Windows o'rnatilishida yaratiladigan MachineGuid
    /// (Windows qayta o'rnatilmaguncha o'zgarmaydi). O'qib bo'lmasa — %LOCALAPPDATA%\PosPlatform\
    /// device-id.txt fayliga bir marta GUID yozib, keyin o'shani ishlatamiz.
    /// </summary>
    public static string Get()
    {
        if (_cached is not null) return _cached;

        _cached = ReadMachineGuid() ?? ReadOrCreateFallback();
        return _cached;
    }

    private static string? ReadMachineGuid()
    {
        try
        {
            // 64-bitli registr ko'rinishi — ilova 32-bit bo'lib qolsa ham to'g'ri kalitni o'qiydi.
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(MachineGuidPath);
            var value = key?.GetValue("MachineGuid") as string;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static string ReadOrCreateFallback()
    {
        // Fayl dastur papkasidan tashqarida: Velopack har yangilanishda dastur papkasini almashtiradi.
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosPlatform");
        var path = Path.Combine(dir, "device-id.txt");
        // Eski o'rnatishlarda fayl exe yonida yotardi — bir marta ko'chirib olamiz (ID o'zgarmasin).
        var legacyPath = Path.Combine(AppContext.BaseDirectory, "device-id.txt");
        try
        {
            foreach (var candidate in new[] { path, legacyPath })
            {
                if (!File.Exists(candidate)) continue;
                var saved = File.ReadAllText(candidate).Trim();
                if (string.IsNullOrWhiteSpace(saved)) continue;
                if (candidate != path)
                {
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(path, saved);
                }
                return saved;
            }

            var generated = Guid.NewGuid().ToString();
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, generated);
            return generated;
        }
        catch
        {
            // Fayl ham yozilmasa — hech bo'lmasa kompyuter nomiga tayanamiz.
            return "PC-" + Environment.MachineName;
        }
    }
}
