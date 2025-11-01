using System;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Backward-compatible alias for MaskEndRule.
    /// Masks the last N characters of a string.
    /// </summary>
    /// <remarks>
    /// <para>This class is maintained for backward compatibility with existing code.</para>
    /// <para>New code should use <see cref="MaskEndRule"/> instead.</para>
    /// <para>Functionally identical to MaskEndRule.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Legacy code (still works):
    /// var rule = new MaskLastRule(3, "*");
    /// rule.Apply("JohnDoe");  // Returns "John***"
    ///
    /// // Recommended for new code:
    /// var newRule = new MaskEndRule(3, "*");
    /// newRule.Apply("JohnDoe");  // Returns "John***" (same result)
    /// </code>
    /// </example>
    [Obsolete("Use MaskEndRule instead. This class is maintained for backward compatibility only.")]
    public class MaskLastRule : MaskEndRule
    {
        /// <summary>
        /// Initializes a new instance of the MaskLastRule class.
        /// </summary>
        /// <param name="count">Number of characters to mask from the end.</param>
        /// <param name="mask">Character to use for masking (default is "*").</param>
        public MaskLastRule(int count, string mask = "*") : base(count, mask)
        {
        }
    }
}
