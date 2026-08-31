using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NUnit.Framework;
using Sigil.Wpf;
using Sigil.Wpf.Engine;
using Sigil.Wpf.Engine.Impl;

namespace Sigil.Wpf.Tests
{
    [TestFixture]
    public class RowLevelRuleTests
    {
        private ScreenViewModel _screen = null!;
        private LineViewModel _line = null!;

        [SetUp]
        public void SetUp()
        {
            _screen = new ScreenViewModel();
            _line = new LineViewModel(_screen, "Widget", 450, "Open");
        }

        [TearDown]
        public void TearDown()
        {
            _line.Dispose();
            _screen.Dispose();
        }

        [Test]
        public void Lock_disables_every_line_field()
        {
            Assert.That(_line["IsEnabled.Description"], Is.True);
            Assert.That(_line["IsEnabled.Amount"], Is.True);
            Assert.That(_line["IsEnabled.Status"], Is.True);

            _screen.IsLocked = true;

            Assert.That(_line["IsEnabled.Description"], Is.False);
            Assert.That(_line["IsEnabled.Amount"], Is.False);
            Assert.That(_line["IsEnabled.Status"], Is.False);
            Assert.That(_line.IsRowEnabled, Is.False);
        }

        [Test]
        public void Hold_wins_over_an_unlocked_row()
        {
            _screen.IsOnHold = true;

            Assert.That(_line["IsEnabled.Description"], Is.False);
            Assert.That(_line["Opacity.Amount"], Is.EqualTo(0.45));
            Assert.That(_line.IsRowEnabled, Is.False);
        }

        [Test]
        public void Unlock_restores_every_line_field()
        {
            _screen.IsLocked = true;
            _screen.IsLocked = false;

            Assert.That(_line["IsEnabled.Description"], Is.True);
            Assert.That(_line["IsEnabled.Amount"], Is.True);
            Assert.That(_line["IsEnabled.Status"], Is.True);
            Assert.That(_line.IsRowEnabled, Is.True);
        }

        [Test]
        public void Freeze_disables_a_posted_row_and_leaves_an_open_row_alone()
        {
            var posted = new LineViewModel(_screen, "Rush kit", 100, "Posted");
            _screen.FreezePostedLines = true;

            Assert.That(posted["IsEnabled.Description"], Is.False);
            Assert.That(posted["IsEnabled.Amount"], Is.False);
            Assert.That(posted["IsEnabled.Status"], Is.False);
            Assert.That(posted.IsRowEnabled, Is.False);

            Assert.That(_line["IsEnabled.Description"], Is.True);
            Assert.That(_line.IsRowEnabled, Is.True);

            posted.Dispose();
        }

        [Test]
        public void Unfreeze_restores_a_posted_row()
        {
            var posted = new LineViewModel(_screen, "Rush kit", 100, "Posted");
            _screen.FreezePostedLines = true;
            _screen.FreezePostedLines = false;

            Assert.That(posted["IsEnabled.Description"], Is.True);
            Assert.That(posted.IsRowEnabled, Is.True);

            posted.Dispose();
        }

        [Test]
        public void Setting_Status_to_Posted_while_freeze_is_on_disables_the_row()
        {
            _screen.FreezePostedLines = true;
            Assert.That(_line["IsEnabled.Amount"], Is.True);

            _line.Status = "Posted";

            Assert.That(_line["IsEnabled.Description"], Is.False);
            Assert.That(_line["IsEnabled.Amount"], Is.False);
            Assert.That(_line["IsEnabled.Status"], Is.False);
            Assert.That(_line.IsRowEnabled, Is.False);
        }

        [Test]
        public void Parent_LineLimit_drives_this_row_Amount_not_the_screen_Amount()
        {
            _screen.Amount = 9999;
            _screen.LineLimit = 400;
            _screen.HighlightOverLimit = true;

            Assert.That(_line["Background.Amount"], Is.EqualTo(Brushes.Yellow));
            Assert.That(_line["IsEnabled.Amount"], Is.True);

            _line.Amount = 900;
            Assert.That(_line["IsEnabled.Amount"], Is.False);
            Assert.That(_line["Foreground.Amount"], Is.EqualTo(Brushes.DarkRed));

            Assert.That(_screen["IsEnabled.Amount"], Is.True);
        }

        [Test]
        public void Lowering_the_parent_LineLimit_refreshes_the_row_indexer()
        {
            _line.Amount = 100;
            Assert.That(_line["Background.Amount"], Is.EqualTo(SystemColors.WindowBrush));

            var itemNotified = false;
            _line.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    itemNotified = true;
            };

            _screen.LineLimit = 50;

            Assert.That(itemNotified, Is.True);
            Assert.That(_line["Background.Amount"], Is.EqualTo(Brushes.Yellow));
        }

        [Test]
        public void HighlightOverLimit_off_clears_amount_chrome()
        {
            _screen.HighlightOverLimit = false;

            Assert.That(_line["Background.Amount"], Is.EqualTo(SystemColors.WindowBrush));
            Assert.That(_line["IsEnabled.Amount"], Is.True);
        }

