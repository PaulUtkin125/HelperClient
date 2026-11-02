using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HelperClient.Models.DTO.RequestDto;
using HelperClient.Models.Profile;
using HelperClient.Services.Api;
using HelperClient.Services.Background;
using HelperClient.Services.Network;
using HelperClient.Services.Profile;

namespace HelperClient.Services
{
    /// <summary>
    /// Точка входа для ViewModel: композиция сервисов клиента + бизнес-операции,
    /// разрешённые спецификацией тонкого клиента.
    /// </summary>
    public sealed class ClientServiceRegistry : IAsyncDisposable
    {
        private const string SettingsFileName = "appsettings.json";

        public ApiSettings Settings { get; }

        public IProfileStore ProfileStore { get; }

        public INetworkChangeNotifier NetworkNotifier { get; }

        public IHelperApiClient Api { get; }

        public IMachineSyncService SyncService { get; }

        /// <summary>Последние загруженные справочники отделений (кэш для UI).</summary>
        public IReadOnlyList<Models.DTO.SimpleDto.IdNameResponseDto> DepartmentsCache { get; private set; } =
            new List<Models.DTO.SimpleDto.IdNameResponseDto>();

        /// <summary>Последние загруженные справочники корпусов (кэш для UI).</summary>
        public IReadOnlyList<Models.DTO.SimpleDto.IdNameResponseDto> HousingsCache { get; private set; } =
            new List<Models.DTO.SimpleDto.IdNameResponseDto>();

        public MachineProfile Profile => SyncService.Profile;

        public event EventHandler<string>? StatusChanged;

        private ClientServiceRegistry(
            ApiSettings settings,
            IProfileStore store,
            INetworkChangeNotifier notifier,
            IHelperApiClient api,
            IMachineSyncService sync)
        {
            Settings = settings;
            ProfileStore = store;
            NetworkNotifier = notifier;
            Api = api;
            SyncService = sync;
            sync.StatusChanged += (_, message) => StatusChanged?.Invoke(this, message);
        }

        /// <summary>Загрузить настройки и собрать все сервисы.</summary>
        public static async Task<ClientServiceRegistry> CreateAsync()
        {
            var settings = LoadSettings();
            var store = new JsonProfileStore();
            var notifier = new NetworkChangeNotifier();
            var api = new HelperApiClient(settings);
            var sync = new MachineSyncService(api, store, notifier, settings);
            await sync.StartAsync().ConfigureAwait(false);
            return new ClientServiceRegistry(settings, store, notifier, api, sync);
        }

        #region Функционал клиента (по ТЗ)

        /// <summary>1. Список категорий проблем с сохранением вложенной структуры.</summary>
        public async Task<IReadOnlyList<CategoryNode>> GetProblemCategoriesAsync()
        {
            var categories = await Api.GetCategoriesAsync().ConfigureAwait(false);
            return CategoryTreeBuilder.Build(categories);
        }

        /// <summary>Справочник отделений (для заполнения профиля при первом запуске).</summary>
        public async Task<IReadOnlyList<Models.DTO.SimpleDto.IdNameResponseDto>> GetDepartmentsAsync()
        {
            DepartmentsCache = await Api.GetDepartmentsAsync().ConfigureAwait(false);
            return DepartmentsCache;
        }

        /// <summary>Справочник корпусов (для заполнения профиля при первом запуске).</summary>
        public async Task<IReadOnlyList<Models.DTO.SimpleDto.IdNameResponseDto>> GetHousingsAsync()
        {
            HousingsCache = await Api.GetHousingsAsync().ConfigureAwait(false);
            return HousingsCache;
        }

