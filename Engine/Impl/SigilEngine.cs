#region

using Sigil.Wpf.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;

#endregion

namespace Sigil.Wpf.Engine.Impl
{
    /// <summary>
    /// Evaluates rule keys in the form <c>{DependencyProperty}.{ViewModelPath}</c>
    /// (for example <c>IsEnabled.Amount</c>).
    /// </summary>
    /// <remarks>
    /// Priority is Temporary, then Global, then Property, then the registered default.
    /// <see cref="StateChangeExemptAttribute"/> on the view-model property skips every rule.
    /// <see cref="Sigil.Wpf.RuleResult.FallThrough"/> as <c>resultIfNoMatch</c> skips to the next rule;
    /// <c>null</c> is a real value.
    /// Register a default for every dependency property you bind through the engine.
    /// </remarks>
    public class SigilEngine : IRuleEngine
    {
        #region Constants and Fields

        private readonly List<Rule> _globalRules = new List<Rule>();
        private readonly IDictionary<string, IList<Rule>> _propertyRules = new Dictionary<string, IList<Rule>>();
        private readonly Dictionary<string, Rule> _temporaryRules = new Dictionary<string, Rule>();
        private readonly Dictionary<string, string> _tooltips = new Dictionary<string, string>();
        private readonly Dictionary<string, DependencyProperty> _registeredProperties = new Dictionary<string, DependencyProperty>();
        private readonly List<PropertyChangeSubscription> _propertyChangeSubscriptions = new List<PropertyChangeSubscription>();
        private readonly ISigilViewModel _viewModel;

        #endregion

        #region Constructors and Destructors

        private SigilEngine(ISigilViewModel viewModel)
        {
            _viewModel = viewModel;
            DefaultResult = new Dictionary<string, object?>();
        }

        #endregion

        #region Public Properties

        private Dictionary<string, object?> DefaultResult { get; set; }

        /// <inheritdoc />
        public IBindingRuleType Add
        {
            get { return new DefaultBindingRuleType(this); }
        }

