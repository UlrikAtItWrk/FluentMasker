using System;
using System.Numerics;
using ITW.FluentMasker.Builders;
using ITW.FluentMasker.MaskRules;

namespace ITW.FluentMasker.Extensions
{
    /// <summary>
    /// Extension methods for numeric masking operations with fluent API support.
    /// </summary>
    public static class NumericMaskingExtensions
    {
        /// <summary>
        /// Sets a seed provider for deterministic masking on the next seeded rule.
        /// The seed provider enables consistent masking where the same input always produces the same masked output.
        /// </summary>
        /// <typeparam name="T">The numeric type (int, long, decimal, double, float, etc.)</typeparam>
        /// <param name="builder">The numeric masking builder instance</param>
        /// <param name="seedProvider">Function that generates a seed value from the input value</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// <para>
        /// The seed provider is applied to the next mask rule that implements <see cref="ISeededMaskRule{T}"/>.
        /// After being applied, the seed provider is cleared, so it only affects one rule.
        /// </para>
        /// <para>
        /// <b>Use Cases:</b>
        /// - HIPAA compliance: Consistent date shifting per patient ID
        /// - Analytics: Reproducible noise for aggregated data
        /// - Testing: Predictable masking for unit tests
        /// </para>
        /// <para>
        /// <b>Security Note:</b> Deterministic masking may enable re-identification if the seed source is known.
        /// Use non-deterministic masking (without seed provider) for maximum privacy.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var rule = new NumericMaskingBuilder&lt;decimal&gt;()
        ///     .WithRandomSeed(salary => salary.GetHashCode())
        ///     .AddRule(new NoiseAdditiveRule&lt;decimal&gt;(5000))
        ///     .Build();
        ///
        /// // Same input always produces same output
        /// var masked1 = rule[0].Apply(75000m); // e.g., 76234
        /// var masked2 = rule[0].Apply(75000m); // Same: 76234
        /// </code>
        /// </example>
        public static NumericMaskingBuilder<T> WithRandomSeed<T>(
            this NumericMaskingBuilder<T> builder,
            SeedProvider<T> seedProvider)
            where T : struct, INumber<T>
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (seedProvider == null)
                throw new ArgumentNullException(nameof(seedProvider));

            builder.PendingSeedProvider = seedProvider;
            return builder;
        }

        /// <summary>
        /// Sets a constant seed value for deterministic masking on the next seeded rule.
        /// Convenience overload for simple deterministic masking scenarios.
        /// </summary>
        /// <typeparam name="T">The numeric type (int, long, decimal, double, float, etc.)</typeparam>
        /// <param name="builder">The numeric masking builder instance</param>
        /// <param name="seed">Constant seed value to use for all operations</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var rule = new NumericMaskingBuilder&lt;decimal&gt;()
        ///     .WithRandomSeed(12345)  // Use constant seed
        ///     .AddRule(new NoiseAdditiveRule&lt;decimal&gt;(5000))
        ///     .Build();
        /// </code>
        /// </example>
        public static NumericMaskingBuilder<T> WithRandomSeed<T>(
            this NumericMaskingBuilder<T> builder,
            int seed)
            where T : struct, INumber<T>
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // Create a constant seed provider
            SeedProvider<T> seedProvider = _ => seed;
            builder.PendingSeedProvider = seedProvider;
            return builder;
        }

        /// <summary>
        /// Adds a NoiseAdditiveRule to the builder for adding random noise to numeric values.
        /// </summary>
        /// <typeparam name="T">The numeric type</typeparam>
        /// <param name="builder">The builder instance</param>
        /// <param name="maxAbs">Maximum absolute noise value</param>
        /// <param name="distribution">Noise distribution (Uniform or Laplace)</param>
        /// <returns>The builder instance for method chaining</returns>
        public static NumericMaskingBuilder<T> NoiseAdditive<T>(
            this NumericMaskingBuilder<T> builder,
            double maxAbs,
            NoiseAdditiveRule<T>.NoiseDistribution distribution = NoiseAdditiveRule<T>.NoiseDistribution.Uniform)
            where T : struct, INumber<T>
        {
            return builder.AddRule(new NoiseAdditiveRule<T>(maxAbs, distribution));
        }

        /// <summary>
        /// Adds a RoundToRule to the builder for rounding numeric values to the nearest increment.
        /// </summary>
        /// <typeparam name="T">The numeric type</typeparam>
        /// <param name="builder">The builder instance</param>
        /// <param name="increment">The rounding increment</param>
        /// <returns>The builder instance for method chaining</returns>
        public static NumericMaskingBuilder<T> RoundTo<T>(
            this NumericMaskingBuilder<T> builder,
            T increment)
            where T : struct, INumber<T>
        {
            return builder.AddRule(new RoundToRule<T>(increment));
        }
    }
}
