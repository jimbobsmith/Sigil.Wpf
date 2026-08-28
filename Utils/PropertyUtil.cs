#region

using System.Windows;

#endregion

namespace Sigil.Wpf.Utils
{
    internal static class PropertyUtil
    {
        internal static string DependencyPropertyName(DependencyProperty property)
        {
            return property.Name;
        }
    }
}