using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HelperClient.ViewModels
{
    public partial class MenuViewModel : ObservableObject
    {
        private readonly MainWindowViewModel _mainWindow;
        private ProfileViewModel? _profileViewModel;

        public MenuViewModel(MainWindowViewModel mainWindow)
            => _mainWindow = mainWindow;

        [RelayCommand]
        private void OpenProfile()
        {
            _profileViewModel ??= new ProfileViewModel(ViewModelLocator.Current);
            _mainWindow.PageHeadTitle = "Профиль";
            _mainWindow.CurrentPage = _profileViewModel;
        }

        [RelayCommand]
        private void OpenPartyLicensy()
        {
            _mainWindow.PageHeadTitle = "Party Licensy";
            _mainWindow.CurrentPage = new PartyLicensyViewModel();
        }
    }
}
