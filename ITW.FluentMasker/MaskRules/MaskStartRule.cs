using System;
using System.Buffers;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Masks the first N characters of a string with a specified mask character.
    /// </summary>
    /// <remarks>
    /// <para>If the count exceeds the string length, the entire string is masked.</para>
    /// <para>This rule preserves the original string length.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance and low memory allocation.</para>
    /// <para>Null and empty strings are returned unchanged.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rule = new MaskStartRule(2, "*");
    /// var result = rule.Apply("JohnDoe");
    /// // result = "**hnDoe"
    ///
    /// // Edge cases:
    /// rule.Apply("Hi");      // Returns "**" (masks entire string)
    /// rule.Apply("");        // Returns "" (empty string unchanged)
    /// rule.Apply(null);      // Returns null
    /// </code>
    /// </example>
    public class MaskStartRule : IMaskRule, IMaskRule<string, string>
    {
        private readonly int _count;
        private readonly string _maskChar;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskStartRule"/> class.
        /// </summary>
        /// <param name="count">Number of characters to mask from the start</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <exception cref="ArgumentException">Thrown when count is negative or maskChar is null/empty</exception>
        public MaskStartRule(int count, string maskChar = "*")
        {
            if (count < 0)
                throw new ArgumentException("Count must be non-negative", nameof(count));
            if (string.IsNullOrEmpty(maskChar))
                throw new ArgumentException("Mask character cannot be null or empty", nameof(maskChar));

            _count = count;
            _maskChar = maskChar;
        }

        /// <summary>
        /// Applies the mask rule to the input string, masking the first N characters.
        /// </summary>
        /// <param name="input">The string to mask. Can be null or empty.</param>
        /// <returns>
        /// The masked string with the first N characters replaced by the mask character.
        /// Returns the original input if it is null, empty, or if count is 0.
        /// If count exceeds the string length, returns a string of mask characters matching the input length.
        /// </returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (_count == 0)
                return input;

            if (_count >= input.Length)
                return new string(_maskChar[0], input.Length);

            // Use ArrayPool for better performance
            var pool = ArrayPool<char>.Shared;
            char[] buffer = pool.Rent(input.Length);

            try
            {
                // Fill with mask characters
                for (int i = 0; i < _count; i++)
                    buffer[i] = _maskChar[0];

                // Copy remaining characters
                input.AsSpan(_count).CopyTo(buffer.AsSpan(_count));

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
