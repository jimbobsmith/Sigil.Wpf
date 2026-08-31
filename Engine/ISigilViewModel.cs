using System.ComponentModel;

namespace Sigil.Wpf.Engine
{
    /// <summary>
    /// View model contract the rules engine and auto-binder require.
    /// </summary>
    public interface ISigilViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The engine that evaluates indexer keys. Typically created in the constructor
        /// with <c>SigilEngine.Create(this)</c>.
        /// </summary>
        IRuleEngine Engine { get; set; }

        /// <summary>
        /// WPF binds keys as <c>{Binding [IsEnabled.Amount]}</c>.
        /// Typical implementation: <c>return Engine.ApplyRulesTo(key);</c>
        /// With <see cref="Sigil.Wpf.AutoBinder.AutoBind"/>, XAML only
        /// needs the value binding; the engine attaches the rest.
        /// </summary>
        object? this[string key] { get; }

        /// <summary>
        /// Raises <see cref="INotifyPropertyChanged.PropertyChanged"/>.
        /// The engine calls this with <c>Item[]</c> when rules need the UI to refresh.
        /// Typical implementations also raise <c>Item[]</c> after the named property
        /// so setters only call <c>RaisePropertyChanged(nameof(Status))</c>.
        /// </summary>
        void RaisePropertyChanged(string propertyName);
    }
}
