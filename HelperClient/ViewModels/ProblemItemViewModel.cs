using CommunityToolkit.Mvvm.ComponentModel;

namespace HelperClient.ViewModels
{
    /// <summary>Элемент категории проблем с поддержкой INotifyPropertyChanged для чекбокса.</summary>
    internal partial class ProblemItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isChecked;

        public string Name { get; }

        public ProblemItemViewModel(string name, bool isChecked = false)
        {
            Name = name;
            _isChecked = isChecked;
        }
    }

    /// <summary>Группа («Expander») с названием и списком элементов.</summary>
    internal partial class ProblemCategoryViewModel : ObservableObject
    {
        /// <summary>Заголовок категории (совпадает с названием категории Category на сервере).</summary>
        public string Title { get; }

        public System.Collections.Generic.List<ProblemItemViewModel> Items { get; }

        public ProblemCategoryViewModel(string title, System.Collections.Generic.IEnumerable<string> itemNames)
        {
            Title = title;
            Items = new System.Collections.Generic.List<ProblemItemViewModel>();
            foreach (var name in itemNames)
                Items.Add(new ProblemItemViewModel(name));
        }
    }
}
