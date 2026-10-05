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
    /// Dialog engine is independent of the tab. It closes over the tab's line limit
    /// for highlight only — lock/hold on the tab do not paint these fields.
    /// </summary>
    public class LineEditorViewModel : Screen, ISigilViewModel
    {
        private static readonly SolidColorBrush MissingBrush = Freeze(Color.FromRgb(255, 228, 225));

        private readonly ILineHost _host;
        private readonly InvoiceLineViewModel _line;
        private string _description;
        private int _amount;
        private string _status;

        public LineEditorViewModel(ILineHost host, InvoiceLineViewModel line)
        {
            _host = host;
            _line = line;
            DisplayName = "Edit line";
            _description = line.Description;
            _amount = line.Amount;
            _status = line.Status;

            Engine = SigilEngine.Create(this);
            Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
            Engine.AddPropertyDefault(FrameworkElement.ToolTipProperty, null);
            Engine.AddPropertyDefault(Control.BackgroundProperty, SystemColors.WindowBrush);
            Engine.AddPropertyDefault(Control.ForegroundProperty, SystemColors.WindowTextBrush);
            Engine.AddPropertyDefault(Control.FontWeightProperty, FontWeights.Normal);

            Engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => Description, _ => string.IsNullOrWhiteSpace(Description), false,
                    RuleResult.FallThrough, "[Dialog] Line item is required");
            Engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Description, _ => string.IsNullOrWhiteSpace(Description),
                    "[Dialog] Line item is required", RuleResult.FallThrough);
            Engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Description, _ => string.IsNullOrWhiteSpace(Description), MissingBrush,
                    RuleResult.FallThrough);

            Engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Amount, _ => AmountOverLimit, Brushes.Yellow, RuleResult.FallThrough);
            Engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => Amount, _ => AmountOverDoubleLimit, Brushes.DarkRed, RuleResult.FallThrough);
            Engine.Add.Binding(Control.FontWeightProperty)
                .PropertyRule(() => Amount, _ => AmountOverDoubleLimit, FontWeights.Bold, RuleResult.FallThrough);
            Engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Amount, _ => AmountOverDoubleLimit, "[Dialog] Over 2× the tab line limit",
                    RuleResult.FallThrough);
        }

        public string[] StatusOptions { get; } = { "Open", "Posted" };

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                RaisePropertyChanged(nameof(Description));
                NotifyOfPropertyChange(nameof(CanSave));
            }
        }

        public int Amount
        {
            get { return _amount; }
            set
            {
                _amount = value;
                RaisePropertyChanged(nameof(Amount));
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value ?? "Open";
                RaisePropertyChanged(nameof(Status));
            }
        }

        public bool CanSave => !string.IsNullOrWhiteSpace(Description);

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

            _line.Description = Description;
            _line.Amount = Amount;
            _line.Status = Status;
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

        private bool AmountOverLimit =>
            _host.HighlightOverLimit && Amount > _host.LineLimit;

        private bool AmountOverDoubleLimit =>
            _host.HighlightOverLimit && Amount > _host.LineLimit * 2;

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
