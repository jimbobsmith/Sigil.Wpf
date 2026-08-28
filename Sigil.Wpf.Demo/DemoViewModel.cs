using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sigil.Wpf;
using Sigil.Wpf.Engine;

namespace Sigil.Wpf.Demo
{
    public class DemoViewModel : SigilViewModel
    {
        public const string HoldRuleKey = "review-hold";
        public const string HoldTooltipRuleKey = "review-hold-tooltip";
        public const string HoldOpacityRuleKey = "review-hold-opacity";

        public const string TemporaryTooltip = "[Temporary] Held for review";
        public const string GlobalTooltip = "[Global] Form is locked";
        public const string AmountPropertyTooltip = "[Property] Amount exceeds the $1,000 limit";
        public const string NamePropertyTooltip = "[Property] Name is required";
        public const string NotesUrgentTooltip = "[Property] URGENT — flagged in notes";
        public const string NotesThinTooltip = "[Property] Notes are too short";
        public const string NotesApprovedTooltip = "[Property] Approved — notes are read-only";

        public const string DefaultNotes = "Hover a field, including disabled ones.";

        private static readonly SolidColorBrush NameMissingBrush = Freeze(Color.FromRgb(255, 228, 225));
        private static readonly SolidColorBrush ApprovedBrush = Freeze(Color.FromRgb(232, 245, 233));
        private static readonly SolidColorBrush RejectedBrush = Freeze(Color.FromRgb(255, 235, 238));

        private bool _isLocked;
        private bool _isOnHold;
        private bool _isRush;
        private int _amount = 250;
        private string _name = "Ada Lovelace";
        private string _notes = DefaultNotes;
        private string _status = "Draft";
        private string _referenceCode = "REF-100";