        [Test]
        public void Empty_Description_disables_only_that_cell()
        {
            _line.Description = " ";

            Assert.That(_line["IsEnabled.Description"], Is.False);
            Assert.That(_line["Background.Description"], Is.EqualTo(LineViewModel.MissingBrush));
            Assert.That(_line["IsEnabled.Amount"], Is.True);
            Assert.That(_line["IsEnabled.Status"], Is.True);
        }

        [Test]
        public void Two_rows_keep_independent_Amount_rules()
        {
            var cheap = new LineViewModel(_screen, "Paper", 80, "Open");

            Assert.That(cheap["Background.Amount"], Is.EqualTo(SystemColors.WindowBrush));
            Assert.That(_line["Background.Amount"], Is.EqualTo(Brushes.Yellow));

            cheap.Dispose();
        }

        [Test]
        public void Parent_lock_notifies_the_row_indexer()
        {
            var itemNotified = false;
            _line.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    itemNotified = true;
            };

            _screen.IsLocked = true;

            Assert.That(itemNotified, Is.True);
        }

        [Test]
        public void Dispose_unhooks_the_parent_so_later_screen_changes_do_not_notify_the_row()
        {
            var itemNotifications = 0;
            _line.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    itemNotifications++;
            };

            _screen.IsLocked = true;
            Assert.That(itemNotifications, Is.EqualTo(1));

            _line.Dispose();
            _screen.IsLocked = false;
            _screen.FreezePostedLines = true;
            _screen.LineLimit = 10;

            Assert.That(itemNotifications, Is.EqualTo(1));
        }

        [Test]
        public void Screen_and_row_engines_do_not_share_property_rules()
        {
            _screen.Engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => _screen.Amount, _ => true, Brushes.Purple, RuleResult.FallThrough);

            Assert.That(_screen["Background.Amount"], Is.EqualTo(Brushes.Purple));
            Assert.That(_line["Background.Amount"], Is.Not.EqualTo(Brushes.Purple));
        }

        [Test]
        public void Disposing_the_screen_disposes_remaining_lines()
        {
            var extra = new LineViewModel(_screen, "Paper", 80, "Open");
            _screen.Lines.Add(extra);

            var itemNotifications = 0;
            extra.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    itemNotifications++;
            };

            _screen.Dispose();
            _screen.IsLocked = true;

            Assert.That(itemNotifications, Is.EqualTo(0));
        }

        [Test]
        public void Removing_a_line_from_the_screen_collection_disposes_it()
        {
            var extra = new LineViewModel(_screen, "Paper", 80, "Open");
            _screen.Lines.Add(extra);

            var itemNotifications = 0;
            extra.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    itemNotifications++;
            };

            _screen.Lines.Remove(extra);
            _screen.IsLocked = true;

            Assert.That(itemNotifications, Is.EqualTo(0));
        }

        [Test]
        public void ISigilViewModel_row_follows_parent_lock_without_inheriting_SigilViewModel()
        {
            var hybrid = new HybridLineViewModel(_screen, 80);
            Assert.That(hybrid["IsEnabled.Amount"], Is.True);

            _screen.IsLocked = true;

            Assert.That(hybrid["IsEnabled.Amount"], Is.False);

            hybrid.Dispose();
        }

        private sealed class ScreenViewModel : SigilViewModel
        {
            private bool _isLocked;
            private bool _isOnHold;
            private bool _freezePostedLines;
            private bool _highlightOverLimit = true;
            private int _lineLimit = 400;
            private int _amount = 250;

            public ScreenViewModel()
            {
                Lines.CollectionChanged += OnLinesChanged;
                Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
                Engine.AddPropertyDefault(Control.BackgroundProperty, SystemColors.WindowBrush);
                Engine.Add.Binding(UIElement.IsEnabledProperty)
                    .AllPropertiesRule(_ => IsLocked, false, RuleResult.FallThrough);
            }

            public ObservableCollection<LineViewModel> Lines { get; } = new ObservableCollection<LineViewModel>();

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
                    RaisePropertyChanged(nameof(IsOnHold));
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

            public bool HighlightOverLimit
            {
                get { return _highlightOverLimit; }
                set
                {
                    _highlightOverLimit = value;
                    RaisePropertyChanged(nameof(HighlightOverLimit));
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

            public int Amount
            {
                get { return _amount; }
                set
                {
                    _amount = value;
                    RaisePropertyChanged(nameof(Amount));
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Lines.CollectionChanged -= OnLinesChanged;
                    foreach (var line in Lines)
                        line.Dispose();
                }

                base.Dispose(disposing);
            }

            private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.OldItems == null)
                    return;

                foreach (LineViewModel line in e.OldItems)
                    line.Dispose();
            }
        }

        private sealed class LineViewModel : SigilViewModel
        {
            public static readonly SolidColorBrush MissingBrush = CreateMissingBrush();

            private readonly ScreenViewModel _parent;
            private string _description;
            private int _amount;
            private string _status;

            public LineViewModel(ScreenViewModel parent, string description, int amount, string status)
            {
                _parent = parent;
                _description = description;
                _amount = amount;
                _status = status;
                _parent.PropertyChanged += OnParentChanged;
                ConfigureRules();
            }

            public bool IsRowEnabled =>
                !_parent.IsOnHold
                && !_parent.IsLocked
                && !(_parent.FreezePostedLines && Status == "Posted");

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
                    RefreshRowChrome();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    _parent.PropertyChanged -= OnParentChanged;
                base.Dispose(disposing);
            }

            private void ConfigureRules()
            {
                Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
                Engine.AddPropertyDefault(UIElement.OpacityProperty, 1.0);
                Engine.AddPropertyDefault(Control.BackgroundProperty, SystemColors.WindowBrush);
                Engine.AddPropertyDefault(Control.ForegroundProperty, SystemColors.WindowTextBrush);

                Engine.Add.Binding(UIElement.IsEnabledProperty)
                    .AllPropertiesRule(_ => _parent.IsOnHold, false, RuleResult.FallThrough);
                Engine.Add.Binding(UIElement.OpacityProperty)
                    .AllPropertiesRule(_ => _parent.IsOnHold, 0.45, RuleResult.FallThrough);

                Engine.Add.Binding(UIElement.IsEnabledProperty)
                    .AllPropertiesRule(_ => _parent.IsLocked, false, RuleResult.FallThrough);

                Engine.Add.Binding(UIElement.IsEnabledProperty)
                    .AllPropertiesRule(_ => _parent.FreezePostedLines && Status == "Posted", false,
                        RuleResult.FallThrough);

                Engine.Add.Binding(Control.BackgroundProperty)
                    .PropertyRule(() => Amount, _ => AmountOverLimit, Brushes.Yellow, RuleResult.FallThrough);
                Engine.Add.Binding(UIElement.IsEnabledProperty)
                    .PropertyRule(() => Amount, _ => AmountOverDoubleLimit, false, RuleResult.FallThrough);
                Engine.Add.Binding(Control.ForegroundProperty)
                    .PropertyRule(() => Amount, _ => AmountOverDoubleLimit, Brushes.DarkRed, RuleResult.FallThrough);

                Engine.Add.Binding(UIElement.IsEnabledProperty)
                    .PropertyRule(() => Description, _ => string.IsNullOrWhiteSpace(Description), false,
                        RuleResult.FallThrough);
                Engine.Add.Binding(Control.BackgroundProperty)
                    .PropertyRule(() => Description, _ => string.IsNullOrWhiteSpace(Description), MissingBrush,
                        RuleResult.FallThrough);
            }

            private bool AmountOverLimit =>
                _parent.HighlightOverLimit && Amount > _parent.LineLimit;

            private bool AmountOverDoubleLimit =>
                _parent.HighlightOverLimit && Amount > _parent.LineLimit * 2;

            private void OnParentChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == null
                    || e.PropertyName == nameof(ScreenViewModel.LineLimit)
                    || e.PropertyName == nameof(ScreenViewModel.HighlightOverLimit)
                    || e.PropertyName == nameof(ScreenViewModel.FreezePostedLines)
                    || e.PropertyName == nameof(ScreenViewModel.IsLocked)
                    || e.PropertyName == nameof(ScreenViewModel.IsOnHold))
                {
                    RefreshRowChrome();
                }
            }

            private void RefreshRowChrome()
            {
                NotifyPropertyChanged(nameof(IsRowEnabled));
                NotifyIndexer();
            }

            private static SolidColorBrush CreateMissingBrush()
            {
                var brush = new SolidColorBrush(Color.FromRgb(255, 228, 225));
                brush.Freeze();
                return brush;
            }
        }

        private sealed class HybridLineViewModel : INotifyPropertyChanged, ISigilViewModel, System.IDisposable
        {
            private readonly ScreenViewModel _parent;
            private int _amount;

            public HybridLineViewModel(ScreenViewModel parent, int amount)
            {
                _parent = parent;
                _amount = amount;
                Engine = SigilEngine.Create(this);
                Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
                Engine.Add.Binding(UIElement.IsEnabledProperty)
                    .AllPropertiesRule(_ => _parent.IsLocked, false, RuleResult.FallThrough);
                _parent.PropertyChanged += OnParentChanged;
            }

            public IRuleEngine Engine { get; set; }

            public object? this[string key] => Engine.ApplyRulesTo(key);

            public event PropertyChangedEventHandler? PropertyChanged;

            public void RaisePropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
                if (propertyName != "Item[]")
                    RaisePropertyChanged("Item[]");
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

            public void Dispose()
            {
                _parent.PropertyChanged -= OnParentChanged;
                Engine.Dispose();
            }

            private void OnParentChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(ScreenViewModel.IsLocked))
                    RaisePropertyChanged("Item[]");
            }
        }
    }
}
