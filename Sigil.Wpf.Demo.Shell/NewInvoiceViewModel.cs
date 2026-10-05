using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Sigil.Wpf;
using Sigil.Wpf.Engine;
using Sigil.Wpf.Engine.Impl;

namespace Sigil.Wpf.Demo.Shell
{
    /// <summary>
    /// Dialog with its own engine. Workspace lock on another tab does not apply here.
    /// </summary>
    public class NewInvoiceViewModel : Screen, ISigilViewModel
    {
        private static readonly SolidColorBrush NameMissingBrush = Freeze(Color.FromRgb(255, 228, 225));

        private string _customer = string.Empty;

        public NewInvoiceViewModel(string number)
        {
            DisplayName = "New invoice";
            Number = number;
            Engine = SigilEngine.Create(this);

            Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
            Engine.AddPropertyDefault(FrameworkElement.ToolTipProperty, null);
            Engine.AddPropertyDefault(Control.BackgroundProperty, SystemColors.WindowBrush);

            Engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => Customer, _ => string.IsNullOrWhiteSpace(Customer), false, RuleResult.FallThrough,
                    "[Property] Customer is required");
            Engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Customer, _ => string.IsNullOrWhiteSpace(Customer), "[Property] Customer is required",
                    RuleResult.FallThrough);
            Engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Customer, _ => string.IsNullOrWhiteSpace(Customer), NameMissingBrush,
                    RuleResult.FallThrough);
        }

        public string Number { get; }

        public string Customer
        {
            get { return _customer; }
            set
            {
                _customer = value;
                RaisePropertyChanged(nameof(Customer));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public bool CanSave => !string.IsNullOrWhiteSpace(Customer);

        public IRuleEngine Engine { get; set; }

        public object this[string key] => Engine.ApplyRulesTo(key);

        public void RaisePropertyChanged(string propertyName)
        {
            NotifyOfPropertyChange(propertyName);
            if (!string.Equals(propertyName, "Item[]", StringComparison.Ordinal))
                NotifyOfPropertyChange("Item[]");
        }

        public Task Save()
        {
            if (!CanSave)
                return Task.CompletedTask;

            return TryCloseAsync(true);
        }

        public Task Cancel()
        {
            return TryCloseAsync(false);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
                Engine.Dispose();

            return base.OnDeactivateAsync(close, cancellationToken);
        }

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