        /// <inheritdoc />
        public IRemovableRule Remove
        {
            get { return new DefaultRemovableRule(this); }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the value for an indexer key such as <c>IsEnabled.Amount</c> or <c>Background.Name</c>.
        /// </summary>
        /// <inheritdoc />
        public object? ApplyRulesTo(string? key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            var dot = key.IndexOf('.');
            string ctrlProperty;
            string vmProperty;
            if (dot < 0)
            {
                ctrlProperty = key;
                vmProperty = string.Empty;
            }
            else
            {
                ctrlProperty = key.Substring(0, dot);
                vmProperty = key.Substring(dot + 1);
            }

            foreach (var rule in _temporaryRules.Values.Where(rs => rs.WithThisTypeBinding(ctrlProperty)))
            {
                var customAttributes = GetCustomAttributes(vmProperty);
                if (customAttributes.Any(rs => rs is StateChangeExemptAttribute))
                    continue;

                if (rule.Match(vmProperty))
                {
                    RememberToolTip(vmProperty, rule);
                    return rule.ResultIfMatch;
                }

                ForgetToolTip(vmProperty, rule);

                if (!ReferenceEquals(rule.ResultIfNoMatch, RuleResult.FallThrough))
                    return rule.ResultIfNoMatch;
            }

            foreach (var rule in _globalRules.Where(rs => rs.WithThisTypeBinding(ctrlProperty)))
            {
                var customAttributes = GetCustomAttributes(vmProperty);
                if (customAttributes.Any(rs => rs is StateChangeExemptAttribute))
                    continue;

                if (rule.Match(vmProperty))
                {
                    RememberToolTip(vmProperty, rule);
                    return rule.ResultIfMatch;
                }

                ForgetToolTip(vmProperty, rule);

                if (!ReferenceEquals(rule.ResultIfNoMatch, RuleResult.FallThrough))
                    return rule.ResultIfNoMatch;
            }

            foreach (var kv in _propertyRules.Where(rs => rs.Key == vmProperty))
            {
                var customAttributes = GetCustomAttributes(kv.Key);
                if (customAttributes.Any(rs => rs is StateChangeExemptAttribute))
                    continue;

                var rules = kv.Value;
                foreach (var rule in rules.Where(rs => rs.WithThisTypeBinding(ctrlProperty)))
                {
                    if (rule.Match(vmProperty))
                    {
                        RememberToolTip(vmProperty, rule);
                        return rule.ResultIfMatch;
                    }

                    ForgetToolTip(vmProperty, rule);

                    if (!ReferenceEquals(rule.ResultIfNoMatch, RuleResult.FallThrough))
                        return rule.ResultIfNoMatch;
                }
            }

            if (key.StartsWith("ToolTip"))
            {
                string? tooltip;
                if (_tooltips.TryGetValue(vmProperty, out tooltip))
                    return tooltip;

                return null;
            }

            if (DefaultResult.ContainsKey(ctrlProperty))
                return DefaultResult[ctrlProperty];

            return null;
        }

        private object[] GetCustomAttributes(string vmProperty)
        {
            if (string.IsNullOrEmpty(vmProperty))
                return new object[0];

            PropertyInfo? prop = null;
            var currentType = _viewModel.GetType();
            object? currentValue = _viewModel;
            foreach (var segment in vmProperty.Split('.'))
            {
                prop = currentType.GetProperty(segment);
                if (prop == null)
                    return new object[0];

                if (currentValue != null)
                    currentValue = prop.GetValue(currentValue, null);

                currentType = prop.PropertyType;
            }

            return prop == null ? new object[0] : prop.GetCustomAttributes(true);
        }

        /// <inheritdoc />
        public IRuleEngine AddPropertyDefault(DependencyProperty property, object? value)
        {
            var name = PropertyUtil.DependencyPropertyName(property);
            if (DefaultResult.ContainsKey(name))
                DefaultResult.Remove(name);

            DefaultResult.Add(name, value);
            RegisterProperty(property);
            return this;
        }

        /// <inheritdoc />
        public IList<string> GetDefaultBindings()
        {
            return DefaultResult.Keys.ToList();
        }

        /// <inheritdoc />
        public IList<DependencyProperty> GetRegisteredProperties()
        {
            return _registeredProperties.Values.ToList();
        }

        private void RegisterProperty(DependencyProperty? property)
        {
            if (property == null)
                return;
            _registeredProperties[PropertyUtil.DependencyPropertyName(property)] = property;
        }

        private void RememberToolTip(string vmProperty, Rule rule)
        {
            if (rule.Tooltip == null)
                return;
            _tooltips[vmProperty] = rule.Tooltip;
        }

        private void ForgetToolTip(string vmProperty, Rule rule)
        {
            if (rule.Tooltip == null)
                return;
            if (_tooltips.ContainsKey(vmProperty))
                _tooltips.Remove(vmProperty);
        }

        private void ForgetToolTipsFor(Rule rule)
        {
            if (rule.Tooltip == null)
                return;

            var stale = _tooltips
                .Where(kv => kv.Value == rule.Tooltip)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var key in stale)
                _tooltips.Remove(key);
        }

        private void AttachPropertyChangeWatchers(string propName)
        {
            object? current = _viewModel;
            var segments = propName.Split('.');
            for (var i = 0; i < segments.Length; i++)
            {
                if (current == null)
                    break;

                var segment = segments[i];
                var inpc = current as INotifyPropertyChanged;
                if (inpc != null)
                    Subscribe(inpc, segment, propName);

                var prop = current.GetType().GetProperty(segment);
                if (prop == null)
                    break;

                current = prop.GetValue(current, null);
            }
        }

        private void Subscribe(INotifyPropertyChanged source, string watchedProperty, string fullPath)
        {
            if (_propertyChangeSubscriptions.Any(s =>
                ReferenceEquals(s.Source, source)
                && s.WatchedProperty == watchedProperty
                && s.FullPath == fullPath))
                return;

            PropertyChangedEventHandler handler = (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.PropertyName)
                    && !string.Equals(e.PropertyName, watchedProperty, StringComparison.Ordinal))
                    return;

                if (!IsLeafProperty(fullPath, watchedProperty))
                    RebindWatchers(fullPath);

                NotifyIndexerBindings();
            };

