using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Sigil.Wpf;
using Sigil.Wpf.Engine;

namespace Sigil.Wpf.Demo
{
    public class InvoiceLineViewModel : SigilViewModel
    {
        private static readonly SolidColorBrush MissingBrush = Freeze(Color.FromRgb(255, 228, 225));

        private readonly DemoViewModel _parent;
        private bool _disposed;
        private string _description;
        private int _amount;
        private string _status;

        public InvoiceLineViewModel(DemoViewModel parent, string description, int amount, string status)
        {
            _parent = parent;
            _description = description;
            _amount = amount;
            _status = status;
            _parent.PropertyChanged += OnParentChanged;
            ConfigureRules();
        }

        public string[] StatusOptions { get; } = { "Open", "Posted" };

        public bool IsRowEnabled =>
            !_parent.IsOnHold
            && !_parent.IsLocked
            && !(_parent.FreezePostedLines && Status == "Posted");

        public double RowOpacity => IsRowEnabled ? 1.0 : 0.45;

        public string RowToolTip =>
            _parent.IsOnHold ? "[Screen] Held for review" :
            _parent.IsLocked ? "[Screen] Form is locked" :
            _parent.FreezePostedLines && Status == "Posted" ? "[Screen] Posted lines are frozen" :
            null;

        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                RaisePropertyChanged(nameof(Description));
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
                NotifyPropertyChanged(nameof(Status));
                // DataGrid cancels the ComboBox edit if the row disables on this same turn.
                Dispatcher.CurrentDispatcher.BeginInvoke(RefreshRowChrome, DispatcherPriority.Input);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
            if (disposing)
                _parent.PropertyChanged -= OnParentChanged;
            base.Dispose(disposing);
        }

        private void ConfigureRules()
        {
            var engine = Engine;
            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
            engine.AddPropertyDefault(UIElement.OpacityProperty, 1.0);
            engine.AddPropertyDefault(FrameworkElement.ToolTipProperty, null);
            engine.AddPropertyDefault(Control.BackgroundProperty, SystemColors.WindowBrush);
            engine.AddPropertyDefault(Control.ForegroundProperty, SystemColors.WindowTextBrush);
            engine.AddPropertyDefault(Control.FontWeightProperty, FontWeights.Normal);
            engine.AddPropertyDefault(Control.BorderBrushProperty, SystemColors.ControlDarkBrush);
            engine.AddPropertyDefault(Control.BorderThicknessProperty, new Thickness(1));

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => _parent.IsOnHold, false, RuleResult.FallThrough, "[Screen] Held for review");
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .AllPropertiesRule(_ => _parent.IsOnHold, "[Screen] Held for review", RuleResult.FallThrough);
            engine.Add.Binding(UIElement.OpacityProperty)
                .AllPropertiesRule(_ => _parent.IsOnHold, 0.45, RuleResult.FallThrough);

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => _parent.IsLocked, false, RuleResult.FallThrough, "[Screen] Form is locked");
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .AllPropertiesRule(_ => _parent.IsLocked, "[Screen] Form is locked", RuleResult.FallThrough);
            engine.Add.Binding(UIElement.OpacityProperty)
                .AllPropertiesRule(_ => _parent.IsLocked, 0.45, RuleResult.FallThrough);

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => _parent.FreezePostedLines && Status == "Posted", false, RuleResult.FallThrough,
                    "[Screen] Posted lines are frozen");
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .AllPropertiesRule(_ => _parent.FreezePostedLines && Status == "Posted",
                    "[Screen] Posted lines are frozen", RuleResult.FallThrough);
            engine.Add.Binding(UIElement.OpacityProperty)
                .AllPropertiesRule(_ => _parent.FreezePostedLines && Status == "Posted", 0.45, RuleResult.FallThrough);

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => Amount, _ => AmountOverDoubleLimit, false, RuleResult.FallThrough,
                    "[Screen] Amount is over 2× the line amount limit");
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Amount, _ => AmountOverDoubleLimit, "[Screen] Amount is over 2× the line amount limit",
                    RuleResult.FallThrough);
            engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => Amount, _ => AmountOverDoubleLimit, Brushes.DarkRed, RuleResult.FallThrough);
            engine.Add.Binding(Control.FontWeightProperty)
                .PropertyRule(() => Amount, _ => AmountOverDoubleLimit, FontWeights.Bold, RuleResult.FallThrough);
            engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Amount, _ => AmountOverLimit, Brushes.Yellow, RuleResult.FallThrough);

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => Description, _ => string.IsNullOrWhiteSpace(Description), false,
                    RuleResult.FallThrough, "[Property] Line item is required");
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Description, _ => string.IsNullOrWhiteSpace(Description),
                    "[Property] Line item is required", RuleResult.FallThrough);
            engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Description, _ => string.IsNullOrWhiteSpace(Description), MissingBrush,
                    RuleResult.FallThrough);
        }

        private bool AmountOverLimit =>
            _parent.HighlightOverLimit && Amount > _parent.LineLimit;

        private bool AmountOverDoubleLimit =>
            _parent.HighlightOverLimit && Amount > _parent.LineLimit * 2;

        private void OnParentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null
                || e.PropertyName == nameof(DemoViewModel.LineLimit)
                || e.PropertyName == nameof(DemoViewModel.HighlightOverLimit)
                || e.PropertyName == nameof(DemoViewModel.FreezePostedLines)
                || e.PropertyName == nameof(DemoViewModel.IsLocked)
                || e.PropertyName == nameof(DemoViewModel.IsOnHold))
            {
                RefreshRowChrome();
            }
        }

        private void RefreshRowChrome()
        {
            NotifyPropertyChanged(nameof(IsRowEnabled));
            NotifyPropertyChanged(nameof(RowOpacity));
            NotifyPropertyChanged(nameof(RowToolTip));
            NotifyIndexer();
        }

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
