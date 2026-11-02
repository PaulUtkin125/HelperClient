using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using HelperClient.ViewModels;

namespace HelperClient.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // DataContext устанавливаетсяcomposition root в Program.cs / App.axaml.cs
        }

        public MainWindow(MainWindowViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
