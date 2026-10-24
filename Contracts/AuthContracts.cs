using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Practice6.Contracts;

public sealed class RegisterDto : IValidatableObject
{
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    [StringLength(254, ErrorMessage = "Email слишком длинный")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(8, ErrorMessage = "Пароль должен содержать не менее 8 символов")]
    public string Password { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Encoding.UTF8.GetByteCount(Password ?? string.Empty) > 72)
            yield return new ValidationResult("Пароль не должен превышать 72 байта UTF-8", [nameof(Password)]);
    }
}

public sealed class LoginDto
{
    [Required(ErrorMessage = "Email обязателен")]
    [StringLength(254, ErrorMessage = "Email слишком длинный")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(200, ErrorMessage = "Пароль слишком длинный")]
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshDto
{
    [Required(ErrorMessage = "Refresh-токен обязателен")]
    [StringLength(200, ErrorMessage = "Refresh-токен слишком длинный")]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed record TokenDto(string AccessToken, string RefreshToken, DateTime ExpiresAt);
