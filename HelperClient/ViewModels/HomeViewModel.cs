using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperClient.Models.DTO.Common;
using HelperClient.Models.FormModel;
using HelperClient.Services;

namespace HelperClient.ViewModels
{
    /// <summary>
    /// Модель главной страницы: категории проблем берутся из справочника Category,
    /// элементы категорий — из дерева категорий (GET /Category/GetAll + /Category/Search).
    /// Отправка заявки → POST /Request/Add.
    /// </summary>
    public partial class HomeViewModel : ObservableObject
    {
        private readonly ReferenceService _references;
        private readonly RequestService _requests;
        private readonly MachineService _machines;

        /// <summary>Список категорий с чекбоксами (отображается на странице).</summary>
        [ObservableProperty]
        private ObservableCollection<ProblemCategory> _problems = ProblemRepository.GetProblems();

        [ObservableProperty]
        private string _machineIp = string.Empty;

        [ObservableProperty]
        private string _officeNumber = string.Empty;

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _comment = string.Empty;

        [ObservableProperty]
        private bool _isSending;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private string? _errorMessage;

        /// <summary>Пользователь авторизован (токен сохранён) — влияет на доступные действия.</summary>
        [ObservableProperty]
        private bool _isAuthenticated;

        public HomeViewModel(ReferenceService references, RequestService requests, MachineService machines, TokenStorage tokens)
        {
            _references = references;
            _requests = requests;
            _machines = machines;
            IsAuthenticated = tokens.IsAuthenticated;
        }

        partial void OnMachineIpChanged(string value) => ErrorMessage = null;
        partial void OnOfficeNumberChanged(string value) => ErrorMessage = null;
        partial void OnPhoneNumberChanged(string value) => ErrorMessage = null;
        partial void OnCommentChanged(string value) => ErrorMessage = null;

        /// <summary>Загружает категории из серверного справочника Category и строит дерево.</summary>
        [RelayCommand]
        public async Task RefreshCategories()
        {
            ErrorMessage = null;
            try
            {
                var tree = await _references.GetAllAsync("Category");
                Problems = BuildProblems(tree);
            }
            catch (ApiException ex)
            {
                ErrorMessage = $"Не удалось загрузить категории: {ex.Message}";
            }
        }

        /// <summary>Поиск категорий по подстроке (GET /Category/Search).</summary>
        [RelayCommand]
        public async Task SearchCategories(string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
            {
                await RefreshCategories();
                return;
            }

            try
            {
                var found = await _references.SearchAsync("Category", searchString);
                Problems = BuildProblems(found);
            }
            catch (ApiException ex)
            {
                ErrorMessage = $"Ошибка поиска категорий: {ex.Message}";
            }
        }

        private static ObservableCollection<ProblemCategory> BuildProblems(IEnumerable<ReferenceItemDto> categories)
        {
            var collection = new ObservableCollection<ProblemCategory>();
            foreach (var category in categories.OrderBy(c => c.Name))
            {
                // Подэлементы берём из дерева, если сервер его возвращает; иначе категория без пунктов
                var children = category is CategoryTreeResponseDto tree
                    ? tree.Children.Select(c => c.Name)
                    : Array.Empty<string>();
                collection.Add(new ProblemCategory(category.Name, children));
            }
            return collection;
        }

        /// <summary>Выбранные пользователем проблемы (отмеченные чекбоксы).</summary>
        public List<string> GetSelectedProblems() =>
            Problems.Where(c => c.IsChecked || c.Items.Any(i => i.IsChecked))
                    .SelectMany(c => c.IsChecked
                        ? new[] { c.Title }
                        : c.Items.Where(i => i.IsChecked).Select(i => i.Name))
                    .ToList();

        /// <summary>Отправка формы → POST /HelperAPI/Request/Add.</summary>
        [RelayCommand(CanExecute = nameof(CanSend))]
        private async Task SendForm()
        {
            ErrorMessage = null;
            StatusMessage = null;

            var selected = GetSelectedProblems();
            if (selected.Count == 0)
            {
                ErrorMessage = "Не выбрано ни одной проблемы.";
                return;
            }

            // Клиентская валидация по правилам спецификации до отправки
            try
            {
                Validation.EnsureValidIp(MachineIp, "IP-адрес");
                Validation.EnsureOptionalText(OfficeNumber.Length == 0 ? null : OfficeNumber, "Номер кабинета");
                Validation.EnsureOptionalText(PhoneNumber.Length == 0 ? null : PhoneNumber, "Телефон");
                Validation.EnsureOptionalText(Comment.Length == 0 ? null : Comment, "Комментарий");
            }
            catch (ArgumentException aex)
            {
                ErrorMessage = aex.Message;
                return;
            }

            IsSending = true;
            SendFormCommand.NotifyCanExecuteChanged();
            try
            {
                // 1. Находим машину по IP (GET /Machine/Search?IP=...)
                var machines = await _machines.SearchAsync(ip: MachineIp.Trim());
                if (machines.Count == 0)
                {
                    ErrorMessage = $"Устройство с IP «{MachineIp}» не найдено. Проверьте адрес.";
                    return;
                }
                var machine = machines[0];

                // 2. Категория: первая выбранная должна существовать в справочнике
                var categories = await _references.SearchAsync("Category", selected[0]);
                var category = categories.FirstOrDefault(c =>
                    string.Equals(c.Name, selected[0], StringComparison.OrdinalIgnoreCase));
                if (category is null)
                {
                    ErrorMessage = $"Категория «{selected[0]}» не найдена в справочнике.";
                    return;
                }

                // 3. Начальный статус: первый элемент справочника Status
                var statuses = await _references.GetAllAsync("Status");
                if (statuses.Count == 0)
                {
                    ErrorMessage = "Справочник статусов пуст — невозможно создать заявку.";
                    return;
                }
                var initialStatus = statuses[0];

                // 4. Формируем текст заявки: выбранные проблемы + данные формы
                var commentParts = new List<string>();
                if (selected.Count > 1)
                    commentParts.Add("Проблемы: " + string.Join(", ", selected));
                if (!string.IsNullOrWhiteSpace(Comment))
                    commentParts.Add(Comment.Trim());
                var commentText = string.Join(". ", commentParts);
                if (commentText.Length > 255)
                    commentText = commentText[..255];

                // 5. POST /Request/Add (дублирование заявок допускается)
                await _requests.AddNewAsync(
                    machineId: machine.Id,
                    categoryId: category.Id,
                    statusId: initialStatus.Id,
                    comment: commentText.Length == 0 ? null : commentText,
                    phoneNumber: string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim());

                StatusMessage = "Заявка отправлена. Спасибо!";
                ResetForm();
            }
            catch (ApiException ex) when (ex.IsUnauthorized)
            {
                ErrorMessage = "Сессия истекла — войдите повторно.";
                IsAuthenticated = false;
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsSending = false;
                SendFormCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanSend() => !IsSending;

        private void ResetForm()
        {
            OfficeNumber = string.Empty;
            PhoneNumber = string.Empty;
            Comment = string.Empty;
            foreach (var category in Problems)
            {
                category.IsChecked = false;
                foreach (var item in category.Items)
                    item.IsChecked = false;
            }
        }

        partial void OnIsAuthenticatedChanged(bool value)
        {
            if (!value)
                StatusMessage = null;
        }
    }
}
