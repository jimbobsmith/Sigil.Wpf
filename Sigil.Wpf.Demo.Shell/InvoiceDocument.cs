using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;

namespace Sigil.Wpf.Demo.Shell
{
    public class InvoiceLineDocument
    {
        public string Description { get; set; }
        public int Amount { get; set; }
        public string Status { get; set; }
    }

    public class InvoiceDocument : PropertyChangedBase
    {
        private string _customer;
        private string _status;
        private int _amount;

        public InvoiceDocument(string number, string customer, string status, int amount,
                               IEnumerable<InvoiceLineDocument> lines)
        {
            Number = number;
            _customer = customer;
            _status = status;
            _amount = amount;
            Lines = lines.ToList();
        }

        public string Number { get; }

        public string Customer
        {
            get { return _customer; }
            set
            {
                _customer = value;
                NotifyOfPropertyChange(nameof(Customer));
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                NotifyOfPropertyChange(nameof(Status));
            }
        }

        public int Amount
        {
            get { return _amount; }
            set
            {
                _amount = value;
                NotifyOfPropertyChange(nameof(Amount));
            }
        }

        public List<InvoiceLineDocument> Lines { get; }

        public int LineCount => Lines.Count;

        public void ReplaceLines(IEnumerable<InvoiceLineDocument> lines)
        {
            Lines.Clear();
            Lines.AddRange(lines);
            NotifyOfPropertyChange(nameof(LineCount));
        }
    }

    public interface ILineHost : System.ComponentModel.INotifyPropertyChanged
    {
        bool IsLocked { get; }
        bool IsOnHold { get; }
        bool FreezePostedLines { get; }
        bool HighlightOverLimit { get; }
        int LineLimit { get; }
    }

    public interface IWorkspaceHost
    {
        System.Threading.Tasks.Task OpenInvoiceAsync(InvoiceDocument invoice);
    }
}
