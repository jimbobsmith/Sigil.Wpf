using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Sigil.Wpf.Demo.Shell
{
    /// <summary>
    /// Navigation screen with no Sigil engine. Opening an invoice creates a tab
    /// that owns one.
    /// </summary>
    public class InvoiceListViewModel : Screen
    {
        private readonly IWorkspaceHost _host;
        private readonly IWindowManager _windowManager;
        private readonly BindableCollection<InvoiceDocument> _invoices;
        private string _filter = string.Empty;
        private InvoiceDocument _selectedInvoice;

        public InvoiceListViewModel(IWorkspaceHost host, IWindowManager windowManager,
                                    BindableCollection<InvoiceDocument> invoices)
        {
            _host = host;
            _windowManager = windowManager;
            _invoices = invoices;
            DisplayName = "Invoices";
        }

        public bool IsClosable => false;

        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value ?? string.Empty;
                NotifyOfPropertyChange(nameof(Filter));
                NotifyOfPropertyChange(nameof(FilteredInvoices));
            }
        }

        public IEnumerable<InvoiceDocument> FilteredInvoices
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Filter))
                    return _invoices;

                return _invoices.Where(invoice =>
                    Contains(invoice.Number, Filter) || Contains(invoice.Customer, Filter));
            }
        }

        public InvoiceDocument SelectedInvoice
        {
            get { return _selectedInvoice; }
            set
            {
                _selectedInvoice = value;
                NotifyOfPropertyChange(nameof(SelectedInvoice));
                NotifyOfPropertyChange(nameof(CanOpenInvoice));
            }
        }

        public bool CanOpenInvoice => SelectedInvoice != null;

        public Task OpenInvoice()
        {
            if (SelectedInvoice == null)
                return Task.CompletedTask;

            return _host.OpenInvoiceAsync(SelectedInvoice);
        }

        public async Task NewInvoice()
        {
            var next = NextNumber();
            var editor = new NewInvoiceViewModel(next);
            var ok = await _windowManager.ShowDialogAsync(editor);
            if (ok != true)
                return;

            var document = new InvoiceDocument(editor.Number, editor.Customer, "Draft", 0,
                Array.Empty<InvoiceLineDocument>());
            _invoices.Add(document);
            NotifyOfPropertyChange(nameof(FilteredInvoices));
            await _host.OpenInvoiceAsync(document);
        }

        private string NextNumber()
        {
            var max = 1000;
            foreach (var invoice in _invoices)
            {
                var suffix = invoice.Number.StartsWith("INV-", StringComparison.Ordinal)
                    ? invoice.Number.Substring(4)
                    : invoice.Number;
                if (int.TryParse(suffix, out var n) && n > max)
                    max = n;
            }

            return "INV-" + (max + 1);
        }

        private static bool Contains(string value, string filter)
        {
            return value != null
                   && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
