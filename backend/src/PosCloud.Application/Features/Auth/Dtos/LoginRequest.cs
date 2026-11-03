using System.ComponentModel.DataAnnotations;

namespace PosCloud.Application.Features.Auth.Dtos;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Kirayotgan kassa stantsiyasi ID (desktop Sozlamalaridagi "Kassa stantsiyasi ID", masalan "KASSA-1").
    /// Foydalanuvchiga kassa biriktirilgan bo'lsa — mos kelmasa kirish rad etiladi.
    /// </summary>
    public string? TerminalId { get; set; }

    /// <summary>
    /// Kassa kompyuterining barqaror barmoq izi (Windows MachineGuid). Birinchi kirishda
    /// loginga bog'lanadi, keyin faqat shu qurilmadan kirish mumkin bo'ladi.
    /// Web (admin panel) uni yubormaydi — bog'lanish faqat Kassir/Menejerga tegishli.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>Kompyuter nomi — admin panelda "qaysi qurilma" ko'rinishi uchun.</summary>
    public string? DeviceName { get; set; }
}
