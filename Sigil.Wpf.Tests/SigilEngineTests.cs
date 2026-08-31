using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Sigil.Wpf;
using Sigil.Wpf.Engine;
using Sigil.Wpf.Engine.Impl;
using Sigil.Wpf.Tests.Support;
using NUnit.Framework;

namespace Sigil.Wpf.Tests
{
    [TestFixture]
    public class SigilEngineTests
    {
        private TestViewModel _vm = null!;
        private SigilEngine _engine = null!;

        [SetUp]
        public void SetUp()
        {
            _vm = new TestViewModel();
            _engine = (SigilEngine)_vm.Engine;
            _engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
        }

        [TearDown]
        public void TearDown()
        {
            _engine.Dispose();
        }

        [Test]
        public void Indexer_delegates_to_ApplyRulesTo()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(p => p == nameof(TestViewModel.Name), false, RuleResult.FallThrough);

            Assert.That(_vm["IsEnabled.Name"], Is.False);
            Assert.That(_vm["IsEnabled.FirstName"], Is.True);
        }

        [Test]
        public void AllPropertiesRule_skips_StateChangeExempt_on_the_view_model_property()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => true, false, RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.ExemptName"), Is.True);
            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.False);
        }

        [Test]
        public void AllPropertiesRule_finds_StateChangeExempt_on_a_null_nested_object()
        {
            _vm.Address = null;
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => true, false, RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Address.City"), Is.True);
            Assert.That(_engine.ApplyRulesTo("IsEnabled.Address.Street"), Is.False);
        }

        [Test]
        public void Tooltip_lookup_uses_the_exact_view_model_property()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(p => p == nameof(TestViewModel.FirstName), false, RuleResult.FallThrough, "from first");
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(p => p == nameof(TestViewModel.Name), false, RuleResult.FallThrough, "from name");

            Assert.That(_engine.ApplyRulesTo("IsEnabled.FirstName"), Is.False);
            Assert.That(_engine.ApplyRulesTo("ToolTip.Name"), Is.Null);
            Assert.That(_engine.ApplyRulesTo("ToolTip.FirstName"), Is.EqualTo("from first"));

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.False);
            Assert.That(_engine.ApplyRulesTo("ToolTip.Name"), Is.EqualTo("from name"));
        }

        [Test]
        public void PropertyRule_notifies_indexer_for_the_exact_property_only()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.FirstName, _ => true, false, RuleResult.FallThrough);

            _vm.Notifications.Clear();
            _vm.Name = "not a match";
            Assert.That(_vm.Notifications, Does.Not.Contain("Item[]"));

            _vm.Notifications.Clear();
            _vm.FirstName = "match";
            Assert.That(_vm.Notifications, Does.Contain("Item[]"));
            Assert.That(_vm.Notifications, Has.None.Null);
        }

        [Test]
        public void PropertyRule_resubscribes_when_a_nested_object_is_replaced()
        {
            var original = new Address { City = "Austin" };
            _vm.Address = original;
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.Address.City, _ => true, false, RuleResult.FallThrough);

            var replacement = new Address { City = "Dallas" };
            _vm.Notifications.Clear();
            _vm.Address = replacement;
            Assert.That(_vm.Notifications, Does.Contain("Item[]"));

            _vm.Notifications.Clear();
            original.City = "should not notify";
            Assert.That(_vm.Notifications, Does.Not.Contain("Item[]"));

            _vm.Notifications.Clear();
            replacement.City = "should notify";
            Assert.That(_vm.Notifications, Does.Contain("Item[]"));
        }

        [Test]
        public void GetRegisteredProperties_includes_defaults_and_rule_bindings()
        {
            _engine.AddPropertyDefault(Control.BackgroundProperty, SystemColors.WindowBrush);
            _engine.Add.Binding(Control.ForegroundProperty)
                .PropertyRule(() => _vm.Name, _ => true, Brushes.Red, RuleResult.FallThrough);

            var registered = _engine.GetRegisteredProperties();
            Assert.That(registered, Does.Contain(UIElement.IsEnabledProperty));
            Assert.That(registered, Does.Contain(Control.BackgroundProperty));
            Assert.That(registered, Does.Contain(Control.ForegroundProperty));
        }

        [Test]
        public void Remove_TemporaryRule_stops_applying_and_restores_the_default()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .TemporaryRule("hold", _ => true, false, RuleResult.FallThrough, "held");

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.False);
            Assert.That(_engine.ApplyRulesTo("ToolTip.Name"), Is.EqualTo("held"));

            _engine.Remove.TemporaryRule("hold");

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.True);
            Assert.That(_engine.ApplyRulesTo("ToolTip.Name"), Is.Null);
        }

        [Test]
        public void Chrome_rule_with_a_null_tooltip_does_not_wipe_a_stashed_tooltip()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.Name, _ => true, false, RuleResult.FallThrough, "keep me");
            _engine.Add.Binding(Control.BackgroundProperty)
                .PropertyRule(() => _vm.Name, _ => true, Brushes.Yellow, RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.False);
            Assert.That(_engine.ApplyRulesTo("Background.Name"), Is.EqualTo(Brushes.Yellow));
            Assert.That(_engine.ApplyRulesTo("ToolTip.Name"), Is.EqualTo("keep me"));
        }

        [Test]
        public void ApplyRulesTo_returns_null_for_a_missing_or_empty_key()
        {
            Assert.That(_engine.ApplyRulesTo(null), Is.Null);
            Assert.That(_engine.ApplyRulesTo(string.Empty), Is.Null);
        }

        [Test]
        public void WithThisTypeBinding_is_ordinal_and_null_safe()
        {
            var rule = new Rule(UIElement.IsEnabledProperty, _ => true, false, true, null);
            Assert.That(rule.WithThisTypeBinding("IsEnabled"), Is.True);
            Assert.That(rule.WithThisTypeBinding("isenabled"), Is.True);
            Assert.That(rule.WithThisTypeBinding(null), Is.False);
        }

        [Test]
        public void ResultIfNoMatch_null_is_returned_not_treated_as_fallthrough()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.Name, _ => false, false, null);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.Null);
        }

        [Test]
        public void ResultIfNoMatch_FallThrough_uses_the_registered_default()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.Name, _ => false, false, RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.True);
        }

        [Test]
        public void Create_throws_when_the_view_model_is_null()
        {
            Assert.That(() => SigilEngine.Create(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void ApplyRulesTo_key_without_a_dot_uses_an_empty_view_model_path()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(p => p == string.Empty, false, RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled"), Is.False);
            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.True);
        }

        [Test]
        public void TemporaryRule_replaces_the_same_key()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .TemporaryRule("hold", _ => true, false, RuleResult.FallThrough);
            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.False);

            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .TemporaryRule("hold", _ => true, true, RuleResult.FallThrough);
            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.True);
        }

        [Test]
        public void Remove_TemporaryRule_missing_key_is_a_no_op()
        {
            Assert.That(() => _engine.Remove.TemporaryRule("missing"), Throws.Nothing);
            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.True);
        }

        [Test]
        public void Evaluation_order_is_temporary_then_global_then_property_then_default()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.Name, _ => true, false, RuleResult.FallThrough);
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => true, "global", RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.EqualTo("global"));

            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .TemporaryRule("hold", _ => true, "temp", RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.EqualTo("temp"));

            _engine.Remove.TemporaryRule("hold");
            _engine.Remove.TemporaryRule("unused");

            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => false, "never", RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.EqualTo("global"));
        }

        [Test]
        public void AllPropertiesRule_no_match_value_blocks_later_property_rules()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.Name, _ => true, "property", RuleResult.FallThrough);
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => false, "matched", false);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.False);
        }

        [Test]
        public void PropertyRule_falls_through_to_the_next_property_rule()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.Name, _ => false, "first", RuleResult.FallThrough);
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.Name, _ => true, "second", RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.EqualTo("second"));
        }

        [Test]
        public void PropertyRule_skips_StateChangeExempt()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.ExemptName, _ => true, false, RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.ExemptName"), Is.True);
        }

        [Test]
        public void AddPropertyDefault_replaces_the_previous_default()
        {
            _engine.AddPropertyDefault(UIElement.IsEnabledProperty, "first");
            _engine.AddPropertyDefault(UIElement.IsEnabledProperty, "second");

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.EqualTo("second"));
            Assert.That(_engine.GetDefaultBindings(), Does.Contain("IsEnabled"));
        }

        [Test]
        public void ApplyRulesTo_returns_null_when_no_default_is_registered()
        {
            Assert.That(_engine.ApplyRulesTo("Background.Name"), Is.Null);
        }

        [Test]
        public void TemporaryRule_fallthrough_reaches_the_all_properties_rule()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .AllPropertiesRule(_ => true, "global", RuleResult.FallThrough);
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .TemporaryRule("hold", _ => false, "temp", RuleResult.FallThrough);

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.EqualTo("global"));
        }

        [Test]
        public void Obsolete_GlobalRule_matches_AllPropertiesRule()
        {
#pragma warning disable CS0618
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .GlobalRule(_ => true, "legacy", RuleResult.FallThrough);
#pragma warning restore CS0618

            Assert.That(_engine.ApplyRulesTo("IsEnabled.Name"), Is.EqualTo("legacy"));
        }

        [Test]
        public void Remove_TemporaryRule_raises_Item()
        {
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .TemporaryRule("hold", _ => true, false, RuleResult.FallThrough);
            _vm.Notifications.Clear();

            _engine.Remove.TemporaryRule("hold");

            Assert.That(_vm.Notifications, Does.Contain("Item[]"));
        }

        [Test]
        public void Dispose_unhooks_nested_property_subscriptions()
        {
            _vm.Address = new Address { City = "Austin" };
            _engine.Add.Binding(UIElement.IsEnabledProperty)
                .PropertyRule(() => _vm.Address.City, _ => true, false, RuleResult.FallThrough);

            _engine.Dispose();
            _vm.Notifications.Clear();
            _vm.Address!.City = "Dallas";

            Assert.That(_vm.Notifications, Does.Not.Contain("Item[]"));
        }

        [Test]
        public void StateChangeExempt_attribute_usage_still_resolves_to_StateChangeExemptAttribute()
        {
            var property = typeof(TestViewModel).GetProperty(nameof(TestViewModel.ExemptName));
            Assert.That(property, Is.Not.Null);
            var attr = property!.GetCustomAttributes(typeof(StateChangeExemptAttribute), true);

            Assert.That(attr, Is.Not.Empty);
        }

        [Test]
        public void RuleResult_FallThrough_is_a_stable_sentinel()
        {
            Assert.That(RuleResult.FallThrough, Is.Not.Null);
            Assert.That(ReferenceEquals(RuleResult.FallThrough, RuleResult.FallThrough), Is.True);
            Assert.That(RuleResult.FallThrough.ToString(), Is.EqualTo("FallThrough"));
        }
    }
}
