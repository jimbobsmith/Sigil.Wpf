using System.Windows;
using MahApps.Metro.Controls;
using MaterialDesignThemes.Wpf;

namespace Sigil.Wpf.Demo.Shell
{
    public partial class ShellView : MetroWindow
    {
        public ShellView()
        {
            InitializeComponent();
        }

        private void ToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var palette = new PaletteHelper();
            var theme = palette.GetTheme();
            theme.SetBaseTheme(theme.GetBaseTheme() == BaseTheme.Dark ? BaseTheme.Light : BaseTheme.Dark);
            palette.SetTheme(theme);
        }
    }
}
