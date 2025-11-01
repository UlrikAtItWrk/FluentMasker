using System;
using System.Buffers;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Keeps the first N characters visible and masks all remaining characters.
    /// </summary>
    /// <remarks>
    /// <para>If keepCount >= string length, the entire string remains visible.</para>
    /// <para>If keepCount = 0, the entire string is masked.</para>
    /// <para>Uses ArrayPool&lt;char&gt; internally for high performance.</para>
    /// <para>Commonly used for masking account numbers, showing only the first few digits.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var rule = new KeepFirstRule(3, "*");
    /// var result = rule.Apply("HelloWorld");
    /// // result = "Hel*******"
    ///
    /// // Account number masking:
    /// var acctRule = new KeepFirstRule(4, "#");
    /// acctRule.Apply("1234567890");  // Returns "1234######"
    ///
    /// // Edge cases:
    /// new KeepFirstRule(0, "*").Apply("Test");  // Returns "****" (keep 0 = full masking)
    /// rule.Apply("Hi");          // Returns "Hi" (keepCount >= length)
    /// rule.Apply("");            // Returns "" (empty string unchanged)
    /// </code>
    /// </example>
    public class KeepFirstRule : IMaskRule, IMaskRule<string, string>
    {
        private readonly int _keepCount;
        private readonly string _maskChar;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeepFirstRule"/> class.
        /// </summary>
        /// <param name="keepCount">Number of characters to keep visible from the start</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <exception cref="ArgumentException">Thrown when keepCount is negative or maskChar is null/empty</exception>
        public KeepFirstRule(int keepCount, string maskChar = "*")
        {
            if (keepCount < 0)
                throw new ArgumentException("Keep count must be non-negative", nameof(keepCount));
            if (string.IsNullOrEmpty(maskChar))
                throw new ArgumentException("Mask character cannot be null or empty", nameof(maskChar));

            _keepCount = keepCount;
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

            if (_keepCount == 0)
                return new string(_maskChar[0], input.Length); // Full masking

            if (_keepCount >= input.Length)
                return input; // Keep entire string

            var pool = ArrayPool<char>.Shared;
            char[] buffer = pool.Rent(input.Length);

            try
            {
                // Copy first N characters
                input.AsSpan(0, _keepCount).CopyTo(buffer);

                // Mask the rest
                for (int i = _keepCount; i < input.Length; i++)
                    buffer[i] = _maskChar[0];

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
