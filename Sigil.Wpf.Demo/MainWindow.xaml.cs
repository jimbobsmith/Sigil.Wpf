using System.Windows;

namespace Sigil.Wpf.Demo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new DemoViewModel();
        }

        private DemoViewModel ViewModel => (DemoViewModel)DataContext;

        private void SetAmount250_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.SetAmount(250);
        }

        private void SetAmount750_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.SetAmount(750);
        }

        private void SetAmount1500_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.SetAmount(1500);
        }

        private void ClearName_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearName();
        }

        private void RestoreName_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.RestoreName();
        }

        private void StampUrgent_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.StampUrgent();
        }

        private void ResetNotes_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.ResetNotes();
        }
    }
}
