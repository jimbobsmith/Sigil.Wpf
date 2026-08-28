using System.Windows;
using Sigil.Wpf;
using Sigil.Wpf.Engine;
using NUnit.Framework;

namespace Sigil.Wpf.Tests
{
    [TestFixture]
    public class SigilViewModelTests
    {
        [Test]
        public void Indexer_and_NotifyIndexer_refresh_bindings()
        {
            using (var vm = new SampleViewModel())
            {
                vm.Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
                vm.Engine.Add.Binding(UIElement.IsEnabledProperty)
                    .PropertyRule(() => vm.Amount, _ => vm.Amount > 10, false, RuleResult.FallThrough);

                var notified = false;
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == "Item[]")
                        notified = true;
                };

                Assert.That(vm["IsEnabled.Amount"], Is.True);

                vm.Amount = 25;

                Assert.That(notified, Is.True);
                Assert.That(vm["IsEnabled.Amount"], Is.False);
            }
        }

        [Test]
        public void Dispose_unhooks_engine_property_subscriptions()
        {
            var vm = new WatchedViewModel();
            vm.Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
            vm.Engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => vm.Amount, _ => true, false, RuleResult.FallThrough);

            var itemNotifications = 0;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                    itemNotifications++;
            };

            vm.Amount = 1;
            Assert.That(itemNotifications, Is.EqualTo(1));

            vm.Dispose();
            vm.Amount = 2;
            Assert.That(itemNotifications, Is.EqualTo(1));
        }

        private sealed class SampleViewModel : SigilViewModel
        {
            private int _amount;

            public int Amount
            {
                get { return _amount; }
                set
                {
                    _amount = value;
                    RaisePropertyChanged(nameof(Amount));
                    NotifyIndexer();
                }
            }
        }

        private sealed class WatchedViewModel : SigilViewModel
        {
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
