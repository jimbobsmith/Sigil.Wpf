using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Sigil.Wpf.Demo.Shell
{
    /// <summary>
    /// App shell only — no Sigil engine. Tabs are other screens; each of those
    /// that has ruled fields creates and disposes its own engine.
    /// </summary>
    public class ShellViewModel : Conductor<Screen>.Collection.OneActive, IWorkspaceHost
    {
        private readonly IWindowManager _windowManager;

        public ShellViewModel(IWindowManager windowManager)
        {
            _windowManager = windowManager;
            DisplayName = "Sigil — workspace";

            Invoices.Add(new InvoiceDocument("INV-1001", "Acme Corp", "Open", 250, new[]
            {
                new InvoiceLineDocument { Description = "Paper ream", Amount = 80, Status = "Open" },
                new InvoiceLineDocument { Description = "Widget", Amount = 450, Status = "Open" },
                new InvoiceLineDocument { Description = "Rush kit", Amount = 950, Status = "Posted" }
            }));
            Invoices.Add(new InvoiceDocument("INV-1002", "Northwind", "Posted", 1200, new[]
            {
                new InvoiceLineDocument { Description = "Annual license", Amount = 1200, Status = "Posted" }
            }));
            Invoices.Add(new InvoiceDocument("INV-1003", "Contoso", "Draft", 80, new[]
            {
                new InvoiceLineDocument { Description = "Sample docket", Amount = 80, Status = "Open" }
            }));

            var list = new InvoiceListViewModel(this, _windowManager, Invoices);
            Items.Add(list);
            ActivateItemAsync(list);
        }

        public BindableCollection<InvoiceDocument> Invoices { get; } = new BindableCollection<InvoiceDocument>();

        public Task OpenInvoiceAsync(InvoiceDocument invoice)
        {
            var existing = Items.OfType<InvoiceWorkspaceViewModel>()
                .FirstOrDefault(item => item.Number == invoice.Number);
            if (existing != null)
                return ActivateItemAsync(existing);

            var workspace = new InvoiceWorkspaceViewModel(invoice, _windowManager);
            Items.Add(workspace);
            return ActivateItemAsync(workspace);
        }

        public Task CloseTab(Screen item)
        {
            if (item is InvoiceListViewModel)
                return Task.CompletedTask;

            return DeactivateItemAsync(item, true);
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                foreach (var workspace in Items.OfType<InvoiceWorkspaceViewModel>().ToList())
                    await DeactivateItemAsync(workspace, true, cancellationToken);
            }

            await base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
