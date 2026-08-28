using System;
using System.Windows;

namespace Sigil.Wpf
{
    /// <summary>
    /// One rule: a dependency property, a match function, and the values to return.
    /// </summary>
    internal class Rule
    {
        public Rule(
            DependencyProperty binding,
            Func<string, bool> matchFunction,
            object? resultIfMatch,
            object? resultIfNoMatch,
            string? tooltip)
        {
            Binding = binding;
            Match = matchFunction;
            ResultIfMatch = resultIfMatch;
            ResultIfNoMatch = resultIfNoMatch;
            Tooltip = tooltip;
        }

        /// <summary>The control dependency property this rule applies to.</summary>
        public DependencyProperty Binding { get; private set; }

        /// <summary>
        /// Receives the view-model property name. Return true to use <see cref="ResultIfMatch"/>.
        /// </summary>
        public Func<string, bool> Match { get; private set; }

        public object? ResultIfMatch { get; private set; }

        /// <summary>
        /// Value when <see cref="Match"/> is false.
        /// <see cref="RuleResult.FallThrough"/> means skip to the next rule.
        /// </summary>
        public object? ResultIfNoMatch { get; private set; }

        /// <summary>
        /// Optional tooltip stored for this view-model property when the rule matches.
        /// Rules with a null tooltip do not overwrite a tooltip set by another rule.
        /// </summary>
        public string? Tooltip { get; private set; }

        /// <summary>
        /// True when <paramref name="key"/> is this rule's dependency property name.
        /// Comparison is ordinal and case-insensitive.
        /// </summary>
        public bool WithThisTypeBinding(string? key)
        {
            if (key == null || Binding == null)
                return false;
            return string.Equals(Binding.Name, key, StringComparison.OrdinalIgnoreCase);
        }
    }
}
