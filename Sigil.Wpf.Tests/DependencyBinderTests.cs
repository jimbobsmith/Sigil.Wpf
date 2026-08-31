using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Sigil.Wpf.AutoBinder;
using Sigil.Wpf.Engine.Impl;
using Sigil.Wpf.Tests.Support;
using NUnit.Framework;

namespace Sigil.Wpf.Tests
{
    [TestFixture]
    public class DependencyBinderTests
    {
        [Test]
        public void CopyBinding_preserves_path_mode_and_validation_rules()
        {
            var original = new Binding("Amount")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                StringFormat = "{}{0:N2}",
                ConverterCulture = CultureInfo.InvariantCulture
            };
            original.ValidationRules.Add(new ExceptionValidationRule());
            original.ValidationRules.Add(new DataErrorValidationRule());

            var copy = DependencyBinder.CopyBinding(original);

            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(copy.Path.Path, Is.EqualTo("Amount"));
            Assert.That(copy.Mode, Is.EqualTo(BindingMode.TwoWay));
            Assert.That(copy.UpdateSourceTrigger, Is.EqualTo(UpdateSourceTrigger.PropertyChanged));
            Assert.That(copy.StringFormat, Is.EqualTo("{}{0:N2}"));
            Assert.That(copy.ConverterCulture, Is.EqualTo(CultureInfo.InvariantCulture));
            Assert.That(copy.ValidationRules.Count, Is.EqualTo(2));
            Assert.That(copy.ValidationRules[0], Is.InstanceOf<ExceptionValidationRule>());
            Assert.That(copy.ValidationRules[1], Is.InstanceOf<DataErrorValidationRule>());
        }

        [Test]
        public void CopyBinding_does_not_throw_on_a_default_binding()
        {
            Assert.That(() => DependencyBinder.CopyBinding(new Binding("Name")), Throws.Nothing);
        }

