using System;
using System.Numerics;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Rounds numeric values to the nearest multiple of a specified increment.
    /// Useful for generalizing numeric data while preserving approximate magnitude.
    /// </summary>
    /// <typeparam name="T">The numeric type (int, long, decimal, double, float, etc.)</typeparam>
    /// <remarks>
    /// <para>
    /// This rule implements privacy-preserving rounding by reducing precision to specified increments.
    /// Common use cases include:
    /// - Salary masking: Round to nearest $1,000 or $5,000
    /// - Age generalization: Round to nearest 5 or 10 years
    /// - Geographic precision: Round coordinates to reduce location accuracy
    /// - Financial reporting: Round transaction amounts to nearest dollar/euro/etc.
    /// </para>
    /// <para>
    /// The rounding uses "round half to even" (banker's rounding) strategy via Math.Round,
    /// which minimizes statistical bias in large datasets.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Salary masking
    /// var rule = new RoundToRule&lt;decimal&gt;(1000m);
    /// decimal masked = rule.Apply(75123m); // Returns 75000
    ///
    /// // Negative values
    /// decimal maskedNeg = rule.Apply(-75123m); // Returns -75000
    ///
    /// // Age generalization
    /// var ageRule = new RoundToRule&lt;int&gt;(5);
    /// int maskedAge = ageRule.Apply(27); // Returns 25
    /// </code>
    /// </example>
    public class RoundToRule<T> : INumericMaskRule<T> where T : struct, INumber<T>
    {
        private readonly T _increment;

        /// <summary>
        /// Initializes a new instance of the RoundToRule class.
        /// </summary>
        /// <param name="increment">The increment to round to (e.g., 1000 for rounding to nearest thousand).
        /// If zero, the rule will return the original value unchanged. Negative increments are treated as their absolute value.</param>
        /// <example>
        /// <code>
        /// // Round salaries to nearest $1,000
        /// var salaryRule = new RoundToRule&lt;decimal&gt;(1000m);
        ///
        /// // Round ages to nearest 5 years
        /// var ageRule = new RoundToRule&lt;int&gt;(5);
        ///
        /// // Round percentages to nearest 0.1
        /// var percentRule = new RoundToRule&lt;double&gt;(0.1);
        ///
        /// // Zero increment returns original value
        /// var noRoundRule = new RoundToRule&lt;int&gt;(0);
        /// </code>
        /// </example>
        public RoundToRule(T increment)
        {
            // Store increment (use absolute value to handle negative increments)
            _increment = T.Abs(increment);
        }

        /// <summary>
        /// Applies the rounding rule to the input value.
        /// Rounds to the nearest multiple of the configured increment.
        /// </summary>
        /// <param name="input">The numeric value to round</param>
        /// <returns>The rounded value (nearest multiple of increment), or original value if increment is zero</returns>
        /// <remarks>
        /// <para>
        /// Uses "round half to even" (banker's rounding) strategy, which rounds .5 to the nearest even number.
        /// This minimizes statistical bias when rounding many values.
        /// </para>
        /// <para>
        /// Algorithm:
        /// 1. If increment is zero, return original value unchanged
        /// 2. Divide input by increment
        /// 3. Round to nearest integer using Math.Round
        /// 4. Multiply by increment
        /// </para>
        /// <para>
        /// Examples:
        /// - 75123 with increment=1000 → 75000
        /// - 75500 with increment=1000 → 76000
        /// - -75123 with increment=1000 → -75000
        /// - 27 with increment=5 → 25
        /// - 28 with increment=5 → 30
        /// - 75123 with increment=0 → 75123 (unchanged)
        /// </para>
        /// </remarks>
        public T Apply(T input)
        {
            // Edge case: if increment is zero, return original value unchanged
            // This allows for graceful degradation when increment is not set or invalid
            if (_increment == T.Zero)
            {
                return input;
            }

            // Convert to double for mathematical operations
            double inputDouble = NumericMaskRuleBase<T>.ToDouble(input);
            double incrementDouble = NumericMaskRuleBase<T>.ToDouble(_increment);

            // Round to nearest increment using "round half to even" strategy
            // Algorithm:
            // 1. Divide by increment to get number of increments
            // 2. Round to nearest integer (banker's rounding)
            // 3. Multiply by increment to get final value
            double numberOfIncrements = inputDouble / incrementDouble;
            double roundedIncrements = Math.Round(numberOfIncrements, MidpointRounding.ToEven);
            double result = roundedIncrements * incrementDouble;

            // Convert back to target numeric type
            return NumericMaskRuleBase<T>.FromDouble(result);
        }
    }
}
