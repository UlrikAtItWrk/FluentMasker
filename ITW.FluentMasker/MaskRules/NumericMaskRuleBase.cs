using System;
using System.Numerics;
using System.Security.Cryptography;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Base class for numeric masking rules with common utilities.
    /// Provides random number generation with seed support, type conversion helpers, and statistical validation methods.
    /// </summary>
    /// <typeparam name="T">The numeric type (int, long, float, double, decimal, etc.)</typeparam>
    public abstract class NumericMaskRuleBase<T> : INumericMaskRule<T>, ISeededMaskRule<T>
        where T : struct, INumber<T>
    {
        /// <summary>
        /// Gets or sets the seed provider for deterministic masking.
        /// If null, the rule will use non-deterministic random masking.
        /// </summary>
        public SeedProvider<T>? SeedProvider { get; set; }

        /// <summary>
        /// Applies the masking rule to the input value.
        /// </summary>
        /// <param name="input">The value to mask</param>
        /// <returns>The masked value</returns>
        public abstract T Apply(T input);

        /// <summary>
        /// Gets a Random instance with optional seed support.
        /// If SeedProvider is set, uses deterministic seeding based on input value.
        /// Otherwise, generates a cryptographically secure random seed.
        /// </summary>
        /// <param name="input">The input value to generate seed from (if SeedProvider is set)</param>
        /// <returns>A Random instance ready for use</returns>
        protected Random GetRandom(T input)
        {
            if (SeedProvider != null)
            {
                // Deterministic: same input always produces same seed
                int seed = SeedProvider(input);
                return new Random(seed);
            }
            else
            {
                // Non-deterministic: use cryptographically secure random seed
                return new Random(GenerateSecureRandomSeed());
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random seed value.
        /// Uses RandomNumberGenerator for security-grade randomness.
        /// </summary>
        /// <returns>A secure random integer seed</returns>
        public static int GenerateSecureRandomSeed()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                return BitConverter.ToInt32(bytes, 0);
            }
        }

        #region Type Conversion Helpers

        /// <summary>
        /// Converts a numeric value to double for mathematical operations.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The value as a double</returns>
        public static double ToDouble(T value)
        {
            return Convert.ToDouble(value);
        }

        /// <summary>
        /// Converts a double back to the target numeric type.
        /// </summary>
        /// <param name="value">The double value to convert</param>
        /// <returns>The value as type T</returns>
        public static T FromDouble(double value)
        {
            return T.CreateChecked(value);
        }

        /// <summary>
        /// Converts a numeric value to decimal for high-precision operations.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The value as a decimal</returns>
        public static decimal ToDecimal(T value)
        {
            return Convert.ToDecimal(value);
        }

        /// <summary>
        /// Converts a decimal back to the target numeric type.
        /// </summary>
        /// <param name="value">The decimal value to convert</param>
        /// <returns>The value as type T</returns>
        public static T FromDecimal(decimal value)
        {
            return T.CreateChecked(value);
        }

        /// <summary>
        /// Converts a numeric value to long for integer operations.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The value as a long</returns>
        public static long ToLong(T value)
        {
            return Convert.ToInt64(value);
        }

        /// <summary>
        /// Converts a long back to the target numeric type.
        /// </summary>
        /// <param name="value">The long value to convert</param>
        /// <returns>The value as type T</returns>
        public static T FromLong(long value)
        {
            return T.CreateChecked(value);
        }

        #endregion

        #region Statistical Validation Methods

        /// <summary>
        /// Calculates the mean (average) of a collection of numeric values.
        /// </summary>
        /// <param name="values">The collection of values</param>
        /// <returns>The mean value</returns>
        /// <exception cref="ArgumentException">Thrown when collection is empty</exception>
        public static double CalculateMean(IEnumerable<T> values)
        {
            var list = values.ToList();
            if (list.Count == 0)
                throw new ArgumentException("Cannot calculate mean of empty collection", nameof(values));

            double sum = 0;
            foreach (var value in list)
            {
                sum += ToDouble(value);
            }

            return sum / list.Count;
        }

        /// <summary>
        /// Calculates the standard deviation of a collection of numeric values.
        /// Uses the sample standard deviation formula (N-1 divisor).
        /// </summary>
        /// <param name="values">The collection of values</param>
        /// <returns>The standard deviation</returns>
        /// <exception cref="ArgumentException">Thrown when collection has less than 2 elements</exception>
        public static double CalculateStdDev(IEnumerable<T> values)
        {
            var list = values.ToList();
            if (list.Count < 2)
                throw new ArgumentException("Cannot calculate standard deviation with less than 2 values", nameof(values));

            double mean = CalculateMean(list);
            double sumSquaredDifferences = 0;

            foreach (var value in list)
            {
                double diff = ToDouble(value) - mean;
                sumSquaredDifferences += diff * diff;
            }

            // Sample standard deviation (N-1)
            return Math.Sqrt(sumSquaredDifferences / (list.Count - 1));
        }

        /// <summary>
        /// Calculates the variance of a collection of numeric values.
        /// Uses the sample variance formula (N-1 divisor).
        /// </summary>
        /// <param name="values">The collection of values</param>
        /// <returns>The variance</returns>
        /// <exception cref="ArgumentException">Thrown when collection has less than 2 elements</exception>
        public static double CalculateVariance(IEnumerable<T> values)
        {
            double stdDev = CalculateStdDev(values);
            return stdDev * stdDev;
        }

        /// <summary>
        /// Validates that two means are within a specified percentage tolerance.
        /// </summary>
        /// <param name="mean1">First mean value</param>
        /// <param name="mean2">Second mean value</param>
        /// <param name="tolerancePercent">Tolerance as a percentage (e.g., 0.01 for ±0.01%)</param>
        /// <returns>True if means are within tolerance, false otherwise</returns>
        public static bool ValidateMeanPreservation(double mean1, double mean2, double tolerancePercent)
        {
            if (Math.Abs(mean1) < double.Epsilon)
            {
                // If mean is effectively zero, check absolute difference
                return Math.Abs(mean2 - mean1) <= tolerancePercent;
            }

            double percentDifference = Math.Abs((mean2 - mean1) / mean1);
            return percentDifference <= (tolerancePercent / 100.0);
        }

        /// <summary>
        /// Validates that two standard deviations are within a specified percentage tolerance.
        /// </summary>
        /// <param name="stdDev1">First standard deviation</param>
        /// <param name="stdDev2">Second standard deviation</param>
        /// <param name="tolerancePercent">Tolerance as a percentage (e.g., 5 for ±5%)</param>
        /// <returns>True if standard deviations are within tolerance, false otherwise</returns>
        public static bool ValidateStdDevPreservation(double stdDev1, double stdDev2, double tolerancePercent)
        {
            if (Math.Abs(stdDev1) < double.Epsilon)
            {
                // If stdDev is effectively zero, check absolute difference
                return Math.Abs(stdDev2 - stdDev1) <= tolerancePercent;
            }

            double percentDifference = Math.Abs((stdDev2 - stdDev1) / stdDev1);
            return percentDifference <= (tolerancePercent / 100.0);
        }

        #endregion
    }
}
