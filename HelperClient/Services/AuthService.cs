using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HelperClient.Models.DTO.AuthDto;

namespace HelperClient.Services
{
    /// <summary>
    /// POST /HelperAPI/Authorize/Login — авторизация и получение Bearer-токена.
    /// Токен сохраняется в TokenStorage и автоматически подставляется ApiClient.
    /// </summary>
    public class AuthService
    {
        private readonly ApiClient _api;
        private readonly TokenStorage _tokens;

        public AuthService(ApiClient api, TokenStorage tokens)
        {
            _api = api;
            _tokens = tokens;
        }

        /// <summary>Хеш пароля по умолчанию — SHA-256 (64 hex-символа).</summary>
        public static string HashPassword(string password) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();

        /// <summary>Возвращает true, если пользователь уже имеет сохранённый токен.</summary>
        public bool IsAuthenticated => _tokens.IsAuthenticated;

        public void Logout() => _tokens.Clear();

        /// <summary>
        /// Авторизация. 400 — невалидный формат, 401 — неверные учётные данные.
        /// </summary>
        public async Task<bool> LoginAsync(string loginName, string password)
        {
            Validation.EnsureRequiredText(loginName, "LoginName");

            var payload = new LoginDto
            {
                LoginName = loginName.Trim(),
                PasswordHash = HashPassword(password)
            };

            var response = await _api.PostAsync<LoginResponseDto>("/Authorize/Login", payload);
            if (string.IsNullOrWhiteSpace(response?.Token))
                return false;

            // по умолчанию считаем токен годным сутки — сервер при истечении вернёт 401
            var lifetime = response.ExpiresAt is { } expires ? expires - DateTime.UtcNow : TimeSpan.FromHours(24);
            if (lifetime <= TimeSpan.Zero) lifetime = TimeSpan.FromHours(24);

            _tokens.Save(response.Token, lifetime);
            return true;
        }
    }
}
