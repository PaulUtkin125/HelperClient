using Avalonia.Controls;
using Avalonia.Interactivity;
using HelperClient.ViewModels;

namespace HelperClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }
        private void SwitchLokeTextBox_Click(object sender, RoutedEventArgs e)
        {
            //CustomErorr.IsEnabled = !CustomErorr.IsEnabled;
        }

        private void MyPopup_Click(object sender, RoutedEventArgs e)
        {

        }


    }
}