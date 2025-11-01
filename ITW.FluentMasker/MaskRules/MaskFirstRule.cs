using System;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Backward-compatible alias for MaskStartRule.
    /// Masks the first N characters of a string.
    /// </summary>
    /// <remarks>
    /// <para>This class is maintained for backward compatibility with existing code.</para>
    /// <para>New code should use <see cref="MaskStartRule"/> instead.</para>
    /// <para>Functionally identical to MaskStartRule.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Legacy code (still works):
    /// var rule = new MaskFirstRule(2, "*");
    /// rule.Apply("JohnDoe");  // Returns "**hnDoe"
    ///
    /// // Recommended for new code:
    /// var newRule = new MaskStartRule(2, "*");
    /// newRule.Apply("JohnDoe");  // Returns "**hnDoe" (same result)
    /// </code>
    /// </example>
    [Obsolete("Use MaskStartRule instead. This class is maintained for backward compatibility only.")]
    public class MaskFirstRule : MaskStartRule
    {
        /// <summary>
        /// Initializes a new instance of the MaskFirstRule class.
        /// </summary>
        /// <param name="count">Number of characters to mask from the start.</param>
        /// <param name="mask">Character to use for masking (default is "*").</param>
        public MaskFirstRule(int count, string mask = "*") : base(count, mask)
        {
        }
    }
}
