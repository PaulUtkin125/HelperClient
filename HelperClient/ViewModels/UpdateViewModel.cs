using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperClient.Models.DTO.VersionDto;
using HelperClient.Services;

namespace HelperClient.ViewModels
{
    /// <summary>Карточка версии ПО для отображения на странице «Обновления».</summary>
    public partial class VersionCardViewModel : ObservableObject
    {
        public VersionResponseDto Version { get; }

        [ObservableProperty]
        private bool _isDownloading;

        public string Title => Version.ProgramName ?? $"Программа #{Version.Program_id}";
        public string ReleaseDate => Version.Release_dateTime.ToString("dd.MM.yyyy HH:mm");
        public string FileSizeText => FormatSize(Version.FileSize);
        public string HashShort => Version.FileHash is { Length: >= 8 } h ? h[..8] + "…" : Version.FileHash;
        public bool IsEndPoint => Version.End_point;

        public VersionCardViewModel(VersionResponseDto version) => Version = version;

        private static string FormatSize(long bytes) => bytes switch
        {
            <= 0 => "—",
            < 1024 => $"{bytes} Б",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} КБ",
            _ => $"{bytes / (1024.0 * 1024.0):F1} МБ"
        };
    }

    /// <summary>
    /// Страница обновлений: GET /Version/GetAll, поиск /Version/Search,
    /// скачивание /Version/Download с проверкой SHA-256 (битый файл → ошибка).
    /// </summary>
    public partial class UpdateViewModel : ObservableObject
    {
        private readonly VersionService _versions;

        [ObservableProperty]
        private ObservableCollection<VersionCardViewModel> _items = new();

        [ObservableProperty]
        private string _searchString = "";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _statusMessage;

        public UpdateViewModel(VersionService versions) => _versions = versions;

        /// <summary>GET /Version/GetAll</summary>
        [RelayCommand]
        public async Task Refresh()
        {
            ErrorMessage = null;
            IsBusy = true;
            try
            {
                var list = await _versions.GetAllAsync();
                Items = new ObservableCollection<VersionCardViewModel>(
                    list.OrderByDescending(v => v.Release_dateTime).Select(v => new VersionCardViewModel(v)));
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>GET /Version/Search?searchString=…</summary>
        [RelayCommand]
        public async Task Search()
        {
            if (string.IsNullOrWhiteSpace(SearchString))
            {
                await Refresh();
                return;
            }

            ErrorMessage = null;
            IsBusy = true;
            try
            {
                var found = await _versions.SearchAsync(SearchString);
                Items = new ObservableCollection<VersionCardViewModel>(
                    found.OrderByDescending(v => v.Release_dateTime).Select(v => new VersionCardViewModel(v)));
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Скачивание файла версии с проверкой хеша.</summary>
        [RelayCommand]
        private async Task Download(VersionCardViewModel? card)
        {
            if (card is null) return;

            card.IsDownloading = true;
            ErrorMessage = null;
            StatusMessage = null;
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HelperClient", "Updates");
                System.IO.Directory.CreateDirectory(dir);
                var target = System.IO.Path.Combine(dir, $"version_{card.Version.Id}.zip");

                await _versions.DownloadAsync(card.Version.Id, target, card.Version.FileHash);
                StatusMessage = $"Файл сохранён: {target}";
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                ErrorMessage = "Запись о версии или файл не найдены.";
            }
            catch (ApiException ex) when (ex.StatusCode == 422)
            {
                ErrorMessage = "Хеш файла не совпадает — возможно, файл повреждён. Повторите попытку.";
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                card.IsDownloading = false;
            }
        }
    }
}