        public DemoViewModel()
        {
            var engine = Engine;

            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
            engine.AddPropertyDefault(UIElement.OpacityProperty, 1.0);
            engine.AddPropertyDefault(UIElement.VisibilityProperty, Visibility.Visible);
            engine.AddPropertyDefault(FrameworkElement.ToolTipProperty, null);
            engine.AddPropertyDefault(Control.BackgroundProperty, SystemColors.WindowBrush);
            engine.AddPropertyDefault(Control.BorderBrushProperty, SystemColors.ControlDarkBrush);
            engine.AddPropertyDefault(Control.BorderThicknessProperty, new Thickness(1));
            engine.AddPropertyDefault(Control.ForegroundProperty, SystemColors.WindowTextBrush);
            engine.AddPropertyDefault(Control.FontWeightProperty, FontWeights.Normal);
            engine.AddPropertyDefault(Control.FontStyleProperty, FontStyles.Normal);
            engine.AddPropertyDefault(Control.FontSizeProperty, 13.0);
            engine.AddPropertyDefault(TextBox.IsReadOnlyProperty, false);

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .GlobalRule(_ => IsLocked, false, RuleResult.FallThrough, GlobalTooltip);
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .GlobalRule(_ => IsLocked, GlobalTooltip, RuleResult.FallThrough);
            engine.Add.Binding(UIElement.OpacityProperty)
                .GlobalRule(_ => IsLocked, 0.45, RuleResult.FallThrough);

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, false, RuleResult.FallThrough, AmountPropertyTooltip);
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, AmountPropertyTooltip, RuleResult.FallThrough);
            engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Amount, _ => Amount > 500, Brushes.Yellow, RuleResult.FallThrough);
            engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, Brushes.DarkRed, RuleResult.FallThrough);
            engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => Amount, _ => Amount > 500, Brushes.DarkGoldenrod, RuleResult.FallThrough);
            engine.Add.Binding(Control.FontWeightProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, FontWeights.Bold, RuleResult.FallThrough);
            engine.Add.Binding(Control.BorderBrushProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, Brushes.OrangeRed, RuleResult.FallThrough);
            engine.Add.Binding(Control.BorderThicknessProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, new Thickness(3), RuleResult.FallThrough);
            engine.Add.Binding(UIElement.VisibilityProperty)
                .PropertyRule(() => Amount, _ => Amount > 1000, Visibility.Visible, Visibility.Collapsed);

            engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => Name, _ => string.IsNullOrWhiteSpace(Name), false, RuleResult.FallThrough, NamePropertyTooltip);
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Name, _ => string.IsNullOrWhiteSpace(Name), NamePropertyTooltip, RuleResult.FallThrough);
            engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Name, _ => string.IsNullOrWhiteSpace(Name), NameMissingBrush, RuleResult.FallThrough);
            engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => Name, _ => string.IsNullOrWhiteSpace(Name), Brushes.IndianRed, RuleResult.FallThrough);
            engine.Add.Binding(Control.FontSizeProperty)
                .PropertyRule(() => Name, _ => IsRush && !string.IsNullOrWhiteSpace(Name), 18.0, RuleResult.FallThrough);
            engine.Add.Binding(Control.FontWeightProperty)
                .PropertyRule(() => Name, _ => IsRush && !string.IsNullOrWhiteSpace(Name), FontWeights.Bold, RuleResult.FallThrough);

            engine.Add.Binding(TextBox.IsReadOnlyProperty)
                .PropertyRule(() => Notes, _ => Status == "Approved", true, RuleResult.FallThrough);
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Notes, _ => Status == "Approved", NotesApprovedTooltip, RuleResult.FallThrough);
            engine.Add.Binding(Control.BorderBrushProperty)
                .PropertyRule(() => Notes, _ => NotesUrgent, Brushes.Crimson, RuleResult.FallThrough);
            engine.Add.Binding(Control.BorderThicknessProperty)
                .PropertyRule(() => Notes, _ => NotesUrgent, new Thickness(3), RuleResult.FallThrough);
            engine.Add.Binding(Control.FontWeightProperty)
                .PropertyRule(() => Notes, _ => NotesUrgent, FontWeights.Bold, RuleResult.FallThrough);
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Notes, _ => NotesUrgent, NotesUrgentTooltip, RuleResult.FallThrough);
            engine.Add.Binding(Control.FontStyleProperty)
                .PropertyRule(() => Notes, _ => NotesThin, FontStyles.Italic, RuleResult.FallThrough);
            engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => Notes, _ => NotesThin, Brushes.Gray, RuleResult.FallThrough);
            engine.Add.Binding(FrameworkElement.ToolTipProperty)
                .PropertyRule(() => Notes, _ => NotesThin, NotesThinTooltip, RuleResult.FallThrough);

            engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Status, _ => Status == "Review", Brushes.LightGoldenrodYellow, RuleResult.FallThrough);
            engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Status, _ => Status == "Approved", ApprovedBrush, RuleResult.FallThrough);
            engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => Status, _ => Status == "Rejected", RejectedBrush, RuleResult.FallThrough);
            engine.Add.Binding(Control.BorderBrushProperty)
                .PropertyRule(() => Status, _ => Status == "Review", Brushes.DarkGoldenrod, RuleResult.FallThrough);
            engine.Add.Binding(Control.BorderBrushProperty)
                .PropertyRule(() => Status, _ => Status == "Approved", Brushes.ForestGreen, RuleResult.FallThrough);
            engine.Add.Binding(Control.BorderBrushProperty)
                .PropertyRule(() => Status, _ => Status == "Rejected", Brushes.Crimson, RuleResult.FallThrough);
            engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => Status, _ => Status == "Approved", Brushes.ForestGreen, RuleResult.FallThrough);
            engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => Status, _ => Status == "Rejected", Brushes.Crimson, RuleResult.FallThrough);
            engine.Add.Binding(Control.FontWeightProperty)
                .PropertyRule(() => Status, _ => Status != "Draft", FontWeights.SemiBold, RuleResult.FallThrough);
        }

        public string[] StatusOptions { get; } = { "Draft", "Review", "Approved", "Rejected" };

        public bool IsLocked
        {
            get { return _isLocked; }
            set
            {
                _isLocked = value;
                RaisePropertyChanged(nameof(IsLocked));
                RefreshRules();
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
                        .TemporaryRule(HoldRuleKey, _ => true, false, RuleResult.FallThrough, TemporaryTooltip);
                    Engine.Add.Binding(FrameworkElement.ToolTipProperty)
                        .TemporaryRule(HoldTooltipRuleKey, _ => true, TemporaryTooltip, RuleResult.FallThrough);
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
                RefreshRules();
            }
        }

        public bool IsRush
        {
            get { return _isRush; }
            set
            {
                _isRush = value;
                RaisePropertyChanged(nameof(IsRush));
                RefreshRules();
            }
        }

        public int Amount
        {
            get { return _amount; }
            set
            {
                _amount = value;
                RaisePropertyChanged(nameof(Amount));
                RefreshRules();
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged(nameof(Name));
                RefreshRules();
            }
        }

        public string Notes
        {
            get { return _notes; }
            set
            {
                _notes = value;
                RaisePropertyChanged(nameof(Notes));
                RefreshRules();
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value ?? "Draft";
                RaisePropertyChanged(nameof(Status));
                RefreshRules();
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

        public bool AmountOverLimit => Amount > 1000;
        public bool AmountWarning => Amount > 500;
        public bool NameMissing => string.IsNullOrWhiteSpace(Name);
        public bool NotesUrgent =>
            !string.IsNullOrEmpty(Notes) &&
            Notes.IndexOf("urgent", StringComparison.OrdinalIgnoreCase) >= 0;
        public bool NotesThin => string.IsNullOrWhiteSpace(Notes) || Notes.Trim().Length < 8;
        public bool NotesApproved => Status == "Approved";

        public string AmountChrome =>
            AmountOverLimit
                ? "Yellow, bold red text, orange border, banner"
                : AmountWarning
                    ? "Yellow + gold text"
                    : "Default chrome";

        public string NameChrome =>
            NameMissing
                ? "Pink + red text"
                : IsRush
                    ? "Rush: 18pt bold"
                    : "Default chrome";

        public string NotesChrome =>
            NotesApproved
                ? "Read-only (Approved)"
                : NotesUrgent
                    ? "Urgent: bold crimson border"
                    : NotesThin
                        ? "Italic gray (too short)"
                        : "Default chrome";

        public string StatusChrome =>
            Status == "Review" ? "Gold review chrome" :
            Status == "Approved" ? "Green + notes read-only" :
            Status == "Rejected" ? "Pink reject chrome" :
            "Default chrome";

        public string NameTooltip => TooltipFor(nameof(Name));
        public string AmountTooltip => TooltipFor(nameof(Amount));
        public string NotesTooltip => TooltipFor(nameof(Notes));
        public string StatusTooltip => TooltipFor(nameof(Status));
        public string ReferenceTooltip => TooltipFor(nameof(ReferenceCode));

        public string NameWinner => WinnerFor(nameof(Name));
        public string AmountWinner => WinnerFor(nameof(Amount));
        public string NotesWinner => WinnerFor(nameof(Notes));
        public string StatusWinner => WinnerFor(nameof(Status));
        public string ReferenceWinner => WinnerFor(nameof(ReferenceCode));

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

        public void StampUrgent()
        {
            if (NotesUrgent)
                return;
            Notes = string.IsNullOrWhiteSpace(Notes) ? "URGENT" : "URGENT — " + Notes;
        }

        public void ResetNotes()
        {
            Notes = DefaultNotes;
        }

        private static SolidColorBrush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private string TooltipFor(string property)
        {
            return this["ToolTip." + property] as string ?? "(none — default)";
        }

        private string WinnerFor(string property)
        {
            if (property == nameof(ReferenceCode))
                return "Exempt (skipped)";
            if (IsOnHold)
                return "Temporary";
            if (IsLocked)
                return "Global";
            if (property == nameof(Amount) && (AmountOverLimit || AmountWarning))
                return "Property";
            if (property == nameof(Name) && (NameMissing || IsRush))
                return "Property";
            if (property == nameof(Notes) && (NotesApproved || NotesUrgent || NotesThin))
                return "Property";
            if (property == nameof(Status) && Status != "Draft")
                return "Property";
            return "Default";
        }

        private void RefreshRules()
        {
            RaisePropertyChanged("Item[]");
            RaisePropertyChanged(nameof(AmountOverLimit));
            RaisePropertyChanged(nameof(AmountWarning));
            RaisePropertyChanged(nameof(AmountChrome));
            RaisePropertyChanged(nameof(NameChrome));
            RaisePropertyChanged(nameof(NameMissing));
            RaisePropertyChanged(nameof(NotesUrgent));
            RaisePropertyChanged(nameof(NotesThin));
            RaisePropertyChanged(nameof(NotesApproved));
            RaisePropertyChanged(nameof(NotesChrome));
            RaisePropertyChanged(nameof(StatusChrome));
            RaisePropertyChanged(nameof(NameTooltip));
            RaisePropertyChanged(nameof(AmountTooltip));
            RaisePropertyChanged(nameof(NotesTooltip));
            RaisePropertyChanged(nameof(StatusTooltip));
            RaisePropertyChanged(nameof(ReferenceTooltip));
            RaisePropertyChanged(nameof(NameWinner));
            RaisePropertyChanged(nameof(AmountWinner));
            RaisePropertyChanged(nameof(NotesWinner));
            RaisePropertyChanged(nameof(StatusWinner));
            RaisePropertyChanged(nameof(ReferenceWinner));
        }
    }
}
