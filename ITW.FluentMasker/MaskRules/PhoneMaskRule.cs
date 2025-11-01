using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Masks phone numbers while preserving formatting and structure.
    /// Supports international formats (E.164), North American, and European phone numbers.
    /// </summary>
    /// <remarks>
    /// <para>By default, preserves separators like spaces, dashes, parentheses, dots, and plus signs.</para>
    /// <para>Can optionally extract digits only and mask without preserving format.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance and low memory allocation.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Preserve formatting (default)
    /// var rule = new PhoneMaskRule(keepLast: 2, preserveSeparators: true);
    /// var result = rule.Apply("+45 12 34 56 78");
    /// // result = "+** ** ** ** 78"
    ///
    /// var result2 = rule.Apply("(555) 123-4567");
    /// // result2 = "(***) ***-**67"
    ///
    /// // Non-preserving mode
    /// var rule2 = new PhoneMaskRule(keepLast: 2, preserveSeparators: false);
    /// var result3 = rule2.Apply("(555) 123-4567");
    /// // result3 = "**********67"
    ///
    /// // Keep last 4 digits
    /// var rule3 = new PhoneMaskRule(keepLast: 4);
    /// var result4 = rule3.Apply("+1-555-123-4567");
    /// // result4 = "+*-***-***-4567"
    /// </code>
    /// </example>
    public class PhoneMaskRule : IMaskRule, IMaskRule<string, string>
    {
        private readonly int _keepLast;
        private readonly bool _preserveSeparators;
        private readonly string _countryHint;
        private readonly string _maskChar;

        private static readonly char[] Separators = { ' ', '-', '(', ')', '.', '+' };

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneMaskRule"/> class.
        /// </summary>
        /// <param name="keepLast">Number of digits to keep visible at the end (default: 2)</param>
        /// <param name="preserveSeparators">Whether to preserve formatting characters (default: true)</param>
        /// <param name="countryHint">Optional country hint for ambiguous formats (not currently used)</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <exception cref="ArgumentException">Thrown when keepLast is negative</exception>
        /// <exception cref="ArgumentNullException">Thrown when maskChar is null</exception>
        public PhoneMaskRule(
            int keepLast = 2,
            bool preserveSeparators = true,
            string countryHint = null,
            string maskChar = "*")
        {
            if (keepLast < 0)
                throw new ArgumentException("Keep last must be non-negative", nameof(keepLast));

            _keepLast = keepLast;
            _preserveSeparators = preserveSeparators;
            _countryHint = countryHint;
            _maskChar = maskChar ?? throw new ArgumentNullException(nameof(maskChar));
        }

        /// <summary>
        /// Applies the phone masking rule to the input string.
        /// </summary>
        /// <param name="input">The phone number to mask. Can be null or empty.</param>
        /// <returns>
        /// The masked phone number with only the last N digits visible.
        /// Returns the original input if it is null or empty.
        /// If preserveSeparators is false, returns only digits (masked and visible).
        /// </returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (!_preserveSeparators)
            {
                // Simple mode: extract digits, mask, return
                string digits = new string(input.Where(char.IsDigit).ToArray());
                string masked = MaskDigits(digits);
                return masked;
            }

            // Complex mode: preserve structure
            return MaskPreservingStructure(input);
        }

        /// <summary>
        /// Masks digits without preserving any formatting.
        /// </summary>
        /// <param name="digits">String containing only digits</param>
        /// <returns>Masked string with last N digits visible</returns>
        private string MaskDigits(string digits)
        {
            if (_keepLast >= digits.Length)
                return digits;

            int maskCount = digits.Length - _keepLast;
            return new string(_maskChar[0], maskCount) + digits.Substring(maskCount);
        }

        /// <summary>
        /// Masks digits while preserving the original formatting and separator characters.
        /// Uses ArrayPool for efficient memory usage.
        /// </summary>
        /// <param name="input">The formatted phone number string</param>
        /// <returns>Masked phone number with formatting preserved</returns>
        private string MaskPreservingStructure(string input)
        {
            // Extract digits and their positions
            var digitPositions = new List<(int position, char digit)>();
            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsDigit(input[i]))
                    digitPositions.Add((i, input[i]));
            }

            if (digitPositions.Count == 0)
                return input;

            // Determine which digits to mask
            int digitsToMask = Math.Max(0, digitPositions.Count - _keepLast);

            // Reconstruct string with masked digits
            var pool = ArrayPool<char>.Shared;
            char[] buffer = pool.Rent(input.Length);

            try
            {
                input.AsSpan().CopyTo(buffer);

                for (int i = 0; i < digitsToMask; i++)
                {
                    buffer[digitPositions[i].position] = _maskChar[0];
                }

                return new string(buffer, 0, input.Length);
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        // Explicit interface implementation to avoid ambiguity in method overload resolution
        string IMaskRule<string, string>.Apply(string input) => Apply(input);
    }
}
