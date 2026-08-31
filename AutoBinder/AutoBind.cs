using System.Windows;

namespace Sigil.Wpf.AutoBinder
{
    /// <summary>
    /// When enabled on a container, value bindings (e.g. <c>Text="{Binding Amount}"</c>)
    /// get indexer bindings for every DP the view-model's rule engine has registered.
    /// </summary>
    public static class AutoBind
    {
        /// <summary>
        /// When true, walks the element's tree on load and data-context change
        /// and attaches indexer bindings for every registered dependency property.
        /// </summary>
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(AutoBind),
            new PropertyMetadata(false, OnEnabledChanged));

        /// <summary>Enables or disables auto-bind on <paramref name="element"/>.</summary>
        public static void SetEnabled(DependencyObject element, bool value)
        {
            element.SetValue(EnabledProperty, value);
        }

        /// <summary>Gets whether auto-bind is enabled on <paramref name="element"/>.</summary>
        public static bool GetEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(EnabledProperty);
        }

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null)
                return;

            if ((bool)e.NewValue)
            {
                element.Loaded += OnLoaded;
                element.DataContextChanged += OnDataContextChanged;
                DependencyBinder.ApplyTo(element);
            }
            else
            {
                element.Loaded -= OnLoaded;
                element.DataContextChanged -= OnDataContextChanged;
            }
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null)
                DependencyBinder.ApplyTo(element);
        }

        private static void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null)
                DependencyBinder.ApplyTo(element);
        }
    }
}
