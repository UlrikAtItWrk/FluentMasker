using System;
using System.Numerics;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Adds random noise to numeric values while preserving statistical properties.
    /// Supports both uniform noise and Laplace noise for differential privacy.
    /// </summary>
    /// <typeparam name="T">The numeric type (int, long, decimal, double, float, etc.)</typeparam>
    /// <remarks>
    /// <para>
    /// This rule implements privacy-preserving numeric masking by adding calibrated random noise.
    /// Two distribution strategies are supported:
    /// - **Uniform**: Adds noise uniformly distributed within [-maxAbs, +maxAbs]. Preserves mean and approximate distribution.
    /// - **Laplace**: Uses Laplace distribution (differential privacy mechanism). Provides formal privacy guarantees.
    /// </para>
    /// <para>
    /// **Deterministic Mode**: When used with <see cref="SeedProvider"/>, the same input always produces
    /// the same noise value. This is useful for consistent masking across multiple runs while still preventing
    /// exact value disclosure.
    /// </para>
    /// <para>
    /// **Statistical Properties**:
    /// - Mean: Preserved (expected value = original mean)
    /// - Standard Deviation: Slightly increased by sqrt(variance_original^2 + variance_noise^2)
    /// - Distribution shape: Approximately preserved for large datasets
    /// </para>
    /// <para>
    /// **Use Cases**:
    /// - Salary masking: Add ±$5,000 noise to prevent exact salary disclosure
    /// - Age obfuscation: Add ±2 years of noise
    /// - Analytics datasets: Export data with noise for external analysis
    /// - Differential privacy: Use Laplace distribution with calibrated epsilon
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic uniform noise
    /// var rule = new NoiseAdditiveRule&lt;decimal&gt;(maxAbs: 5000);
    /// decimal masked = rule.Apply(75000m); // Returns value between 70000-80000
    ///
    /// // Deterministic noise (same input always produces same output)
    /// rule.SeedProvider = salary => salary.GetHashCode();
    /// decimal masked1 = rule.Apply(75000m); // e.g., 76234
    /// decimal masked2 = rule.Apply(75000m); // Same: 76234
    ///
    /// // Laplace noise for differential privacy
    /// var dpRule = new NoiseAdditiveRule&lt;decimal&gt;(
    ///     maxAbs: 5000,
    ///     distribution: NoiseAdditiveRule&lt;decimal&gt;.NoiseDistribution.Laplace
    /// );
    /// decimal dpMasked = rule.Apply(75000m); // Differential privacy guarantee
    /// </code>
    /// </example>
    public class NoiseAdditiveRule<T> : NumericMaskRuleBase<T>
        where T : struct, INumber<T>
    {
        private readonly double _maxAbs;
        private readonly NoiseDistribution _distribution;

        /// <summary>
        /// Specifies the noise distribution strategy.
        /// </summary>
        public enum NoiseDistribution
        {
            /// <summary>
            /// Uniform random noise within [-maxAbs, +maxAbs].
            /// Simple and effective for general privacy use cases.
            /// </summary>
            Uniform,

            /// <summary>
            /// Laplace distribution (differential privacy mechanism).
            /// Provides formal privacy guarantees with calibrated epsilon.
            /// Scale parameter b = maxAbs / ln(2).
            /// </summary>
            Laplace
        }

        /// <summary>
        /// Initializes a new instance of the NoiseAdditiveRule class.
        /// </summary>
        /// <param name="maxAbs">Maximum absolute noise value. For uniform distribution, noise is in [-maxAbs, +maxAbs].
        /// For Laplace distribution, this determines the scale parameter b = maxAbs / ln(2).</param>
        /// <param name="distribution">The noise distribution to use (Uniform or Laplace). Default is Uniform.</param>
        /// <exception cref="ArgumentException">Thrown when maxAbs is negative</exception>
        /// <example>
        /// <code>
        /// // Salary masking with ±$5,000 noise
        /// var salaryRule = new NoiseAdditiveRule&lt;decimal&gt;(5000m);
        ///
        /// // Age masking with ±2 years noise
        /// var ageRule = new NoiseAdditiveRule&lt;int&gt;(2);
        ///
        /// // Differential privacy with Laplace noise
        /// var dpRule = new NoiseAdditiveRule&lt;double&gt;(
        ///     maxAbs: 100.0,
        ///     distribution: NoiseAdditiveRule&lt;double&gt;.NoiseDistribution.Laplace
        /// );
        /// </code>
        /// </example>
        public NoiseAdditiveRule(double maxAbs, NoiseDistribution distribution = NoiseDistribution.Uniform)
        {
            if (maxAbs < 0)
                throw new ArgumentException("maxAbs must be non-negative", nameof(maxAbs));

            _maxAbs = maxAbs;
            _distribution = distribution;
        }

        /// <summary>
        /// Applies additive noise to the input value.
        /// </summary>
        /// <param name="input">The value to add noise to</param>
        /// <returns>The input value plus random noise</returns>
        /// <remarks>
        /// If <see cref="NumericMaskRuleBase{T}.SeedProvider"/> is set, noise is deterministic (same input always produces same output).
        /// Otherwise, noise is non-deterministic (different each time).
        /// </remarks>
        public override T Apply(T input)
        {
            // Handle zero maxAbs - no noise to add
            if (_maxAbs == 0)
                return input;

            // Get Random instance (deterministic if SeedProvider is set, otherwise random)
            var rng = GetRandom(input);

            // Generate noise based on distribution
            double noise = _distribution switch
            {
                NoiseDistribution.Uniform => GenerateUniformNoise(rng, _maxAbs),
                NoiseDistribution.Laplace => GenerateLaplaceNoise(rng, _maxAbs),
                _ => throw new InvalidOperationException($"Unsupported noise distribution: {_distribution}")
            };

            // Add noise to input and return
            return input + T.CreateChecked(noise);
        }

        /// <summary>
        /// Generates uniform random noise within [-maxAbs, +maxAbs].
        /// </summary>
        /// <param name="rng">The random number generator</param>
        /// <param name="maxAbs">The maximum absolute noise value</param>
        /// <returns>A random noise value uniformly distributed in [-maxAbs, +maxAbs]</returns>
        /// <remarks>
        /// Formula: noise = Random.NextDouble() * 2 * maxAbs - maxAbs
        /// Expected value: 0 (mean-preserving)
        /// Variance: (maxAbs^2) / 3
        /// </remarks>
        private static double GenerateUniformNoise(Random rng, double maxAbs)
        {
            // Generate random value in [0, 1)
            double u = rng.NextDouble();

            // Scale to [-maxAbs, +maxAbs]
            // u=0 → -maxAbs, u=1 → +maxAbs
            return (u * 2.0 * maxAbs) - maxAbs;
        }

        /// <summary>
        /// Generates Laplace-distributed noise using the inverse CDF method.
        /// This implements the Laplace mechanism for differential privacy.
        /// </summary>
        /// <param name="rng">The random number generator</param>
        /// <param name="maxAbs">The maximum absolute noise value (determines scale parameter)</param>
        /// <returns>A random noise value following the Laplace distribution</returns>
        /// <remarks>
        /// <para>
        /// **Laplace Distribution**: Two-sided exponential distribution centered at 0.
        /// </para>
        /// <para>
        /// **Scale Parameter**: b = maxAbs / ln(2) ≈ maxAbs / 0.693
        /// This ensures approximately 50% of noise values fall within [-maxAbs, +maxAbs].
        /// </para>
        /// <para>
        /// **Inverse CDF Method**:
        /// - Sample u uniformly from [-0.5, +0.5]
        /// - Apply inverse CDF: noise = -b * sign(u) * ln(1 - 2|u|)
        /// </para>
        /// <para>
        /// **Differential Privacy**:
        /// For a query with sensitivity Δf, set b = Δf / ε to achieve ε-differential privacy.
        /// In this implementation: b = maxAbs / ln(2), so choose maxAbs = (Δf / ε) * ln(2).
        /// </para>
        /// </remarks>
        private static double GenerateLaplaceNoise(Random rng, double maxAbs)
        {
            // Calculate scale parameter b
            // b = maxAbs / ln(2) ensures ~50% of values within [-maxAbs, +maxAbs]
            double b = maxAbs / Math.Log(2.0);

            // Sample u uniformly from (0, 1), excluding exactly 0.5 to avoid log(0)
            double u = rng.NextDouble();

            // Shift to [-0.5, +0.5] range
            u = u - 0.5;

            // Apply inverse CDF of Laplace distribution
            // Laplace CDF: F(x) = 0.5 + 0.5 * sign(x) * (1 - exp(-|x|/b))
            // Inverse CDF: x = -b * sign(u) * ln(1 - 2|u|)
            double sign = u < 0 ? -1.0 : 1.0;
            double absU = Math.Abs(u);

            // Clamp absU to avoid log(0) or log(negative)
            // When absU approaches 0.5, we get very large values (tail of distribution)
            if (absU >= 0.5)
                absU = 0.5 - 1e-10; // Small epsilon to avoid log(0)

            double noise = -b * sign * Math.Log(1.0 - 2.0 * absU);

            return noise;
        }
    }
}
