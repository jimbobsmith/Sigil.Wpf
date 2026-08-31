using System;
using System.Collections.Generic;
using System.Windows;

namespace Sigil.Wpf.Engine
{
    /// <summary>
    /// Registers rules and evaluates indexer keys such as <c>IsEnabled.Amount</c>.
    /// Dispose the engine when the view model goes away so nested
    /// <see cref="System.ComponentModel.INotifyPropertyChanged"/> subscriptions are released.
    /// </summary>
    public interface IRuleEngine : IDisposable
    {
        /// <summary>
        /// Starts a fluent add: <c>engine.Add.Binding(UIElement.IsEnabledProperty).AllPropertiesRule(...)</c>.
        /// </summary>
        IBindingRuleType Add { get; }

        /// <summary>
        /// Starts a fluent remove: <c>engine.Remove.TemporaryRule("hold")</c>.
        /// The temporary rule is deleted, not merely deactivated.
        /// </summary>
        IRemovableRule Remove { get; }

        /// <summary>
        /// Evaluates <paramref name="key"/> in the form <c>{DependencyProperty}.{ViewModelPath}</c>.
        /// </summary>
        object? ApplyRulesTo(string? key);

        /// <summary>
        /// Sets the fall-through value when no rule matches for this dependency property.
        /// Also registers the property for <see cref="Sigil.Wpf.AutoBinder.AutoBind"/>.
        /// </summary>
        IRuleEngine AddPropertyDefault(DependencyProperty property, object? value);

        /// <summary>
        /// Names of dependency properties that have a default.
        /// </summary>
        IList<string> GetDefaultBindings();

        /// <summary>
        /// Dependency properties the engine knows about — defaults plus every rule binding.
        /// Used to attach indexer bindings from a normal value path (for example Amount → [Background.Amount]).
        /// </summary>
        IList<DependencyProperty> GetRegisteredProperties();
    }
}
