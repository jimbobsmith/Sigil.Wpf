namespace Sigil.Wpf
{
    /// <summary>
    /// Sentinel values for rule results.
    /// </summary>
    public static class RuleResult
    {
        /// <summary>
        /// Pass as <c>resultIfNoMatch</c> to skip this rule and try the next one.
        /// <c>null</c> is a real value — it does not mean fall through.
        /// </summary>
        public static readonly object FallThrough = new FallThroughSentinel();

        private sealed class FallThroughSentinel
        {
            public override string ToString()
            {
                return "FallThrough";
            }
        }
    }
}
