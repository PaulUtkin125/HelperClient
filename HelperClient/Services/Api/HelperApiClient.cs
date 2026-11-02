using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HelperClient.Models.DTO.CategoryDto;
using HelperClient.Models.DTO.MachinDto;
using HelperClient.Models.DTO.RequestDto;
using HelperClient.Models.DTO.SimpleDto;

namespace HelperClient.Services.Api
{
    /// <summary>
    /// Тонкий HTTP-клиент по спецификации /HelperAPI.
    /// Использует ТОЛЬКО эндпоинты, не требующие авторизации:
    ///   GET  /Category/GetAll
    ///   POST /Request/Add
    ///   POST /Machine/Sync
    ///   PATCH /Machine/Heartbeat
    ///   GET  /Department/GetAll, /Housing/GetAll, /Status/GetAll (справочники для профиля и заявки)
    /// Прямые запросы к БД запрещены — обмен только через контракты API.
    /// </summary>
    public interface IHelperApiClient
    {
        Task<IReadOnlyList<CategoryResponseDto>> GetCategoriesAsync(CancellationToken ct = default);

        Task<IReadOnlyList<IdNameResponseDto>> GetDepartmentsAsync(CancellationToken ct = default);

        Task<IReadOnlyList<IdNameResponseDto>> GetHousingsAsync(CancellationToken ct = default);

        Task<IReadOnlyList<IdNameResponseDto>> GetStatusesAsync(CancellationToken ct = default);

        /// <summary>Актуализация данных о машине: нет машины с таким MAC — создаётся новая, иначе обновляется.</summary>
        Task<MachineResponseDto?> SyncMachineAsync(MachineSyncDto dto, CancellationToken ct = default);

        /// <summary>Обновление времени последнего отклика по MAC.</summary>
        Task HeartbeatAsync(string macAddress, CancellationToken ct = default);

        /// <summary>Создание заявки на обслуживание.</summary>
        Task<CreateRequestDto> AddRequestAsync(CreateRequestDto dto, CancellationToken ct = default);
    }

    public sealed class HelperApiClient : IHelperApiClient, IDisposable
    {
        private readonly HttpClient _http;
        private readonly ApiSettings _settings;

        public HelperApiClient(ApiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _http = new HttpClient
            {
                BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + settings.ApiPrefix),
                Timeout = TimeSpan.FromSeconds(30),
            };
            // Клиент анонимный: заголовок Authorization намеренно не отправляется.
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        #region Справочники (публичные)

        public Task<IReadOnlyList<CategoryResponseDto>> GetCategoriesAsync(CancellationToken ct = default) =>
            GetListAsync<CategoryResponseDto>("Category/GetAll", ct);

        public Task<IReadOnlyList<IdNameResponseDto>> GetDepartmentsAsync(CancellationToken ct = default) =>
            GetListAsync<IdNameResponseDto>("Department/GetAll", ct);

        public Task<IReadOnlyList<IdNameResponseDto>> GetHousingsAsync(CancellationToken ct = default) =>
            GetListAsync<IdNameResponseDto>("Housing/GetAll", ct);

        public Task<IReadOnlyList<IdNameResponseDto>> GetStatusesAsync(CancellationToken ct = default) =>
            GetListAsync<IdNameResponseDto>("Status/GetAll", ct);

        #endregion

        #region Машина (публичные Sync / Heartbeat)

        public async Task<MachineResponseDto?> SyncMachineAsync(MachineSyncDto dto, CancellationToken ct = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            using var response = await _http.PostAsJsonAsync("Machine/Sync", dto, JsonOptionsProvider.Options, ct)
                .ConfigureAwait(false);
            await ThrowIfProblemAsync(response, "Не удалось актуализировать данные машины на сервере", ct)
                .ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync<MachineResponseDto>(stream, JsonOptionsProvider.Options, ct)
                .ConfigureAwait(false);
        }

        public async Task HeartbeatAsync(string macAddress, CancellationToken ct = default)
        {
            var dto = new MachineHeartbeatDto { MAC_address = macAddress };
            using var message = new HttpRequestMessage(HttpMethod.Patch, "Machine/Heartbeat")
            {
                Content = JsonContent.Create(dto, options: JsonOptionsProvider.Options),
            };
            using var response = await _http.SendAsync(message, ct).ConfigureAwait(false);
            await ThrowIfProblemAsync(response, "Сервер не принял сигнал активности машины", ct).ConfigureAwait(false);
        }

        #endregion

        #region Заявки (публичный Add)

        public async Task<CreateRequestDto> AddRequestAsync(CreateRequestDto dto, CancellationToken ct = default)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            using var response = await _http.PostAsJsonAsync("Request/Add", dto, JsonOptionsProvider.Options, ct)
                .ConfigureAwait(false);
            await ThrowIfProblemAsync(response, "Не удалось создать заявку", ct).ConfigureAwait(false);

            // Сервер может вернуть как тело созданной заявки, так и пустой ответ.
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var created = await JsonSerializer
                .DeserializeAsync<CreateRequestDto>(stream, JsonOptionsProvider.Options, ct)
                .ConfigureAwait(false);
            return created ?? dto;
        }

        #endregion

        private async Task<IReadOnlyList<T>> GetListAsync<T>(string route, CancellationToken ct)
        {
            try
            {
                var list = await _http.GetFromJsonAsync<List<T>>(route, JsonOptionsProvider.Options, ct)
                    .ConfigureAwait(false);
                return list ?? new List<T>();
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException(0, "NETWORK_ERROR", $"Сервер недоступен: {ex.Message}");
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new ApiException(0, "TIMEOUT", "Превышено время ожидания ответа сервера");
            }
            catch (JsonException ex)
            {
                throw new ApiException(0, "BAD_RESPONSE", $"Сервер вернул некорректный JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Разбор ошибок в формате RFC 7807 (Problem Details) согласно спецификации.
        /// </summary>
        private static async Task ThrowIfProblemAsync(HttpResponseMessage response, string fallbackMessage,
            CancellationToken ct)
        {
            if (response.IsSuccessStatusCode) return;

            ProblemDetailsDto? problem = null;
            string? body = null;
            try
            {
                body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(body))
                    problem = JsonSerializer.Deserialize<ProblemDetailsDto>(body, JsonOptionsProvider.Options);
            }
            catch (JsonException)
            {
                // Тело не является ProblemDetails — используем запасное сообщение.
            }

            var status = (int)response.StatusCode;
            var code = problem?.Code ?? response.StatusCode.ToString();
            var message = !string.IsNullOrWhiteSpace(problem?.Message)
                ? problem!.Message!
                : $"{fallbackMessage} (HTTP {(int)response.StatusCode} {response.StatusCode})";

            throw new ApiException(status, code, message, problem);
        }

        public void Dispose() => _http.Dispose();
    }
}
