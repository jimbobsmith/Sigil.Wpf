using System;
using System.Linq.Expressions;

namespace Sigil.Wpf.Engine
{
    /// <summary>
    /// Fluent rule builders for one dependency property. Obtain an instance from
    /// <see cref="IBindingRuleType.Binding"/>.
    /// </summary>
    /// <remarks>
    /// Evaluation order is <see cref="TemporaryRule"/>, then <see cref="AllPropertiesRule"/>,
    /// then <see cref="PropertyRule{TProp}"/>, then the engine default.
    /// <para>
    /// The match function receives the view-model property name
    /// (for example <c>Amount</c>), not the property value. Close over the view model
    /// to read values: <c>_ =&gt; Amount &gt; 1000</c>.
    /// </para>
    /// <para>
    /// Pass <see cref="Sigil.Wpf.RuleResult.FallThrough"/> for <c>resultIfNoMatch</c> to
    /// skip this rule and try the next one. <c>null</c> is a real value.
    /// </para>
    /// </remarks>
    public interface IRuleProvider
    {
        /// <summary>
        /// A rule that can apply to every non-exempt property on this engine
        /// (this view model, not the whole application).
        /// </summary>
        IRuleEngine AllPropertiesRule(Func<string, bool> matchFunction, object? resultIfMatch, object? resultIfNoMatch,
                                      string? tooltip = null);

        /// <summary>
        /// Obsolete name for <see cref="AllPropertiesRule"/>. Same behavior.
        /// </summary>
        [Obsolete("Use AllPropertiesRule. Same behavior: every non-exempt property on this engine.")]
        IRuleEngine GlobalRule(Func<string, bool> matchFunction, object? resultIfMatch, object? resultIfNoMatch,
                               string? tooltip = null);

        /// <summary>
        /// A rule that applies only to the control bound to <paramref name="property"/>.
        /// </summary>
        IRuleEngine PropertyRule<TProp>(Expression<Func<TProp>> property, Func<string, bool> matchFunction,
                                        object? resultIfMatch, object? resultIfNoMatch, string? tooltip = null);

        /// <summary>
        /// A rule that can be added and removed at runtime under <paramref name="key"/>.
        /// Highest priority. Replacing the same key overwrites the previous rule.
        /// </summary>
        IRuleEngine TemporaryRule(string key, Func<string, bool> matchFunction, object? resultIfMatch,
            object? resultIfNoMatch, string? tooltip = null);
    }
}
