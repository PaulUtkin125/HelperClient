using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace HelperClient.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private object? _currentPage;

        [ObservableProperty]
        private string _pageHeadTitle = "С какой проблемой Вы столкнулись?";

        private HomeViewModel _homeViewModel = new HomeViewModel();
        public MainWindowViewModel() => CurrentPage = _homeViewModel;

        [RelayCommand]
        private void GoToMenu()
        {
            if (CurrentPage is MenuViewModel)
            {
                PageHeadTitle = "С какой проблемой Вы столкнулись?";
                CurrentPage = _homeViewModel;
            }
            else
            {
                PageHeadTitle = "Меню";
                CurrentPage = new MenuViewModel(this);
            }
        }

    }

    
}
