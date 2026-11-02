using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HelperClient.Models.DTO.Common;

namespace HelperClient.Services
{
    /// <summary>
    /// Низкоуровневый HTTP-клиент для /HelperAPI.
    ///  - сериализует JSON в camelCase (контрактная конвенция ASP.NET Core);
    ///  - подставляет заголовок "Authorization: Bearer &lt;токен&gt;" из TokenStorage;
    ///  - ошибки разбирает по формату RFC 7807 и бросает ApiException;
    ///  - при 401 очищает локальный токен (истёк/невалиден).
    /// </summary>
    public class ApiClient : IDisposable
    {
        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _http;
        private readonly TokenStorage _tokens;
        private readonly string _prefix;

        /// <summary>Событие возникновения 401 — UI может предложить повторный вход.</summary>
        public event Action? Unauthorized;

        public ApiClient(AppConfig config, TokenStorage tokens)
        {
            _tokens = tokens;
            _prefix = config.RoutePrefix.TrimEnd('/');
            _http = new HttpClient
            {
                BaseAddress = new Uri(config.BaseUrl.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
            };
        }

        public TokenStorage Tokens => _tokens;

        private HttpRequestMessage CreateRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, _prefix + path);
            if (_tokens.IsAuthenticated)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.Token);
            return request;
        }

        private static HttpContent? JsonContent(object? payload) =>
            payload is null ? null : new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        private static string Q(string name, string value) =>
            $"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";

        internal static string BuildQuery(params (string Name, object? Value)[] parameters)
        {
            var parts = new List<string>();
            foreach (var (name, value) in parameters)
            {
                if (value is null) continue;
                if (value is string s && string.IsNullOrWhiteSpace(s)) continue;
                if (value is int i && i == 0) continue; // 0 = не задано для опциональных id
                parts.Add(Q(name, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!));
            }
            return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
        }

        /// <summary>Выполняет запрос и возвращает разобранный ответ либо бросает ApiException.</summary>
        private async Task<T> SendAsync<T>(HttpRequestMessage request, HttpContent? content)
        {
            if (content is not null)
                request.Content = content;

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request);
            }
            catch (TaskCanceledException)
            {
                throw new ApiException((int)HttpStatusCode.RequestTimeout, null);
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException(0, new ProblemDetailsDto
                {
                    Status = 0,
                    Code = "NETWORK_ERROR",
                    Message = $"Сервер недоступен: {ex.Message}"
                });
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _tokens.Clear();
                    Unauthorized?.Invoke();
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await SafeReadBody(response);
                    ProblemDetailsDto? problem = null;
                    try
                    {
                        problem = JsonSerializer.Deserialize<ProblemDetailsDto>(body, JsonOptions);
                    }
                    catch
                    {
                        // тело не является Problem Details — формируем синтетическую ошибку
                    }

                    problem ??= new ProblemDetailsDto
                    {
                        Status = (int)response.StatusCode,
                        Message = string.IsNullOrWhiteSpace(body) ? null : Truncate(body)
                    };

                    throw new ApiException((int)response.StatusCode, problem);
                }

                if (typeof(T) == typeof(Unit))
                    return default!;

                if (typeof(T) == typeof(byte[]))
                    return (T)(object)await response.Content.ReadAsByteArrayAsync();

                if (typeof(T) == typeof(string))
                    return (T)(object)await response.Content.ReadAsStringAsync();

                var stream = await response.Content.ReadAsStreamAsync();
                if (stream.Length == 0)
                    return default!;

                var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
                return result!;
            }
        }

        private static async Task<string> SafeReadBody(HttpResponseMessage response)
        {
            try { return await response.Content.ReadAsStringAsync(); }
            catch { return string.Empty; }
        }

        private static string Truncate(string value, int max = 512) =>
            value.Length <= max ? value : value[..max] + "…";

        /// <summary>Маркер «ответ без полезной нагрузки».</summary>
        public readonly struct Unit { }

        // ============================ Публичные helpers ============================

        public Task<List<T>> GetListAsync<T>(string path) =>
            SendAsync<List<T>>(CreateRequest(HttpMethod.Get, path), null);

        public Task<T> GetAsync<T>(string path) =>
            SendAsync<T>(CreateRequest(HttpMethod.Get, path), null);

        public Task PostAsync(string path, object? payload = null) =>
            SendAsync<Unit>(CreateRequest(HttpMethod.Post, path), JsonContent(payload));

        public Task<T> PostAsync<T>(string path, object? payload = null) =>
            SendAsync<T>(CreateRequest(HttpMethod.Post, path), JsonContent(payload));

        public Task PutAsync(string path, object? payload = null) =>
            SendAsync<Unit>(CreateRequest(HttpMethod.Put, path), JsonContent(payload));

        public Task<T> PutAsync<T>(string path, object? payload = null) =>
            SendAsync<T>(CreateRequest(HttpMethod.Put, path), JsonContent(payload));

        public Task PatchAsync(string path, object? payload = null) =>
            SendAsync<Unit>(CreateRequest(new HttpMethod("PATCH"), path), JsonContent(payload));

        public Task DeleteAsync(string path, object? payload = null) =>
            SendAsync<Unit>(CreateRequest(HttpMethod.Delete, path), JsonContent(payload));

        public Task<byte[]> DownloadAsync(string path) =>
            SendAsync<byte[]>(CreateRequest(HttpMethod.Get, path), null);

        public Task PostMultipartAsync(string path, MultipartFormDataContent form) =>
            SendAsync<Unit>(CreateRequest(HttpMethod.Post, path), form);

        public void Dispose()
        {
            _http.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
