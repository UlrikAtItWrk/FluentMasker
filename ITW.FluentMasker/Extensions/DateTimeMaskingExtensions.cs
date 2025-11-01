using System;
using ITW.FluentMasker.Builders;
using ITW.FluentMasker.MaskRules;

namespace ITW.FluentMasker.Extensions
{
    /// <summary>
    /// Extension methods for DateTime masking operations with fluent API support.
    /// </summary>
    public static class DateTimeMaskingExtensions
    {
        /// <summary>
        /// Sets a seed provider for deterministic masking on the next seeded rule.
        /// The seed provider enables consistent masking where the same seed produces the same masked output.
        /// </summary>
        /// <param name="builder">The DateTime masking builder instance</param>
        /// <param name="seedProvider">Function that generates a seed value from the input DateTime</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// <para>
        /// The seed provider is applied to the next mask rule that implements <see cref="ISeededMaskRule{T}"/>.
        /// After being applied, the seed provider is cleared, so it only affects one rule.
        /// </para>
        /// <para>
        /// <b>HIPAA Compliance:</b> For HIPAA Safe Harbor compliance, use a seed based on patient ID
        /// to ensure all dates for the same patient are shifted by the same amount:
        /// <code>builder.WithRandomSeed(dt => patientId.GetHashCode())</code>
        /// </para>
        /// <para>
        /// <b>Use Cases:</b>
        /// - HIPAA compliance: Consistent date shifting per patient ID
        /// - Temporal analysis: Maintain relative date relationships
        /// - Testing: Predictable date masking for unit tests
        /// </para>
        /// <para>
        /// <b>Security Note:</b> Deterministic masking may enable re-identification if the seed source is known.
        /// Use non-deterministic masking (without seed provider) for maximum privacy.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // HIPAA-compliant: consistent shift per patient
        /// var rule = new DateTimeMaskingBuilder()
        ///     .WithRandomSeed(dt => patientId.GetHashCode())
        ///     .DateShift(180)
        ///     .Build();
        ///
        /// // All dates for this patient shifted by same amount
        /// var admission = rule[0].Apply(new DateTime(2025, 1, 15));
        /// var discharge = rule[0].Apply(new DateTime(2025, 1, 20));
        /// // Duration between admission and discharge is preserved
        /// </code>
        /// </example>
        public static DateTimeMaskingBuilder WithRandomSeed(
            this DateTimeMaskingBuilder builder,
            SeedProvider<DateTime> seedProvider)
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
        /// <param name="builder">The DateTime masking builder instance</param>
        /// <param name="seed">Constant seed value to use for all operations</param>
        /// <returns>The builder instance for method chaining</returns>
        /// <example>
        /// <code>
        /// var rule = new DateTimeMaskingBuilder()
        ///     .WithRandomSeed(12345)  // Use constant seed
        ///     .DateShift(180)
        ///     .Build();
        /// </code>
        /// </example>
        public static DateTimeMaskingBuilder WithRandomSeed(
            this DateTimeMaskingBuilder builder,
            int seed)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // Create a constant seed provider
            SeedProvider<DateTime> seedProvider = _ => seed;
            builder.PendingSeedProvider = seedProvider;
            return builder;
        }

