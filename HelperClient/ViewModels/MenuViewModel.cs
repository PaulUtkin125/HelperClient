using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperClient.ViewModels
{
    internal partial class MenuViewModel : ObservableObject
    {
        private readonly MainWindowViewModel _mainWindow;

        public MenuViewModel(MainWindowViewModel mainWindow) 
            => _mainWindow = mainWindow;

        [RelayCommand]
        private void OpenProfile()
        {
            _mainWindow.PageHeadTitle = "Профиль";
            _mainWindow.CurrentPage = new ProfileViewModel();
        }

        [RelayCommand]
        private void OpenPartyLicensy()
        {
            _mainWindow.PageHeadTitle = "Party Licensy";
            _mainWindow.CurrentPage = new PartyLicensyViewModel();
        }
    }
}
