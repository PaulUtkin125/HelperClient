using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperClient.Services;

namespace HelperClient.ViewModels
{
    /// <summary>Элемент дерева категорий проблем в UI (отображает вложенность через отступ).</summary>
    public sealed partial class CategoryItemViewModel : ObservableObject
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int Level { get; init; }

        public bool IsLeaf { get; init; }

        /// <summary>Отступ для визуального сохранения иерархии.</summary>
        public double Indent => Level * 16;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isExpanded;
    }

    /// <summary>
    /// Главная страница: вывод категорий проблем (иерархия из /Category/GetAll)
    /// и формирование заявки (категория + мобильный телефон → /Request/Add).
    /// </summary>
    public partial class HomeViewModel : ViewModelBase
    {
        private const string CustomProblemCommentPrefix = "Моей проблемы нет в списке.";

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _customProblem = string.Empty;

        [ObservableProperty]
        private bool _isCustomErrorBoxEnable;

        [ObservableProperty]
        private bool _isCategoriesLoading = true;

        [ObservableProperty]
        private CategoryItemViewModel? _selectedCategory;

        [ObservableProperty]
        private string _categoriesLoadError = string.Empty;

        public ObservableCollection<CategoryItemViewModel> Categories { get; } = new();

        public bool CanSubmit =>
            !IsBusy &&
            (IsCustomErrorBoxEnable || SelectedCategory is not null) &&
            !string.IsNullOrWhiteSpace(PhoneNumber);

        public HomeViewModel(ClientServiceRegistry services) : base(services)
        {
            Services.StatusChanged += OnServiceStatus;
            _ = LoadCategoriesAsync();
        }

        partial void OnSelectedCategoryChanged(CategoryItemViewModel? value)
        {
            if (value is not null) IsCustomErrorBoxEnable = false;
            OnPropertyChanged(nameof(CanSubmit));
        }

        partial void OnIsCustomErrorBoxEnableChanged(bool value)
        {
            if (value) SelectedCategory = null;
            OnPropertyChanged(nameof(CanSubmit));
        }

        partial void OnPhoneNumberChanged(string value) => OnPropertyChanged(nameof(CanSubmit));

        partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));

        [RelayCommand]
        private Task ReloadCategoriesAsync() => LoadCategoriesAsync();

        private async Task LoadCategoriesAsync()
        {
            IsCategoriesLoading = true;
            CategoriesLoadError = string.Empty;
            try
            {
                var roots = await Services.GetProblemCategoriesAsync().ConfigureAwait(false);
                var flat = new List<CategoryItemViewModel>();
                foreach (var root in roots)
                    AddRecursive(root, flat);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Categories.Clear();
                    foreach (var item in flat) Categories.Add(item);
                });

                if (flat.Count == 0)
                    CategoriesLoadError = "Справочник категорий пуст";
            }
            catch (Exception ex)
            {
                CategoriesLoadError = $"Не удалось загрузить категории: {ex.Message}";
            }
            finally
            {
                IsCategoriesLoading = false;
            }
        }

        private static void AddRecursive(CategoryNode node, List<CategoryItemViewModel> target)
        {
            target.Add(new CategoryItemViewModel
            {
                Id = node.Id,
                Name = node.Name,
                Level = node.Level,
                IsLeaf = node.IsLeaf,
                IsExpanded = node.Level == 0,
            });

            foreach (var child in node.Children)
                AddRecursive(child, target);
        }

        [RelayCommand]
        private Task SubmitRequestAsync()
        {
            if (!CanSubmit)
            {
                ShowStatus("Выберите категорию проблемы (или отметьте «Моей проблемы нет в списке») и укажите номер телефона");
                return Task.CompletedTask;
            }

            int categoryId;
            string? comment = null;

            if (IsCustomErrorBoxEnable)
            {
                // Для произвольной проблемы берём первую доступную категорию и описываем проблему в комментарии.
                if (Categories.Count == 0)
                {
                    ShowStatus("Категории не загружены — сначала выполните синхронизацию профиля");
                    return Task.CompletedTask;
                }

                var text = CustomProblem.Trim();
                if (text.Length == 0)
                {
                    ShowStatus("Опишите вашу проблему");
                    return Task.CompletedTask;
                }

                if (text.Length > 230) text = text.Substring(0, 230);
                comment = $"{CustomProblemCommentPrefix} {text}";
                categoryId = FindFallbackCategoryId();
            }
            else
            {
                categoryId = SelectedCategory!.Id;
            }

            return RunAsync(async () =>
            {
                var result = await Services.CreateRequestAsync(categoryId, PhoneNumber, comment)
                    .ConfigureAwait(false);
                ShowStatus(result.Message);

                if (result.Success)
                {
                    SelectedCategory = null;
                    CustomProblem = string.Empty;
                    IsCustomErrorBoxEnable = false;
                }
            });
        }

        private int FindFallbackCategoryId()
        {
            foreach (var item in Categories)
                if (item.IsLeaf) return item.Id;
            return Categories.Count > 0 ? Categories[0].Id : 0;
        }

        private void OnServiceStatus(object? sender, string message) => ShowStatus(message);
    }
}
