using System.Windows;

namespace Sigil.Wpf.Engine
{
    /// <summary>
    /// Fluent step after <see cref="IRuleEngine.Add"/>: choose the dependency property the rule binds to.
    /// </summary>
    public interface IBindingRuleType
    {
        /// <summary>
        /// Continues the fluent chain for <paramref name="property"/>
        /// (for example <see cref="UIElement.IsEnabledProperty"/>).
        /// </summary>
        IRuleProvider Binding(DependencyProperty property);
    }
}
