using System.ComponentModel;
using System.Windows;
using NUnit.Framework;
using Sigil.Wpf;
using Sigil.Wpf.Engine;
using Sigil.Wpf.Engine.Impl;

namespace Sigil.Wpf.Tests
{
    [TestFixture]
    public class ForeignBaseViewModelTests
    {
        [Test]
        public void ISigilViewModel_on_a_non_SigilViewModel_base_drives_rules()
        {
            var vm = new HybridViewModel();
            vm.Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
            vm.Engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => vm.Amount, _ => vm.Amount > 10, false, RuleResult.FallThrough);

            var itemNotified = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    itemNotified = true;
            };

            Assert.That(vm["IsEnabled.Amount"], Is.True);

            vm.Amount = 25;

            Assert.That(itemNotified, Is.True);
            Assert.That(vm["IsEnabled.Amount"], Is.False);

            vm.Engine.Dispose();
            itemNotified = false;
            vm.Amount = 1;

            Assert.That(itemNotified, Is.False);
        }

        private class ForeignPropertyChangedBase : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            protected void NotifyOfPropertyChange(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private sealed class HybridViewModel : ForeignPropertyChangedBase, ISigilViewModel
        {
            public HybridViewModel()
            {
                Engine = SigilEngine.Create(this);
            }

            public IRuleEngine Engine { get; set; }

            public object? this[string key] => Engine.ApplyRulesTo(key);

            public void RaisePropertyChanged(string propertyName)
            {
                NotifyOfPropertyChange(propertyName);
            }

            private int _amount;

            public int Amount
            {
                get { return _amount; }
                set
                {
                    _amount = value;
                    RaisePropertyChanged(nameof(Amount));
                }
            }
        }
    }
}
