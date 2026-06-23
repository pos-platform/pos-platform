using System.ComponentModel.DataAnnotations;

namespace PosPlatform.Application.Features.Auth.Dtos;

/// <summary>Foydalanuvchi o'z parolini o'zgartiradi (joriy parolni tasdiqlab).</summary>
public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
