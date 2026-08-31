using System.Windows;
using Caliburn.Micro;

namespace Sigil.Wpf.Demo.Caliburn
{
    public class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper()
        {
            Initialize();
        }

        protected override async void OnStartup(object sender, StartupEventArgs e)
        {
            await DisplayRootViewForAsync<InvoiceViewModel>();
        }
    }
}
