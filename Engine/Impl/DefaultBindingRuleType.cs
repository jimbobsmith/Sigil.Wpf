using System.Windows;

namespace Sigil.Wpf.Engine.Impl
{
    /// <summary>
    /// Default <see cref="IBindingRuleType"/> used by <see cref="SigilEngine.Add"/>.
    /// </summary>
    internal class DefaultBindingRuleType : IBindingRuleType
    {
        private readonly SigilEngine _engine;

        public DefaultBindingRuleType(SigilEngine engine)
        {
            _engine = engine;
        }

        public IRuleProvider Binding(DependencyProperty property)
        {
            return new DefaultRuleProvider(_engine, property);
        }
    }
}