        [Test]
        public void CopyBinding_returns_an_empty_binding_when_source_is_null()
        {
            var copy = DependencyBinder.CopyBinding(null);
            Assert.That(copy, Is.Not.Null);
            Assert.That(copy.Path, Is.Null);
        }
    }

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DependencyBinderAutoBindTests
    {
        [Test]
        public void ApplyToControl_attaches_indexer_bindings_from_the_value_path()
        {
            var vm = new TestViewModel();
            var engine = (SigilEngine)vm.Engine;
            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
            engine.AddPropertyDefault(Control.BackgroundProperty, Brushes.White);
            engine.AddPropertyDefault(UIElement.VisibilityProperty, Visibility.Visible);

            var box = new TextBox();
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(TestViewModel.Name)));
            box.DataContext = vm;

            DependencyBinder.ApplyToControl(box, engine);

            Assert.That(BindingOperations.GetBinding(box, UIElement.IsEnabledProperty).Path.Path,
                Is.EqualTo("[IsEnabled.Name]"));
            Assert.That(BindingOperations.GetBinding(box, Control.BackgroundProperty).Path.Path,
                Is.EqualTo("[Background.Name]"));
            Assert.That(BindingOperations.GetBinding(box, UIElement.VisibilityProperty), Is.Null);
            Assert.That(ToolTipService.GetShowOnDisabled(box), Is.True);
        }

        [Test]
        public void ApplyToControl_does_not_replace_an_existing_binding()
        {
            var vm = new TestViewModel();
            var engine = (SigilEngine)vm.Engine;
            engine.AddPropertyDefault(Control.BackgroundProperty, Brushes.White);

            var box = new TextBox();
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(TestViewModel.Name)));
            box.SetBinding(Control.BackgroundProperty, new Binding("SomeBrush"));
            box.DataContext = vm;

            DependencyBinder.ApplyToControl(box, engine);

            Assert.That(BindingOperations.GetBinding(box, Control.BackgroundProperty).Path.Path,
                Is.EqualTo("SomeBrush"));
        }

        [Test]
        public void ApplyToControl_uses_a_Value_binding_path()
        {
            var vm = new TestViewModel();
            var engine = (SigilEngine)vm.Engine;
            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);

            var slider = new Slider();
            slider.SetBinding(System.Windows.Controls.Primitives.RangeBase.ValueProperty,
                new Binding(nameof(TestViewModel.Name)));
            slider.DataContext = vm;

            DependencyBinder.ApplyToControl(slider, engine);

            Assert.That(BindingOperations.GetBinding(slider, UIElement.IsEnabledProperty).Path.Path,
                Is.EqualTo("[IsEnabled.Name]"));
        }

        [Test]
        public void ApplyTo_walks_the_tree_and_paints_a_wrapping_border()
        {
            var vm = new TestViewModel();
            var engine = (SigilEngine)vm.Engine;
            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);
            engine.AddPropertyDefault(Control.BackgroundProperty, Brushes.White);
            engine.AddPropertyDefault(Control.BorderBrushProperty, Brushes.Black);

            var box = new TextBox();
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(TestViewModel.Name)));

            var border = new Border { Child = box };
            var panel = new StackPanel { DataContext = vm };
            panel.Children.Add(border);

            DependencyBinder.ApplyTo(panel);

            Assert.That(BindingOperations.GetBinding(box, UIElement.IsEnabledProperty).Path.Path,
                Is.EqualTo("[IsEnabled.Name]"));
            Assert.That(BindingOperations.GetBinding(box, Control.BackgroundProperty), Is.Null);
            Assert.That(BindingOperations.GetBinding(border, Border.BackgroundProperty).Path.Path,
                Is.EqualTo("[Background.Name]"));
            Assert.That(BindingOperations.GetBinding(border, Border.BorderBrushProperty).Path.Path,
                Is.EqualTo("[BorderBrush.Name]"));
        }

        [Test]
        public void ApplyTo_uses_the_control_DataContext_engine()
        {
            var parent = new TestViewModel();
            parent.Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);

            var row = new TestViewModel();
            row.Engine.AddPropertyDefault(Control.BackgroundProperty, Brushes.White);

            var box = new TextBox();
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(TestViewModel.Name)));
            box.DataContext = row;

            var panel = new StackPanel { DataContext = parent };
            panel.Children.Add(box);

            DependencyBinder.ApplyTo(panel);

            Assert.That(BindingOperations.GetBinding(box, Control.BackgroundProperty).Path.Path,
                Is.EqualTo("[Background.Name]"));
            Assert.That(BindingOperations.GetBinding(box, UIElement.IsEnabledProperty), Is.Null);
        }

        [Test]
        public void ApplyTo_uses_each_row_DataContext_when_siblings_have_different_engines()
        {
            var parent = new TestViewModel();
            parent.Engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);

            var first = new TestViewModel();
            first.Engine.AddPropertyDefault(Control.BackgroundProperty, Brushes.White);

            var second = new TestViewModel();
            second.Engine.AddPropertyDefault(UIElement.OpacityProperty, 1.0);

            var firstBox = new TextBox { DataContext = first };
            firstBox.SetBinding(TextBox.TextProperty, new Binding(nameof(TestViewModel.Name)));
            var secondBox = new TextBox { DataContext = second };
            secondBox.SetBinding(TextBox.TextProperty, new Binding(nameof(TestViewModel.Name)));

            var panel = new StackPanel { DataContext = parent };
            panel.Children.Add(firstBox);
            panel.Children.Add(secondBox);

            DependencyBinder.ApplyTo(panel);

            Assert.That(BindingOperations.GetBinding(firstBox, Control.BackgroundProperty).Path.Path,
                Is.EqualTo("[Background.Name]"));
            Assert.That(BindingOperations.GetBinding(firstBox, UIElement.OpacityProperty), Is.Null);
            Assert.That(BindingOperations.GetBinding(secondBox, UIElement.OpacityProperty).Path.Path,
                Is.EqualTo("[Opacity.Name]"));
            Assert.That(BindingOperations.GetBinding(secondBox, Control.BackgroundProperty), Is.Null);
        }

        [Test]
        public void ApplyTo_does_nothing_when_root_or_view_model_is_missing()
        {
            Assert.That(() => DependencyBinder.ApplyTo(null), Throws.Nothing);
            Assert.That(() => DependencyBinder.ApplyTo(new StackPanel()), Throws.Nothing);
        }

        [Test]
        public void ApplyToControl_does_nothing_without_a_value_binding()
        {
            var vm = new TestViewModel();
            var engine = (SigilEngine)vm.Engine;
            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);

            var box = new TextBox { DataContext = vm };
            DependencyBinder.ApplyToControl(box, engine);

            Assert.That(BindingOperations.GetBinding(box, UIElement.IsEnabledProperty), Is.Null);
        }

        [Test]
        public void ApplyToControl_skips_an_indexer_value_path()
        {
            var vm = new TestViewModel();
            var engine = (SigilEngine)vm.Engine;
            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);

            var box = new TextBox();
            box.SetBinding(TextBox.TextProperty, new Binding("[IsEnabled.Name]"));
            box.DataContext = vm;

            DependencyBinder.ApplyToControl(box, engine);

            Assert.That(BindingOperations.GetBinding(box, UIElement.IsEnabledProperty), Is.Null);
        }

        [Test]
        public void ApplyToControl_uses_SelectedItem_on_a_combo_box()
        {
            var vm = new TestViewModel();
            var engine = (SigilEngine)vm.Engine;
            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);

            var combo = new ComboBox();
            combo.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(TestViewModel.Name)));
            combo.DataContext = vm;

            DependencyBinder.ApplyToControl(combo, engine);

            Assert.That(BindingOperations.GetBinding(combo, UIElement.IsEnabledProperty).Path.Path,
                Is.EqualTo("[IsEnabled.Name]"));
        }

        [Test]
        public void ShouldSkipAutoBind_covers_layout_properties()
        {
            Assert.That(DependencyBinder.ShouldSkipAutoBind(UIElement.VisibilityProperty), Is.True);
            Assert.That(DependencyBinder.ShouldSkipAutoBind(FrameworkElement.WidthProperty), Is.True);
            Assert.That(DependencyBinder.ShouldSkipAutoBind(UIElement.IsEnabledProperty), Is.False);
            Assert.That(DependencyBinder.ShouldSkipAutoBind(null), Is.False);
        }

        [Test]
        public void ResolveProperty_is_null_safe_and_matches_by_name()
        {
            Assert.That(DependencyBinder.ResolveProperty(null, UIElement.IsEnabledProperty), Is.Null);
            Assert.That(DependencyBinder.ResolveProperty(new TextBox(), null), Is.Null);
            Assert.That(DependencyBinder.ResolveProperty(new TextBox(), UIElement.IsEnabledProperty),
                Is.EqualTo(UIElement.IsEnabledProperty));
        }

        [Test]
        public void FindValueBindingPath_is_null_for_a_null_control()
        {
            Assert.That(DependencyBinder.FindValueBindingPath(null), Is.Null);
        }

        [Test]
        public void CopyBinding_preserves_source_and_element_name()
        {
            var source = new TestViewModel();
            var withSource = new Binding("Name") { Source = source };
            var copiedSource = DependencyBinder.CopyBinding(withSource);
            Assert.That(copiedSource.Source, Is.SameAs(source));

            var withElement = new Binding("Name") { ElementName = "AmountBox" };
            var copiedElement = DependencyBinder.CopyBinding(withElement);
            Assert.That(copiedElement.ElementName, Is.EqualTo("AmountBox"));
        }

        [Test]
        public void UpdateInputControlDataBinding_attaches_engine_indexers()
        {
            var vm = new TestViewModel();
            var engine = (SigilEngine)vm.Engine;
            engine.AddPropertyDefault(UIElement.IsEnabledProperty, true);

            var box = new TextBox { DataContext = vm };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(TestViewModel.Name)));

            var binder = DependencyBinder.Create.AddAutoBindingProperty(Control.BackgroundProperty);
            binder.UpdateInputControlDataBinding(box, TextBox.TextProperty);

            Assert.That(BindingOperations.GetBinding(box, UIElement.IsEnabledProperty).Path.Path,
                Is.EqualTo("[IsEnabled.Name]"));
            Assert.That(box.GetBindingExpression(TextBox.TextProperty).ParentBinding.ValidatesOnNotifyDataErrors,
                Is.True);
        }

        [Test]
        public void AutoBind_Enabled_get_set_round_trips()
        {
            var panel = new StackPanel();
            Assert.That(AutoBind.GetEnabled(panel), Is.False);
            AutoBind.SetEnabled(panel, true);
            Assert.That(AutoBind.GetEnabled(panel), Is.True);
            AutoBind.SetEnabled(panel, false);
            Assert.That(AutoBind.GetEnabled(panel), Is.False);
        }

        [Test]
        public void AutoBind_Enabled_applies_the_elements_own_DataContext()
        {
            var row = new TestViewModel();
            row.Engine.AddPropertyDefault(Control.BackgroundProperty, Brushes.White);

            var box = new TextBox { DataContext = row };
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(TestViewModel.Name)));

            AutoBind.SetEnabled(box, true);

            Assert.That(BindingOperations.GetBinding(box, Control.BackgroundProperty).Path.Path,
                Is.EqualTo("[Background.Name]"));
        }

        [Test]
        public void AutoBind_DataContextChanged_applies_when_a_generated_cell_receives_its_row()
        {
            var row = new TestViewModel();
            row.Engine.AddPropertyDefault(Control.BackgroundProperty, Brushes.White);

            var box = new TextBox();
            box.SetBinding(TextBox.TextProperty, new Binding(nameof(TestViewModel.Name)));
            AutoBind.SetEnabled(box, true);

            Assert.That(BindingOperations.GetBinding(box, Control.BackgroundProperty), Is.Null);

            box.DataContext = row;

            Assert.That(BindingOperations.GetBinding(box, Control.BackgroundProperty).Path.Path,
                Is.EqualTo("[Background.Name]"));
        }
    }
}
