using System;
using System.ComponentModel;
using Sigil.Wpf.Engine.Impl;

namespace Sigil.Wpf.Engine
{
    /// <summary>
    /// Base view model that owns a <see cref="SigilEngine"/>, the indexer WPF binds to,
    /// and <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public abstract class SigilViewModel : ISigilViewModel, IDisposable
    {
        /// <summary>
        /// Creates the engine bound to this instance. Keep the view model for the
        /// lifetime of the view and dispose it when the view goes away.
        /// </summary>
        protected SigilViewModel()
        {
            Engine = SigilEngine.Create(this);
        }

        /// <inheritdoc />
        public IRuleEngine Engine { get; set; }

        /// <inheritdoc />
        public object? this[string key] => Engine.ApplyRulesTo(key);

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc />
        /// <remarks>
        /// Notifies <paramref name="propertyName"/>, then <c>Item[]</c>, so setters only need
        /// <c>RaisePropertyChanged(nameof(Status))</c>. Call <see cref="NotifyPropertyChanged"/>
        /// when a derived display property should not refresh indexer bindings.
        /// </remarks>
        public virtual void RaisePropertyChanged(string propertyName)
        {
            NotifyPropertyChanged(propertyName);
            if (!IsIndexerName(propertyName))
                NotifyPropertyChanged("Item[]");
        }

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for <paramref name="propertyName"/> only.
        /// </summary>
        protected void NotifyPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises <c>Item[]</c> so indexer bindings refresh after a rule-relevant change.
        /// </summary>
        protected void NotifyIndexer()
        {
            NotifyPropertyChanged("Item[]");
        }

        private static bool IsIndexerName(string propertyName)
        {
            return string.Equals(propertyName, "Item[]", StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the engine's property-change subscriptions.
        /// </summary>
        /// <param name="disposing">True when called from <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                Engine.Dispose();
        }
    }
}
