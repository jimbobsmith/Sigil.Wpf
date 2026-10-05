using System.Windows.Controls;
using System.Windows.Input;

namespace Sigil.Wpf.Demo.Shell
{
    public partial class InvoiceListView : UserControl
    {
        public InvoiceListView()
        {
            InitializeComponent();
        }

        private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as InvoiceListViewModel;
            if (vm != null && vm.CanOpenInvoice)
                vm.OpenInvoice();
        }
    }
}