        /// <summary>
        /// 2. Создание профиля машины: валидация, сохранение в локальный файл конфигурации
        /// и актуализация на сервере через /Machine/Sync.
        /// </summary>
        public async Task<(bool Synced, string Error)> SaveProfileAndSyncAsync(MachineProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            // Системные поля всегда перечитываем с ОС — пользователь не должен вводить MAC/IP вручную.
            var snapshot = NetworkInfoProvider.Capture();
            if (string.IsNullOrWhiteSpace(profile.MacAddress) || !NetworkAddressValidator.IsValidMac(profile.MacAddress))
                profile.MacAddress = snapshot.MacAddress;
            if (profile.IpAddresses is null || profile.IpAddresses.Count == 0)
                profile.IpAddresses = snapshot.Ipv4Addresses.ToList();
            if (profile.MachineType <= 0)
                profile.MachineType = Settings.DefaultMachineType;

            // Валидация по ограничениям контракта Machine/Sync из спецификации.
            if (profile.DepartmentId <= 0)
                return (false, "Выберите отделение");
            if (profile.HousingId <= 0)
                return (false, "Выберите корпус");
            if (!NetworkAddressValidator.IsValidMac(profile.MacAddress))
                return (false, "Не удалось определить корректный MAC-адрес машины");
            if (profile.IpAddresses.Count == 0 || profile.IpAddresses.Any(ip => !NetworkAddressValidator.IsValidIp(ip)))
                return (false, "Не найдено ни одного корректного IP-адреса на сетевых адаптерах");
            if (profile.OfficeNumber is { Length: > 255 })
                return (false, "Номер кабинета не должен превышать 255 символов");

            await ProfileStore.SaveAsync(profile).ConfigureAwait(false);
            SyncService.ApplyExternalProfile(profile);

            try
            {
                var synced = await SyncService.SyncNowAsync().ConfigureAwait(false);
                return synced
                    ? (true, null)
                    : (false, "Сервер недоступен: профиль сохранён локально, синхронизация повторится автоматически");
            }
            catch (ApiException ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>3. Проверка актуальности IP-адресов со всех сетевых адаптеров.</summary>
        public async Task<IReadOnlyList<string>> RefreshIpAddressesAsync()
        {
            var current = NetworkInfoProvider.Capture().Ipv4Addresses;
            var known = SyncService.Profile.IpAddresses;
            if (!current.SequenceEqual(known))
            {
                SyncService.Profile.IpAddresses = current.ToList();
                await ProfileStore.SaveAsync(SyncService.Profile).ConfigureAwait(false);
                await SyncService.SyncNowAsync().ConfigureAwait(false);
            }

            return current;
        }

        /// <summary>4. Формирование заявки: категория проблемы + мобильный контактный телефон → Request/Add.</summary>
        public async Task<RequestCreationResult> CreateRequestAsync(int categoryId, string? phone, string? comment = null)
        {
            var machineId = SyncService.Profile.ServerMachineId;
            if (machineId <= 0)
            {
                // Машина ещё не зарегистрирована (сервер был недоступен) — пробуем синхронизироваться сейчас.
                var synced = await SyncService.SyncNowAsync().ConfigureAwait(false);
                machineId = SyncService.Profile.ServerMachineId;
                if (!synced || machineId <= 0)
                    return RequestCreationResult.Failure(
                        "Машина не зарегистрирована на сервере. Проверьте профиль и доступность HelperAPI.");
            }

            var normalizedPhone = PhoneNumberNormalizer.Normalize(phone);
            if (normalizedPhone is null)
                return RequestCreationResult.Failure(
                    PhoneNumberNormalizer.Describe(PhoneNumberNormalizer.Validate(phone)));

            var dto = new CreateRequestDto
            {
                MachineId = machineId,
                // Даты по спецификации — ISO 8601, ответы API всегда UTC.
                Create_DateTime = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                CategoryId = categoryId,
                StatusId = Settings.DefaultRequestStatusId,
                Phone_number = normalizedPhone,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            };

            try
            {
                await Api.AddRequestAsync(dto).ConfigureAwait(false);
                return RequestCreationResult.Success($"Заявка создана (категория {categoryId})");
            }
            catch (ApiException ex)
            {
                return RequestCreationResult.Failure(ex.Message);
            }
        }

        /// <summary>5. Актуализация данных на сервере (ручной запуск).</summary>
        public Task<bool> ForceSyncAsync() => SyncService.SyncNowAsync();

        public Task<bool> SendHeartbeatAsync() => SyncService.HeartbeatNowAsync();

        #endregion

        private static ApiSettings LoadSettings()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, SettingsFileName),
                Path.Combine(JsonProfileStore.GetDefaultConfigDirectory(), SettingsFileName),
            };

            foreach (var path in candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    using var stream = File.OpenRead(path);
                    using var document = JsonDocument.Parse(stream);
                    if (document.RootElement.TryGetProperty("HelperApi", out var section))
                    {
                        var loaded = section.Deserialize<ApiSettings>(new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        });
                        if (loaded is not null && !string.IsNullOrWhiteSpace(loaded.BaseUrl))
                            return loaded;
                    }
                }
                catch (Exception ex) when (ex is JsonException or IOException)
                {
                    // Некорректный конфиг — используем значения по умолчанию.
                }
            }

            var defaults = new ApiSettings();

            // Создаём файл настроек при первом запуске, чтобы администратор мог поправить адрес сервера.
            try
            {
                var target = candidates[0];
                if (!File.Exists(target))
                {
                    var json = JsonSerializer.Serialize(new { HelperApi = defaults },
                        new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(target, json);
                }
            }
            catch (IOException)
            {
                // Нет прав на запись рядом с исполняемым файлом — работаем на дефолтах.
            }

            return defaults;
        }

        public async ValueTask DisposeAsync()
        {
            await SyncService.DisposeAsync().ConfigureAwait(false);
            NetworkNotifier.Dispose();
            if (Api is IDisposable disposable) disposable.Dispose();
        }
    }

    public sealed record RequestCreationResult(bool IsSuccess, string Message)
    {
        public static RequestCreationResult Success(string message) => new(true, message);

        public static RequestCreationResult Failure(string message) => new(false, message);
    }
}
