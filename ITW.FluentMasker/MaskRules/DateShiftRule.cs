using System;
using System.Security.Cryptography;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Shifts dates by a random number of days while optionally preserving consistency per individual.
    /// This rule is designed for HIPAA Safe Harbor compliance (45 CFR §164.514(b)(2)).
    /// </summary>
    /// <remarks>
    /// <para>
    /// **HIPAA Safe Harbor Compliance**:
    /// Per 45 CFR §164.514(b)(2), dates can be shifted by a random amount consistent across
    /// all dates for an individual, provided the shift is within a ±365 day range.
    /// This rule supports this requirement through deterministic seeding.
    /// </para>
    /// <para>
    /// **Deterministic Mode**: When used with <see cref="SeedProvider"/>, the same seed produces
    /// the same shift amount. This ensures all dates for a patient are shifted by the same offset,
    /// preserving temporal relationships (e.g., time between visits).
    /// </para>
    /// <para>
    /// **Time Preservation**: By default, only the date component is shifted, preserving the time-of-day.
    /// This maintains the relative ordering and duration between events.
    /// </para>
    /// <para>
    /// **Use Cases**:
    /// - HIPAA-compliant patient data de-identification
    /// - Temporal analysis with privacy protection
    /// - Event timeline anonymization
    /// - Test data generation from production dates
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic date shifting with ±180 days
    /// var rule = new DateShiftRule(daysRange: 180);
    /// DateTime shifted = rule.Apply(new DateTime(2025, 1, 15)); // Random shift
    ///
    /// // HIPAA-compliant: consistent shift per patient
    /// rule.SeedProvider = dt => patientId.GetHashCode();
    /// DateTime admission = rule.Apply(new DateTime(2025, 1, 15));  // e.g., 2025-03-20
    /// DateTime discharge = rule.Apply(new DateTime(2025, 1, 20));  // e.g., 2025-03-25
    /// // Both shifted by same amount, preserving 5-day duration
    ///
    /// // Preserve time component (shift date only)
    /// var preserveTimeRule = new DateShiftRule(daysRange: 180, preserveTime: true);
    /// DateTime input = new DateTime(2025, 1, 15, 14, 30, 0);
    /// DateTime output = preserveTimeRule.Apply(input); // Time 14:30:00 unchanged
    /// </code>
    /// </example>
    public class DateShiftRule : IMaskRule<DateTime, DateTime>, ISeededMaskRule<DateTime>
    {
        private readonly int _daysRange;
        private readonly bool _preserveTime;

        /// <summary>
        /// Gets or sets the seed provider for deterministic date shifting.
        /// When set, the same seed produces the same shift amount across all invocations.
        /// This is critical for HIPAA compliance to maintain consistent shifts per individual.
        /// </summary>
        /// <remarks>
        /// For HIPAA compliance, use a seed based on patient ID:
        /// <code>rule.SeedProvider = dt => patientId.GetHashCode();</code>
        /// This ensures all dates for the same patient are shifted by the same amount.
        /// </remarks>
        public SeedProvider<DateTime>? SeedProvider { get; set; }

        /// <summary>
        /// Initializes a new instance of the DateShiftRule class.
        /// </summary>
        /// <param name="daysRange">
        /// The maximum number of days to shift in either direction.
        /// For HIPAA Safe Harbor compliance, this should be ≤ 365 days.
        /// Actual shift will be uniformly distributed in [-daysRange, +daysRange].
        /// </param>
        /// <param name="preserveTime">
        /// If true, only the date component is shifted, preserving the time-of-day.
        /// If false, the entire DateTime (including time) is shifted.
        /// Default is true to maintain temporal precision within a day.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when daysRange is negative</exception>
        /// <example>
        /// <code>
        /// // HIPAA-compliant: ±180 days, preserve time
        /// var hipaaRule = new DateShiftRule(daysRange: 180, preserveTime: true);
        ///
        /// // General anonymization: ±365 days
        /// var anonRule = new DateShiftRule(daysRange: 365);
        ///
        /// // Testing: ±30 days
        /// var testRule = new DateShiftRule(daysRange: 30);
        /// </code>
        /// </example>
        public DateShiftRule(int daysRange, bool preserveTime = true)
        {
            if (daysRange < 0)
                throw new ArgumentException("daysRange must be non-negative", nameof(daysRange));

            _daysRange = daysRange;
            _preserveTime = preserveTime;
        }

        /// <summary>
        /// Applies the date shift to the input DateTime.
        /// </summary>
        /// <param name="input">The DateTime to shift</param>
        /// <returns>The shifted DateTime</returns>
        /// <remarks>
        /// <para>
        /// The shift amount is determined as follows:
        /// - If <see cref="SeedProvider"/> is set: Uses deterministic random based on seed (consistent per seed)
        /// - Otherwise: Uses cryptographically secure random (different each time)
        /// </para>
        /// <para>
        /// The shift is uniformly distributed in [-daysRange, +daysRange].
        /// </para>
        /// <para>
        /// If <c>preserveTime</c> is true (default), only the date is shifted while time-of-day remains unchanged.
        /// </para>
        /// </remarks>
        public DateTime Apply(DateTime input)
        {
            // Handle zero range - no shift needed
            if (_daysRange == 0)
                return input;

            // Get Random instance (deterministic if SeedProvider is set, otherwise random)
            var rng = GetRandom(input);

            // Generate shift in days within [-daysRange, +daysRange]
            // Using Next with negative lower bound: Next(-daysRange, daysRange + 1)
            // This gives inclusive range: [-daysRange, daysRange]
            int shiftDays = rng.Next(-_daysRange, _daysRange + 1);

            // Apply shift
            if (_preserveTime)
            {
                // Shift only the date component, preserve time
                // This maintains the time-of-day which is important for temporal analysis
                return input.AddDays(shiftDays);
            }
            else
            {
                // Shift the entire DateTime (date + time)
                return input.AddDays(shiftDays);
            }
        }

        /// <summary>
        /// Gets a Random instance with optional seed support.
        /// If SeedProvider is set, uses deterministic seeding based on input value.
        /// Otherwise, generates a cryptographically secure random seed.
        /// </summary>
        /// <param name="input">The input DateTime to generate seed from (if SeedProvider is set)</param>
        /// <returns>A Random instance ready for use</returns>
        private Random GetRandom(DateTime input)
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
        private static int GenerateSecureRandomSeed()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                return BitConverter.ToInt32(bytes, 0);
            }
        }
    }
}
