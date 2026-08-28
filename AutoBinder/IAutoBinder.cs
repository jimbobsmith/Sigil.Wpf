using System.Windows;
using System.Windows.Controls;

namespace Sigil.Wpf.AutoBinder
{
    /// <summary>
    /// Rewrites an existing value binding and attaches indexer bindings for registered
    /// dependency properties. Prefer <see cref="AutoBind"/> on a container.
    /// </summary>
    public interface IAutoBinder : IInitialAutoBinderConfigurator
    {
        /// <summary>
        /// Copies the current binding on <paramref name="dependencyProperty"/>, enables
        /// validation, and attaches engine indexer bindings for the same path.
        /// </summary>
        void UpdateInputControlDataBinding<T>(T control, DependencyProperty dependencyProperty,
                                              bool notifyOnValidationErrors = true) where T : Control;

        /// <summary>
        /// Copies the current binding on <paramref name="dependencyProperty"/> and attaches
        /// engine indexer bindings for the same path.
        /// </summary>
        void UpdateNonInputControlDataBinding<T>(T control, DependencyProperty dependencyProperty)
            where T : Control;
    }
}
