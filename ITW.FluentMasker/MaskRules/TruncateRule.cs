using System;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Truncates a string to a maximum length with an optional suffix indicator.
    /// </summary>
    /// <remarks>
    /// <para>If the string length is ≤ maxLength, it is returned unchanged.</para>
    /// <para>If the string length exceeds maxLength, it is truncated and the suffix is appended.</para>
    /// <para>The suffix length is included in the maxLength calculation to ensure total length never exceeds the limit.</para>
    /// <para>Useful for limiting field lengths in UIs, reports, or data exports while indicating truncation.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rule = new TruncateRule(10, "...");
    ///
    /// // String within limit - unchanged:
    /// rule.Apply("Short");  // Returns "Short"
    ///
    /// // String exceeds limit - truncated:
    /// rule.Apply("A very long string");  // Returns "A very lo..."
    ///
    /// // Suffix length is accounted for:
    /// // maxLength=10, suffix="..." (3 chars), so 7 chars + "..." = 10 total
    /// rule.Apply("1234567890");  // Returns "1234567..."
    ///
    /// // Custom suffix:
    /// var ellipsis = new TruncateRule(15, "…");
    /// ellipsis.Apply("This is a longer text");  // Returns "This is a long…"
    ///
    /// // No suffix:
    /// var noSuffix = new TruncateRule(8, "");
    /// noSuffix.Apply("HelloWorld");  // Returns "HelloWor"
    /// </code>
    /// </example>
    public class TruncateRule : IStringMaskRule
    {
        private readonly int _maxLength;
        private readonly string _suffix;

        /// <summary>
        /// Initializes a new instance of the <see cref="TruncateRule"/> class.
        /// </summary>
        /// <param name="maxLength">Maximum allowed length of the output string (including suffix)</param>
        /// <param name="suffix">Suffix to append when truncating (default: "…")</param>
        /// <exception cref="ArgumentException">Thrown when maxLength is negative</exception>
        public TruncateRule(int maxLength, string suffix = "…")
        {
            if (maxLength < 0)
                throw new ArgumentException("Max length must be non-negative", nameof(maxLength));

            _maxLength = maxLength;
            _suffix = suffix ?? string.Empty;
        }

        /// <summary>
        /// Applies the truncate rule to the input string.
        /// </summary>
        /// <param name="input">The string to truncate</param>
        /// <returns>The truncated string with suffix if needed, or the original string if within maxLength</returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= _maxLength)
                return input;

            // Calculate how many characters we can keep (accounting for suffix length)
            int truncateLength = Math.Max(0, _maxLength - _suffix.Length);
            return input.Substring(0, truncateLength) + _suffix;
        }
    }
}
