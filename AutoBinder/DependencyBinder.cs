#region

using Sigil.Wpf.Engine;
using Sigil.Wpf.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

// ReSharper disable ArrangeAccessorOwnerBody

#endregion

namespace Sigil.Wpf.AutoBinder
{
    /// <summary>
    /// Attaches indexer bindings from a value-binding path and the engine's registered
    /// dependency properties. Used by <see cref="AutoBind"/>.
    /// </summary>
    public class DependencyBinder : IAutoBinder
    {
        private static readonly DependencyProperty[] ValueProperties =
        {
            Selector.SelectedItemProperty,
            Selector.SelectedValueProperty,
            RangeBase.ValueProperty,
            TextBox.TextProperty,
            ComboBox.TextProperty
        };

        private static readonly HashSet<string> SkipAutoBind =
            new HashSet<string> { "Visibility", "Width", "Height", "MinWidth", "MinHeight", "MaxWidth", "MaxHeight", "Margin" };

        private static readonly HashSet<string> WrapperChromeNames =
            new HashSet<string> { "Background", "BorderBrush", "BorderThickness" };

        private readonly IDictionary<string, DependencyProperty> _specificAutoBindingProperties = new Dictionary<string, DependencyProperty>();

        private DependencyBinder()
        {
        }

        /// <summary>
        /// Returns this instance for fluent <c>Create.AddAutoBindingProperty(...).Update</c> calls.
        /// </summary>
        public IAutoBinder Update
        {
            get { return this; }
        }

        /// <summary>
        /// Starts a manual binder. Prefer <see cref="AutoBind"/> on a container.
        /// </summary>
        public static IInitialAutoBinderConfigurator Create
        {
            get { return new DependencyBinder(); }
        }

        #region IAutoBinder Members

        /// <inheritdoc />
        public void UpdateNonInputControlDataBinding<T>(T control, DependencyProperty dependencyProperty)
            where T : Control
        {
            var bindingExpression = control.GetBindingExpression(dependencyProperty);
            if (bindingExpression == null || control.DataContext == null) return;

            var pb = bindingExpression.ParentBinding;
            var path = pb.Path.Path;

            control.SetValue(ToolTipService.ShowOnDisabledProperty, true);

            var newBinding = CopyBinding(pb);
            control.SetBinding(dependencyProperty, newBinding);

            ApplyEngineBindings(control, path);
        }

        /// <inheritdoc />
        public void UpdateInputControlDataBinding<T>(T control, DependencyProperty dependencyProperty,
            bool notifyOnValidationErrors = true) where T : Control
        {
            var bindingExpression = control.GetBindingExpression(dependencyProperty);
            if (bindingExpression == null || control.DataContext == null) return;

            var pb = bindingExpression.ParentBinding;
            var path = pb.Path.Path;

            var newBinding = CopyBinding(pb);
            newBinding.NotifyOnValidationError = notifyOnValidationErrors;
            newBinding.ValidatesOnDataErrors = true;
            newBinding.ValidatesOnExceptions = true;
            newBinding.ValidatesOnNotifyDataErrors = true;
            control.SetBinding(dependencyProperty, newBinding);
            control.SetValue(ToolTipService.ShowOnDisabledProperty, true);

            ApplyEngineBindings(control, path);
        }

        /// <inheritdoc />
        public IAutoBinder AddAutoBindingProperty(DependencyProperty autoBoundProperty)
        {
            var name = PropertyUtil.DependencyPropertyName(autoBoundProperty);
            if (!_specificAutoBindingProperties.ContainsKey(name))
            {
                _specificAutoBindingProperties.Add(name, autoBoundProperty);
            }
            return this;
        }

        #endregion

        /// <summary>
        /// Walks <paramref name="root"/> and, for each control with a value binding
        /// (Text, SelectedItem, …), attaches indexer bindings for every DP the engine has registered.
        /// </summary>
        public static void ApplyTo(FrameworkElement? root)
        {
            if (root == null)
                return;

            var vm = FindViewModel(root);
            if (vm == null || vm.Engine == null)
                return;

            foreach (var control in WalkControls(root))
                ApplyToControl(control, vm.Engine);
        }

        /// <summary>
        /// Attaches indexer bindings on <paramref name="control"/> from its value-binding path
        /// and the engine's registered dependency properties.
        /// </summary>
        public static void ApplyToControl(Control? control, IRuleEngine? engine)
        {
            if (control == null || engine == null)
                return;

            var path = FindValueBindingPath(control);
            if (string.IsNullOrEmpty(path))
                return;

            control.SetValue(ToolTipService.ShowOnDisabledProperty, true);

            var wrapper = LogicalTreeHelper.GetParent(control) as Border;
            foreach (var property in engine.GetRegisteredProperties())
            {
                if (wrapper != null && IsWrapperChrome(property))
                    TryAddAutoBinding(property, wrapper, path);
                else
                    TryAddAutoBinding(property, control, path);
            }
        }

        private void ApplyEngineBindings(Control control, string path)
        {
            foreach (var prop in _specificAutoBindingProperties.Values)
                TryAddAutoBinding(prop, control, path);

            var vm = control.DataContext as ISigilViewModel;
            if (vm == null || vm.Engine == null)
                return;

            foreach (var prop in vm.Engine.GetRegisteredProperties())
                TryAddAutoBinding(prop, control, path);
        }

        private static ISigilViewModel? FindViewModel(FrameworkElement element)
        {
            for (var current = element; current != null; current = current.Parent as FrameworkElement)
            {
                var vm = current.DataContext as ISigilViewModel;
                if (vm != null)
                    return vm;
            }

            return element.DataContext as ISigilViewModel;
        }

