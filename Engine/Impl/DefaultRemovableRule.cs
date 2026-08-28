namespace Sigil.Wpf.Engine.Impl
{
    /// <summary>
    /// Default <see cref="IRemovableRule"/> used by <see cref="SigilEngine.Remove"/>.
    /// </summary>
    internal class DefaultRemovableRule : IRemovableRule
    {
        private readonly SigilEngine _engine;

        public DefaultRemovableRule(SigilEngine indexerRulesEngine)
        {
            _engine = indexerRulesEngine;
        }

        public IRuleEngine TemporaryRule(string key)
        {
            _engine.RemoveTemporaryRule(key);
            return _engine;
        }
    }
}
