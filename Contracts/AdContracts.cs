using System.ComponentModel.DataAnnotations;

namespace Practice6.Contracts;

public sealed class AdWriteDto
{
    [Required(ErrorMessage = "Заголовок обязателен")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Заголовок должен содержать от 2 до 150 символов")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Текст обязателен")]
    [StringLength(5000, ErrorMessage = "Текст не должен превышать 5000 символов")]
    public string Text { get; set; } = string.Empty;
}

public sealed record AdReadDto(int Id, int UserId, string Title, string Text, bool IsHidden);
