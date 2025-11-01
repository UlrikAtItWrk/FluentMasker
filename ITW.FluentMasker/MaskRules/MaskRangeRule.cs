using System;
using System.Buffers;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Masks a specific range of characters in a string starting at a given position.
    /// </summary>
    /// <remarks>
    /// <para>If start is beyond the string length, no masking occurs.</para>
    /// <para>If start + length exceeds the string length, masks to the end of the string.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance.</para>
    /// <para>Useful for masking specific portions like middle digits of a credit card.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rule = new MaskRangeRule(2, 5, "*");
    /// var result = rule.Apply("HelloWorld");
    /// // result = "He*****rld"
    ///
    /// // Credit card middle digits:
    /// var ccRule = new MaskRangeRule(4, 8, "*");
    /// ccRule.Apply("1234567812345678");  // Returns "1234********5678"
    ///
    /// // Edge cases:
    /// rule.Apply("Hi");          // Returns "Hi" (start beyond length)
    /// new MaskRangeRule(15, 5, "*").Apply("Short");  // Returns "Short" (start out of range)
    /// new MaskRangeRule(0, 0, "*").Apply("Test");    // Returns "Test" (length = 0)
    /// </code>
    /// </example>
    public class MaskRangeRule : IMaskRule, IMaskRule<string, string>
    {
        private readonly int _start;
        private readonly int _length;
        private readonly string _maskChar;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskRangeRule"/> class.
        /// </summary>
        /// <param name="start">Starting position (0-based index)</param>
        /// <param name="length">Number of characters to mask</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid or maskChar is null/empty</exception>
        public MaskRangeRule(int start, int length, string maskChar = "*")
        {
            if (start < 0)
                throw new ArgumentException("Start position must be non-negative", nameof(start));
            if (length < 0)
                throw new ArgumentException("Length must be non-negative", nameof(length));
            if (string.IsNullOrEmpty(maskChar))
                throw new ArgumentException("Mask character cannot be null or empty", nameof(maskChar));

            _start = start;
            _length = length;
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

            if (_start >= input.Length || _length == 0)
                return input; // Nothing to mask

            // Calculate actual range to mask
            int effectiveStart = _start;
            int effectiveLength = Math.Min(_length, input.Length - _start);

            var pool = ArrayPool<char>.Shared;
            char[] buffer = pool.Rent(input.Length);

            try
            {
                // Copy characters before the range
                if (effectiveStart > 0)
                    input.AsSpan(0, effectiveStart).CopyTo(buffer);

                // Mask the range
                for (int i = effectiveStart; i < effectiveStart + effectiveLength; i++)
                    buffer[i] = _maskChar[0];

                // Copy characters after the range
                int afterRangeStart = effectiveStart + effectiveLength;
                if (afterRangeStart < input.Length)
                    input.AsSpan(afterRangeStart).CopyTo(buffer.AsSpan(afterRangeStart));

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