        /// <summary>
        /// Adds a DateShiftRule to the builder for shifting dates by a random number of days.
        /// This rule is designed for HIPAA Safe Harbor compliance.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="daysRange">
        /// Maximum number of days to shift in either direction.
        /// For HIPAA Safe Harbor compliance, this should be ≤ 365 days.
        /// </param>
        /// <param name="preserveTime">
        /// If true, only the date component is shifted, preserving time-of-day.
        /// Default is true to maintain temporal precision within a day.
        /// </param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// <para>
        /// <b>HIPAA Safe Harbor Context:</b>
        /// Per 45 CFR §164.514(b)(2), dates can be shifted by a random amount consistent across
        /// all dates for an individual, provided the shift is within a ±365 day range.
        /// </para>
        /// <para>
        /// To achieve HIPAA compliance, combine with <see cref="WithRandomSeed(DateTimeMaskingBuilder, SeedProvider{DateTime})"/>
        /// using a seed based on patient ID:
        /// <code>.WithRandomSeed(dt => patientId.GetHashCode()).DateShift(180)</code>
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Basic date shifting
        /// var basicRule = new DateTimeMaskingBuilder()
        ///     .DateShift(daysRange: 180)
        ///     .Build();
        ///
        /// // HIPAA-compliant date shifting
        /// var hipaaRule = new DateTimeMaskingBuilder()
        ///     .WithRandomSeed(dt => patientId.GetHashCode())  // Consistent per patient
        ///     .DateShift(daysRange: 180, preserveTime: true)   // ±180 days, preserve time
        ///     .Build();
        ///
        /// DateTime admission = new DateTime(2025, 1, 15, 14, 30, 0);
        /// DateTime masked = hipaaRule[0].Apply(admission);
        /// // masked will have same time (14:30:00) but shifted date
        /// </code>
        /// </example>
        public static DateTimeMaskingBuilder DateShift(
            this DateTimeMaskingBuilder builder,
            int daysRange,
            bool preserveTime = true)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var rule = new DateShiftRule(daysRange, preserveTime);
            return builder.AddRule(rule);
        }

        /// <summary>
        /// Adds a TimeBucketRule to reduce temporal precision by bucketing timestamps to a specified granularity.
        /// Useful for privacy-preserving analytics where exact timestamps are not required.
        /// </summary>
        /// <param name="builder">The builder instance</param>
        /// <param name="granularity">
        /// The level of time granularity for bucketing.
        /// Options: Hour, Day, Week, Month, Quarter, Year
        /// </param>
        /// <returns>The builder instance for method chaining</returns>
        /// <remarks>
        /// <para>
        /// This rule reduces temporal precision while preserving time-based patterns for analytics.
        /// </para>
        /// <para>
        /// <b>Use Cases:</b>
        /// </para>
        /// <list type="bullet">
        /// <item><description><b>Analytics Privacy:</b> Publish hourly/daily aggregations without exact timestamps</description></item>
        /// <item><description><b>Log Masking:</b> Reduce precision of audit logs for GDPR compliance</description></item>
        /// <item><description><b>Time-Series Data:</b> Align events to consistent time buckets</description></item>
        /// </list>
        /// <para>
        /// <b>Granularity Examples:</b>
        /// </para>
        /// <list type="bullet">
        /// <item><description><b>Hour:</b> 2025-10-31 14:32:15 → 2025-10-31 14:00:00</description></item>
        /// <item><description><b>Day:</b> 2025-10-31 14:32:15 → 2025-10-31 00:00:00</description></item>
        /// <item><description><b>Week:</b> 2025-10-31 14:32:15 → 2025-10-27 00:00:00 (Monday)</description></item>
        /// <item><description><b>Month:</b> 2025-10-31 14:32:15 → 2025-10-01 00:00:00</description></item>
        /// <item><description><b>Quarter:</b> 2025-10-31 14:32:15 → 2025-10-01 00:00:00 (Q4 start)</description></item>
        /// <item><description><b>Year:</b> 2025-10-31 14:32:15 → 2025-01-01 00:00:00</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Hourly precision for audit logs
        /// var hourlyRule = new DateTimeMaskingBuilder()
        ///     .TimeBucket(TimeBucketRule.Granularity.Hour)
        ///     .Build();
        ///
        /// var logTime = new DateTime(2025, 10, 31, 14, 32, 15);
        /// var bucketed = hourlyRule[0].Apply(logTime);
        /// // Result: 2025-10-31 14:00:00
        ///
        /// // Daily aggregation for analytics
        /// var dailyRule = new DateTimeMaskingBuilder()
        ///     .TimeBucket(TimeBucketRule.Granularity.Day)
        ///     .Build();
        ///
        /// // Quarterly financial reporting
        /// var quarterlyRule = new DateTimeMaskingBuilder()
        ///     .TimeBucket(TimeBucketRule.Granularity.Quarter)
        ///     .Build();
        /// </code>
        /// </example>
        public static DateTimeMaskingBuilder TimeBucket(
            this DateTimeMaskingBuilder builder,
            TimeBucketRule.Granularity granularity)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            var rule = new TimeBucketRule(granularity);
            return builder.AddRule(rule);
        }
    }
}
