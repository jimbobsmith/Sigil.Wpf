using System.Windows;

namespace Sigil.Wpf.AutoBinder
{
    /// <summary>
    /// Optional extra dependency properties to auto-bind in addition to those
    /// registered on the view model's rule engine.
    /// </summary>
    public interface IInitialAutoBinderConfigurator
    {
        /// <summary>
        /// Registers an extra dependency property to auto-bind in addition to
        /// those on the view model's engine.
        /// </summary>
        IAutoBinder AddAutoBindingProperty(DependencyProperty autoBoundProperty);
    }
}
