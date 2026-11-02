using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperClient.Models.DTO.SimpleDto;
using HelperClient.Models.Profile;
using HelperClient.Services;
using HelperClient.Services.Network;

namespace HelperClient.ViewModels
{
    /// <summary>
    /// Страница профиля машины: ID отделения, ID корпуса, номер кабинета,
    /// список IP-адресов (со всех адаптеров) и MAC-адрес.
    /// Сохранение — в локальный файл конфигурации + актуализация на сервере через /Machine/Sync.
    /// </summary>
    public partial class ProfileViewModel : ViewModelBase
    {
        [ObservableProperty]
        private IdNameResponseDto? _selectedHousing;

        [ObservableProperty]
        private IdNameResponseDto? _selectedDepartment;

        [ObservableProperty]
        private string _officeNumber = string.Empty;

        [ObservableProperty]
        private string _macAddress = string.Empty;

        [ObservableProperty]
        private string _ipAddressesText = string.Empty;

        [ObservableProperty]
        private bool _isReferencesLoading;

        [ObservableProperty]
        private string _referencesLoadError = string.Empty;

        public List<IdNameResponseDto> Housings { get; } = new();

        public List<IdNameResponseDto> Departments { get; } = new();

        /// <summary>Путь к локальному файлу конфигурации профиля.</summary>
        public string ProfileFilePath => Services.ProfileStore.FilePath;

        public ProfileViewModel(ClientServiceRegistry services) : base(services)
        {
            LoadFromProfile(Services.Profile);
            Services.SyncService.StatusChanged += (_, message) => ShowStatus(message);
            _ = LoadReferencesAsync();
        }

        private void LoadFromProfile(MachineProfile profile)
        {
            MacAddress = profile.MacAddress;
            OfficeNumber = profile.OfficeNumber ?? string.Empty;
            IpAddressesText = string.Join(Environment.NewLine, profile.IpAddresses);
            SelectedHousing = Housings.FirstOrDefault(h => h.Id == profile.HousingId);
            SelectedDepartment = Departments.FirstOrDefault(d => d.Id == profile.DepartmentId);
        }

        private async Task LoadReferencesAsync()
        {
            if (IsReferencesLoading) return;
            IsReferencesLoading = true;
            ReferencesLoadError = string.Empty;
            try
            {
                // Справочники доступны без авторизации (GET /Department/GetAll, GET /Housing/GetAll).
                var housings = await Services.GetHousingsAsync().ConfigureAwait(false);
                var departments = await Services.GetDepartmentsAsync().ConfigureAwait(false);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Housings.Clear();
                    Housings.AddRange(housings.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase));
                    Departments.Clear();
                    Departments.AddRange(departments.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase));
                    LoadFromProfile(Services.Profile);
                });

                if (housings.Count == 0 || departments.Count == 0)
                    ReferencesLoadError = "Справочники корпусов/отделений пусты — проверьте доступность сервера";
            }
            catch (Exception ex)
            {
                ReferencesLoadError = $"Не удалось загрузить справочники: {ex.Message}";
            }
            finally
            {
                IsReferencesLoading = false;
            }
        }

        [RelayCommand]
        private Task ReloadReferencesAsync() => LoadReferencesAsync();

        /// <summary>Проверка актуальности IP-адресов со всех сетевых адаптеров.</summary>
        [RelayCommand]
        private async Task RefreshNetworkAsync()
        {
            await RunAsync(async () =>
            {
                var current = await Services.RefreshIpAddressesAsync().ConfigureAwait(false);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IpAddressesText = string.Join(Environment.NewLine, current);
                    MacAddress = Services.Profile.MacAddress;
                });
                ShowStatus(current.Count > 0
                    ? $"Проверено адаптеров: IP-адресов — {current.Count}"
                    : "IP-адреса на активных адаптерах не найдены");
            });
        }

        /// <summary>Сохранить профиль в локальный конфиг и актуализировать данные на сервере.</summary>
        [RelayCommand]
        private Task SaveAsync()
        {
            if (SelectedHousing is null)
            {
                ShowStatus("Выберите корпус");
                return Task.CompletedTask;
            }

            if (SelectedDepartment is null)
            {
                ShowStatus("Выберите отделение");
                return Task.CompletedTask;
            }

            var office = OfficeNumber?.Trim();
            if (office is { Length: > 0 } && string.IsNullOrWhiteSpace(office))
                office = null;

            var profile = new MachineProfile
            {
                DepartmentId = SelectedDepartment.Id,
                HousingId = SelectedHousing.Id,
                OfficeNumber = string.IsNullOrWhiteSpace(office) ? null : office,
                MachineType = Services.Profile.MachineType > 0
                    ? Services.Profile.MachineType
                    : Services.Settings.DefaultMachineType,
                MacAddress = NetworkAddressValidator.NormalizeMac(MacAddress),
                // IP/MAC всегда перечитываются с ОС внутри реестра сервисов.
                IpAddresses = ParseIpList(IpAddressesText),
                ServerMachineId = Services.Profile.ServerMachineId,
                ConcurrencyStamp = Services.Profile.ConcurrencyStamp,
            };

            return RunAsync(async () =>
            {
                var (synced, error) = await Services.SaveProfileAndSyncAsync(profile).ConfigureAwait(false);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MacAddress = Services.Profile.MacAddress;
                    IpAddressesText = string.Join(Environment.NewLine, Services.Profile.IpAddresses);
                });

                ShowStatus(synced
                    ? "Профиль сохранён и актуализирован на сервере"
                    : error ?? "Профиль сохранён локально");
            });
        }

        private static List<string> ParseIpList(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return text
                .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(NetworkAddressValidator.IsValidIp)
                .Distinct()
                .ToList();
        }
    }
}
