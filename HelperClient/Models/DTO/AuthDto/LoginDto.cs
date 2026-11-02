using System.ComponentModel.DataAnnotations;

namespace HelperClient.Models.DTO.AuthDto
{
    /// <summary>Payload для POST /HelperAPI/Authorize/Login.</summary>
    public record LoginDto
    {
        [Required]
        [StringLength(255)]
        public string LoginName { get; init; } = null!;

        /// <summary>Хеш пароля, вычисленный на клиенте (например SHA-256).</summary>
        [Required]
        public string PasswordHash { get; init; } = null!;
    }

    /// <summary>Ответ авторизации — токен для заголовка Authorization: Bearer.</summary>
    public record LoginResponseDto
    {
        [Required]
        public string Token { get; init; } = null!;

        /// <summary>Ориентировочное время жизни токена (если сервер его сообщает).</summary>
        public System.DateTime? ExpiresAt { get; init; }
    }
}
