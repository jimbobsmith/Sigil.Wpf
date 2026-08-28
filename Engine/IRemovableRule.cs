namespace Sigil.Wpf.Engine
{
    /// <summary>
    /// Fluent step after <see cref="IRuleEngine.Remove"/>.
    /// </summary>
    public interface IRemovableRule
    {
        /// <summary>
        /// Deletes the temporary rule registered under <paramref name="key"/>.
        /// The rule is removed from the engine, not left inactive.
        /// </summary>
        IRuleEngine TemporaryRule(string key);
    }
}