            source.PropertyChanged += handler;
            _propertyChangeSubscriptions.Add(new PropertyChangeSubscription
            {
                Source = source,
                WatchedProperty = watchedProperty,
                FullPath = fullPath,
                Handler = handler
            });
        }

        private static bool IsLeafProperty(string fullPath, string watchedProperty)
        {
            var lastDot = fullPath.LastIndexOf('.');
            var leaf = lastDot < 0 ? fullPath : fullPath.Substring(lastDot + 1);
            return string.Equals(leaf, watchedProperty, StringComparison.Ordinal);
        }

        private void RebindWatchers(string fullPath)
        {
            DetachWatchers(fullPath);
            AttachPropertyChangeWatchers(fullPath);
        }

        private void DetachWatchers(string fullPath)
        {
            for (var i = _propertyChangeSubscriptions.Count - 1; i >= 0; i--)
            {
                var sub = _propertyChangeSubscriptions[i];
                if (sub.FullPath != fullPath)
                    continue;
                sub.Source.PropertyChanged -= sub.Handler;
                _propertyChangeSubscriptions.RemoveAt(i);
            }
        }

        private void NotifyIndexerBindings()
        {
            _viewModel.RaisePropertyChanged("Item[]");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            for (var i = _propertyChangeSubscriptions.Count - 1; i >= 0; i--)
            {
                var sub = _propertyChangeSubscriptions[i];
                sub.Source.PropertyChanged -= sub.Handler;
            }
            _propertyChangeSubscriptions.Clear();
        }

        private sealed class PropertyChangeSubscription
        {
            public INotifyPropertyChanged Source = null!;
            public string WatchedProperty = null!;
            public string FullPath = null!;
            public PropertyChangedEventHandler Handler = null!;
        }

        /// <summary>
        /// Creates an engine bound to <paramref name="viewModel"/>. Keep the instance for the
        /// lifetime of the view model and dispose it when the view model goes away.
        /// </summary>
        public static SigilEngine Create(ISigilViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException("viewModel");
            return new SigilEngine(viewModel);
        }

        internal SigilEngine AddGlobalRule(
            DependencyProperty binding,
            Func<string, bool> matchFunction,
            object? resultIfMatch,
            object? resultIfNoMatch, string? tooltip = null)
        {
            var rule = new Rule(binding, matchFunction, resultIfMatch, resultIfNoMatch, tooltip);
            RegisterProperty(binding);
            _globalRules.Add(rule);
            return this;
        }


        internal SigilEngine AddPropertyRule<TProp>(Expression<Func<TProp>> property, Rule rule)
        {
            var propName = FullPath(property);

            AttachPropertyChangeWatchers(propName);
            RegisterProperty(rule.Binding);

            if (_propertyRules.ContainsKey(propName))
            {
                var value = _propertyRules[propName];
                value.Add(rule);
            }
            else
            {
                _propertyRules.Add(propName, new List<Rule> { rule });
            }
            return this;
        }

        private string FullPath<T>(Expression<Func<T>> expr)
        {
            var fullPath = string.Empty;
            MemberExpression? me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    var ue = expr.Body as UnaryExpression;
                    me = ((ue != null) ? ue.Operand : null) as MemberExpression;
                    break;
                default:
                    me = expr.Body as MemberExpression;
                    break;
            }

            while (me != null)
            {
                string propertyName = me.Member.Name;

                fullPath = string.IsNullOrWhiteSpace(fullPath) ? propertyName : propertyName + "." + fullPath;

                if (IsViewModelInstance(me.Expression))
                    break;

                me = me.Expression as MemberExpression;
            }
            return fullPath;
        }

        private bool IsViewModelInstance(System.Linq.Expressions.Expression? expression)
        {
            if (expression == null)
                return false;

            try
            {
                var value = System.Linq.Expressions.Expression.Lambda(expression).Compile().DynamicInvoke();
                return ReferenceEquals(value, _viewModel);
            }
            catch
            {
                return false;
            }
        }
        internal SigilEngine InjectTemporaryRule(string key, Rule rule)
        {
            if (_temporaryRules.ContainsKey(key))
                _temporaryRules.Remove(key);

            _temporaryRules.Add(key, rule);
            RegisterProperty(rule.Binding);
            return this;
        }

        /// <summary>
        /// Deletes the temporary rule registered under <paramref name="key"/>.
        /// </summary>
        public SigilEngine RemoveTemporaryRule(string key)
        {
            Rule? rule;
            if (!_temporaryRules.TryGetValue(key, out rule))
                return this;

            _temporaryRules.Remove(key);
            ForgetToolTipsFor(rule);
            NotifyIndexerBindings();
            return this;
        }

        #endregion
    }
}