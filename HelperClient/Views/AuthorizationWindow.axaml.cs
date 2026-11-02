using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HelperClient.ViewModels;

namespace HelperClient.Views
{
    /// <summary>Модальное окно входа (POST /HelperAPI/Authorize/Login).</summary>
    public partial class AuthorizationWindow : Window
    {
        public AuthorizationWindow()
        {
            InitializeComponent();
        }

        public AuthorizationWindow(AuthViewModel viewModel) : this()
        {
            DataContext = viewModel;
            viewModel.AuthorizationSuccess += () => Close(true);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
