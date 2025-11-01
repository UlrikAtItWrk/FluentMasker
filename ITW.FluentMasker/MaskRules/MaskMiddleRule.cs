using System;
using System.Buffers;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Keeps the first and last N characters visible and masks the middle portion.
    /// </summary>
    /// <remarks>
    /// <para>If keepFirst + keepLast >= string length, the entire string is kept unchanged.</para>
    /// <para>If both keepFirst and keepLast are 0, the entire string is masked.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance.</para>
    /// <para>Ideal for masking email addresses, names, or sensitive data while preserving context.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rule = new MaskMiddleRule(2, 2, "*");
    /// var result = rule.Apply("HelloWorld");
    /// // result = "He******ld"
    ///
    /// // Email masking:
    /// var emailRule = new MaskMiddleRule(3, 8, "*");
    /// emailRule.Apply("john.doe@example.com");  // Returns "joh*********ample.com"
    ///
    /// // Edge cases:
    /// rule.Apply("Hi");          // Returns "Hi" (too short, unchanged)
    /// new MaskMiddleRule(0, 0, "*").Apply("Test");  // Returns "****" (full masking)
    /// rule.Apply("");            // Returns "" (empty string unchanged)
    /// </code>
    /// </example>
    public class MaskMiddleRule : IMaskRule, IMaskRule<string, string>
    {
        private readonly int _keepFirst;
        private readonly int _keepLast;
        private readonly string _maskChar;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskMiddleRule"/> class.
        /// </summary>
        /// <param name="keepFirst">Number of characters to keep visible at the start</param>
        /// <param name="keepLast">Number of characters to keep visible at the end</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <exception cref="ArgumentException">Thrown when counts are negative or maskChar is null/empty</exception>
        public MaskMiddleRule(int keepFirst, int keepLast, string maskChar = "*")
        {
            if (keepFirst < 0)
                throw new ArgumentException("Keep first must be non-negative", nameof(keepFirst));
            if (keepLast < 0)
                throw new ArgumentException("Keep last must be non-negative", nameof(keepLast));
            if (string.IsNullOrEmpty(maskChar))
                throw new ArgumentException("Mask character cannot be null or empty", nameof(maskChar));

            _keepFirst = keepFirst;
            _keepLast = keepLast;
            _maskChar = maskChar;
        }

        /// <summary>
        /// Applies the mask rule to the input string.
        /// </summary>
        /// <param name="input">The string to mask</param>
        /// <returns>The masked string</returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Full masking when both are 0
            if (_keepFirst == 0 && _keepLast == 0)
                return new string(_maskChar[0], input.Length);

            if (_keepFirst + _keepLast >= input.Length)
                return input; // Keep entire string

            var pool = ArrayPool<char>.Shared;
            char[] buffer = pool.Rent(input.Length);

            try
            {
                // Copy first N characters
                input.AsSpan(0, _keepFirst).CopyTo(buffer);

                // Mask the middle
                for (int i = _keepFirst; i < input.Length - _keepLast; i++)
                    buffer[i] = _maskChar[0];

                // Copy last N characters
                input.AsSpan(input.Length - _keepLast).CopyTo(buffer.AsSpan(input.Length - _keepLast));

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
