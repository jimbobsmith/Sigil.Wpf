using System;

namespace Sigil.Wpf.Engine
{
    /// <summary>
    /// Marks a view-model property that the rules engine must not change.
    /// Temporary, global, and property rules are skipped; the registered default is used.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public sealed class StateChangeExemptAttribute : Attribute
    {
    }
}
