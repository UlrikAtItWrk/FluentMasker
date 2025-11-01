using System;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Specifies which part of the string to mask when using percentage-based masking.
    /// </summary>
    public enum MaskFrom
    {
        /// <summary>
        /// Mask from the start of the string
        /// </summary>
        Start,

        /// <summary>
        /// Mask from the end of the string
        /// </summary>
        End,

        /// <summary>
        /// Mask the middle of the string, keeping characters at both ends
        /// </summary>
        Middle
    }

    /// <summary>
    /// Masks a percentage of the string length from a specified position.
    /// </summary>
    /// <remarks>
    /// <para>Percentage must be between 0.0 (0%) and 1.0 (100%).</para>
    /// <para>The number of characters to mask is calculated as: (int)(string.Length * percentage).</para>
    /// <para>Supports masking from Start, End, or Middle of the string.</para>
    /// <para>Useful for dynamic masking based on string length rather than fixed counts.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Mask 50% from the end:
    /// var rule = new MaskPercentageRule(0.5, MaskFrom.End, "*");
    /// rule.Apply("HelloWorld");  // Returns "Hello*****" (5 chars masked)
    ///
    /// // Mask 30% from the start:
    /// var startRule = new MaskPercentageRule(0.3, MaskFrom.Start, "#");
    /// startRule.Apply("1234567890");  // Returns "###4567890" (3 chars masked)
    ///
    /// // Mask 60% from the middle:
    /// var middleRule = new MaskPercentageRule(0.6, MaskFrom.Middle, "*");
    /// middleRule.Apply("TestString");  // Returns "Te******ng" (6 chars masked)
    ///
    /// // Edge cases:
    /// new MaskPercentageRule(0.0, MaskFrom.End).Apply("Test");  // Returns "Test" (0% = no masking)
    /// new MaskPercentageRule(1.0, MaskFrom.Start).Apply("Test");  // Returns "****" (100% = full masking)
    /// </code>
    /// </example>
    public class MaskPercentageRule : IMaskRule, IMaskRule<string, string>
    {
        private readonly double _percentage;
        private readonly MaskFrom _from;
        private readonly string _maskChar;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskPercentageRule"/> class.
        /// </summary>
        /// <param name="percentage">Percentage of the string to mask (0.0 to 1.0)</param>
        /// <param name="from">Which part of the string to mask (default: End)</param>
        /// <param name="maskChar">Character to use for masking (default: "*")</param>
        /// <exception cref="ArgumentException">Thrown when percentage is not between 0 and 1, or maskChar is null/empty</exception>
        public MaskPercentageRule(double percentage, MaskFrom from = MaskFrom.End, string maskChar = "*")
        {
            if (percentage < 0 || percentage > 1)
                throw new ArgumentException("Percentage must be between 0 and 1", nameof(percentage));
            if (string.IsNullOrEmpty(maskChar))
                throw new ArgumentException("Mask character cannot be null or empty", nameof(maskChar));

            _percentage = percentage;
            _from = from;
            _maskChar = maskChar;
        }

        /// <summary>
        /// Applies the percentage-based mask rule to the input string.
        /// </summary>
        /// <param name="input">The string to mask</param>
        /// <returns>The masked string</returns>
        public string Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            int maskCount = (int)Math.Ceiling(input.Length * _percentage);

            return _from switch
            {
                MaskFrom.Start => new MaskStartRule(maskCount, _maskChar).Apply(input),
                MaskFrom.End => new MaskEndRule(maskCount, _maskChar).Apply(input),
                MaskFrom.Middle => new MaskMiddleRule(
                    (input.Length - maskCount) / 2,
                    (input.Length - maskCount) / 2,
                    _maskChar).Apply(input),
                _ => input
            };
        }

        // Explicit interface implementation to avoid ambiguity in method overload resolution
        string IMaskRule<string, string>.Apply(string input) => Apply(input);
    }
}
