#region

using System;
using System.Linq.Expressions;
using System.Windows;

#endregion

namespace Sigil.Wpf.Engine.Impl
{
    /// <summary>
    /// Default <see cref="IRuleProvider"/> used by <see cref="DefaultBindingRuleType.Binding"/>.
    /// </summary>
    internal class DefaultRuleProvider : IRuleProvider
    {
        #region Constructors and Destructors

        public DefaultRuleProvider(SigilEngine engine, DependencyProperty dependencyProperty)
        {
            Engine = engine;
            DependencyProperty = dependencyProperty;
        }

        #endregion

        private SigilEngine Engine { get; set; }

        private DependencyProperty DependencyProperty { get; set; }

        #region Public Methods

        /// <inheritdoc />
        public IRuleEngine GlobalRule(
            Func<string, bool> matchFunction, object? resultIfMatch, object? resultIfNoMatch, string? tooltip = null)
        {
            Engine.AddGlobalRule(DependencyProperty, matchFunction, resultIfMatch, resultIfNoMatch, tooltip);
            return Engine;
        }

        /// <inheritdoc />
        public IRuleEngine PropertyRule<TProp>(Expression<Func<TProp>> property, Func<string, bool> matchFunction,
                                               object? resultIfMatch, object? resultIfNoMatch, string? tooltip = null)
        {
            var rule = new Rule(DependencyProperty, matchFunction, resultIfMatch, resultIfNoMatch, tooltip);
            Engine.AddPropertyRule(property, rule);
            return Engine;
        }

        /// <inheritdoc />
        public IRuleEngine TemporaryRule(string key,
                                         Func<string, bool> matchFunction,
                                         object? resultIfMatch,
                                         object? resultIfNoMatch, string? tooltip = null)
        {
            var rule = new Rule(DependencyProperty, matchFunction, resultIfMatch, resultIfNoMatch, tooltip);
            Engine.InjectTemporaryRule(key, rule);
            return Engine;
        }

        #endregion
    }
}