        internal static IEnumerable<Control> WalkControls(DependencyObject? root)
        {
            if (root == null)
                yield break;

            var seen = new HashSet<DependencyObject>();
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!seen.Add(node))
                    continue;

                var control = node as Control;
                if (control != null)
                    yield return control;

                var queued = false;
                var visual = node as Visual;
                if (visual != null)
                {
                    var count = VisualTreeHelper.GetChildrenCount(node);
                    if (count > 0)
                    {
                        queued = true;
                        for (var i = 0; i < count; i++)
                            queue.Enqueue(VisualTreeHelper.GetChild(node, i));
                    }
                }

                if (queued)
                    continue;

                foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
                    queue.Enqueue(child);
            }
        }

        internal static string? FindValueBindingPath(Control? control)
        {
            if (control == null)
                return null;

            foreach (var property in ValueProperties)
            {
                if (!IsApplicable(control, property))
                    continue;

                var binding = BindingOperations.GetBinding(control, property);
                if (binding == null || binding.Path == null || string.IsNullOrEmpty(binding.Path.Path))
                    continue;

                if (binding.Path.Path.StartsWith("["))
                    continue;

                return binding.Path.Path;
            }

            return FindNamedValuePath(control, "Value");
        }

        private static string? FindNamedValuePath(Control control, string propertyName)
        {
            var descriptor = DependencyPropertyDescriptor.FromName(propertyName, control.GetType(), control.GetType());
            if (descriptor == null)
                return null;

            var binding = BindingOperations.GetBinding(control, descriptor.DependencyProperty);
            if (binding == null || binding.Path == null || string.IsNullOrEmpty(binding.Path.Path))
                return null;

            return binding.Path.Path.StartsWith("[") ? null : binding.Path.Path;
        }

        internal static bool ShouldSkipAutoBind(DependencyProperty? property)
        {
            return property != null && SkipAutoBind.Contains(property.Name);
        }

        private static bool IsWrapperChrome(DependencyProperty? property)
        {
            return property != null && WrapperChromeNames.Contains(property.Name);
        }

        private static void TryAddAutoBinding(DependencyProperty dependencyProperty, FrameworkElement element, string path)
        {
            if (dependencyProperty == null || element == null || ShouldSkipAutoBind(dependencyProperty))
                return;

            var target = ResolveProperty(element, dependencyProperty);
            if (target == null)
                return;

            AddAutoBinding(target, element, path);
        }

        internal static DependencyProperty? ResolveProperty(FrameworkElement? element, DependencyProperty? source)
        {
            if (element == null || source == null)
                return null;

            if (DependencyPropertyDescriptor.FromProperty(source, element.GetType()) != null)
                return source;

            var byName = DependencyPropertyDescriptor.FromName(source.Name, element.GetType(), element.GetType());
            return byName == null ? null : byName.DependencyProperty;
        }

        private static bool IsApplicable(Control control, DependencyProperty dependencyProperty)
        {
            return ResolveProperty(control, dependencyProperty) != null;
        }

        private static void AddAutoBinding(DependencyProperty dependencyProperty, FrameworkElement element, string path)
        {
            if (BindingOperations.GetBindingExpression(element, dependencyProperty) != null)
                return;

            var bindingName = PropertyUtil.DependencyPropertyName(dependencyProperty);
            var newBinding = new Binding($"[{bindingName}.{path}]") {Mode = BindingMode.OneWay};
            element.SetBinding(dependencyProperty, newBinding);
        }

        internal static Binding CopyBinding(Binding? oldBinding)
        {
            if (oldBinding == null)
                return new Binding();

            var newBinding = new Binding
            {
                Path = oldBinding.Path,
                Mode = oldBinding.Mode,
                UpdateSourceTrigger = oldBinding.UpdateSourceTrigger,
                Converter = oldBinding.Converter,
                ConverterParameter = oldBinding.ConverterParameter,
                ConverterCulture = oldBinding.ConverterCulture,
                StringFormat = oldBinding.StringFormat,
                TargetNullValue = oldBinding.TargetNullValue,
                FallbackValue = oldBinding.FallbackValue,
                NotifyOnValidationError = oldBinding.NotifyOnValidationError,
                NotifyOnSourceUpdated = oldBinding.NotifyOnSourceUpdated,
                NotifyOnTargetUpdated = oldBinding.NotifyOnTargetUpdated,
                ValidatesOnDataErrors = oldBinding.ValidatesOnDataErrors,
                ValidatesOnExceptions = oldBinding.ValidatesOnExceptions,
                ValidatesOnNotifyDataErrors = oldBinding.ValidatesOnNotifyDataErrors,
                IsAsync = oldBinding.IsAsync,
                BindingGroupName = oldBinding.BindingGroupName,
                BindsDirectlyToSource = oldBinding.BindsDirectlyToSource,
                Delay = oldBinding.Delay,
                XPath = oldBinding.XPath,
                UpdateSourceExceptionFilter = oldBinding.UpdateSourceExceptionFilter
            };

            if (oldBinding.Source != null)
                newBinding.Source = oldBinding.Source;
            else if (oldBinding.RelativeSource != null)
                newBinding.RelativeSource = oldBinding.RelativeSource;
            else if (!string.IsNullOrEmpty(oldBinding.ElementName))
                newBinding.ElementName = oldBinding.ElementName;

            foreach (var rule in oldBinding.ValidationRules)
                newBinding.ValidationRules.Add(rule);

            return newBinding;
        }
    }
}