using System;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Replaces any input value with null.
    /// </summary>
    /// <remarks>
    /// <para>This rule always returns null regardless of the input value.</para>
    /// <para>Useful for completely removing sensitive fields from serialized output.</para>
    /// <para>When used with AbstractMasker, the field will be omitted from JSON output or set to null.</para>
    /// <para>This is the simplest and most complete form of data masking.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rule = new NullOutRule();
    ///
    /// // All inputs result in null:
    /// rule.Apply("SensitiveData");      // Returns null
    /// rule.Apply("AnyValue");           // Returns null
    /// rule.Apply("");                   // Returns null
    /// rule.Apply(null);                 // Returns null
    ///
    /// // Typical usage in a masker:
    /// masker.MaskFor(x => x.SSN, new NullOutRule());  // SSN field will be null in output
    /// </code>
    /// </example>
    public class NullOutRule : IStringMaskRule
    {
        /// <summary>
        /// Applies the null-out rule to the input string.
        /// </summary>
        /// <param name="input">The string to replace with null (ignored)</param>
        /// <returns>Always returns null</returns>
        public string Apply(string input)
        {
            return null;
        }
    }
}
