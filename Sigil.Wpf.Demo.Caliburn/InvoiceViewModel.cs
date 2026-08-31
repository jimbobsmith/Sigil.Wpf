using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Sigil.Wpf;
using Sigil.Wpf.Engine;
using Sigil.Wpf.Engine.Impl;

namespace Sigil.Wpf.Demo.Caliburn
{
    /// <summary>
    /// Caliburn <see cref="Screen"/> that implements <see cref="ISigilViewModel"/>
    /// instead of inheriting <see cref="SigilViewModel"/>.
    /// </summary>
    public class InvoiceViewModel : Screen, ISigilViewModel
    {
        public const string HoldRuleKey = "review-hold";
        public const string HoldTooltipRuleKey = "review-hold-tooltip";
        public const string HoldOpacityRuleKey = "review-hold-opacity";

        private static readonly SolidColorBrush NameMissingBrush = Freeze(Color.FromRgb(255, 228, 225));

        private bool _isLocked;
        private bool _isOnHold;
        private int _amount = 250;
        private string _name = "Ada Lovelace";
        private string _referenceCode = "REF-100";
        private int _lineLimit = 400;
        private bool _highlightOverLimit = true;
        private bool _freezePostedLines;
        private InvoiceLineViewModel _selectedLine;

        public InvoiceViewModel()
        {
            DisplayName = "Sigil — Caliburn.Micro Screen";
            Engine = SigilEngine.Create(this);

            var engine = Engine;
            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
            engine.AddPropertyDefault(UIElement.OpacityProperty, 1.0);
            engine.AddPropertyDefault(UIElement.VisibilityProperty, Visibility.Visible);
            engine.AddPropertyDefault(FrameworkElement.ToolTipProperty, null);
            engine.AddPropertyDefault(Control.BackgroundProperty, SystemColors.WindowBrush);
            engine.AddPropertyDefault(Control.ForegroundProperty, SystemColors.WindowTextBrush);
            engine.AddPropertyDefault(Control.FontWeightProperty, FontWeights.Normal);
            engine.AddPropertyDefault(Control.BorderBrushProperty, SystemColors.ControlDarkBrush);
            engine.AddPropertyDefault(Control.BorderThicknessProperty, new Thickness(1));

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => IsLocked, false, RuleResult.FallThrough, "[AllProperties] Form is locked");
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .AllPropertiesRule(_ => IsLocked, "[AllProperties] Form is locked", RuleResult.FallThrough);
            engine.Add.Binding(UIElement.OpacityProperty)
                .AllPropertiesRule(_ => IsLocked, 0.45, RuleResult.FallThrough);

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, false, RuleResult.FallThrough, "[Property] Over the $1,000 limit");
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, "[Property] Over the $1,000 limit", RuleResult.FallThrough);
            engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Amount, _ => Amount > 500, Brushes.Yellow, RuleResult.FallThrough);
            engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, Brushes.DarkRed, RuleResult.FallThrough);
            engine.Add.Binding(Control.FontWeightProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, FontWeights.Bold, RuleResult.FallThrough);
            engine.Add.Binding(UIElement.VisibilityProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, Visibility.Visible, Visibility.Collapsed);

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => Name, _ => string.IsNullOrWhiteSpace(Name), false, RuleResult.FallThrough, "[Property] Name is required");
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Name, _ => string.IsNullOrWhiteSpace(Name), "[Property] Name is required", RuleResult.FallThrough);
            engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Name, _ => string.IsNullOrWhiteSpace(Name), NameMissingBrush, RuleResult.FallThrough);

            Lines = new ObservableCollection<InvoiceLineViewModel>
            {
                new InvoiceLineViewModel(this, "Paper ream", 80, "Open"),
                new InvoiceLineViewModel(this, "Widget", 450, "Open"),
                new InvoiceLineViewModel(this, "Rush kit", 950, "Posted")
            };
            Lines.CollectionChanged += OnLinesChanged;
        }

        public ObservableCollection<InvoiceLineViewModel> Lines { get; }

        public InvoiceLineViewModel SelectedLine
        {
            get { return _selectedLine; }
            set
            {
                _selectedLine = value;
                RaisePropertyChanged(nameof(SelectedLine));
            }
        }

        public IRuleEngine Engine { get; set; }

        public object this[string key] => Engine.ApplyRulesTo(key);

        public void RaisePropertyChanged(string propertyName)
        {
            NotifyOfPropertyChange(propertyName);
            if (!string.Equals(propertyName, "Item[]", StringComparison.Ordinal))
                NotifyOfPropertyChange("Item[]");
        }

        public bool IsLocked
        {
            get { return _isLocked; }
            set
            {
                _isLocked = value;
                RaisePropertyChanged(nameof(IsLocked));
            }
        }

        public bool IsOnHold
        {
            get { return _isOnHold; }
            set
            {
                _isOnHold = value;
                if (value)
                {
                    Engine.Add.Binding(UIElement.IsEnabledProperty)
                        .TemporaryRule(HoldRuleKey, _ => true, false, RuleResult.FallThrough, "[Temporary] Held for review");
                    Engine.Add.Binding(FrameworkElement.ToolTipProperty)
                        .TemporaryRule(HoldTooltipRuleKey, _ => true, "[Temporary] Held for review", RuleResult.FallThrough);
                    Engine.Add.Binding(UIElement.OpacityProperty)
                        .TemporaryRule(HoldOpacityRuleKey, _ => true, 0.45, RuleResult.FallThrough);
                }
                else
                {
                    Engine.Remove.TemporaryRule(HoldRuleKey);
                    Engine.Remove.TemporaryRule(HoldTooltipRuleKey);
                    Engine.Remove.TemporaryRule(HoldOpacityRuleKey);
                }

                RaisePropertyChanged(nameof(IsOnHold));
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

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        public int LineLimit
        {
            get { return _lineLimit; }
            set
            {
                _lineLimit = value;
                RaisePropertyChanged(nameof(LineLimit));
            }
        }

        public bool HighlightOverLimit
        {
            get { return _highlightOverLimit; }
            set
            {
                _highlightOverLimit = value;
                RaisePropertyChanged(nameof(HighlightOverLimit));
            }
        }

        public bool FreezePostedLines
        {
            get { return _freezePostedLines; }
            set
            {
                _freezePostedLines = value;
                RaisePropertyChanged(nameof(FreezePostedLines));
            }
        }

        [StateChangeExempt]
        public string ReferenceCode
        {
            get { return _referenceCode; }
            set
            {
                _referenceCode = value;
                RaisePropertyChanged(nameof(ReferenceCode));
            }
        }

        public void SetAmount(int amount)
        {
            Amount = amount;
        }

        public void ClearName()
        {
            Name = string.Empty;
        }

        public void RestoreName()
        {
            Name = "Ada Lovelace";
        }

        public void AddLine()
        {
            Lines.Add(new InvoiceLineViewModel(this, "New item", 0, "Open"));
        }

        public void RemoveSelectedLine()
        {
            if (SelectedLine == null)
                return;

            Lines.Remove(SelectedLine);
            SelectedLine = null;
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                if (Lines != null)
                {
                    Lines.CollectionChanged -= OnLinesChanged;
                    foreach (var line in Lines)
                        line.Dispose();
                }

                Engine.Dispose();
            }

            return base.OnDeactivateAsync(close, cancellationToken);
        }

        private void OnLinesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems == null)
                return;

            foreach (InvoiceLineViewModel line in e.OldItems)
                line.Dispose();
        }

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
