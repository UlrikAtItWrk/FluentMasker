using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Masks dates and ages for GDPR and HIPAA Safe Harbor compliance.
    /// Supports multiple masking modes including year-only, date shifting, and age generalization.
    /// </summary>
    /// <remarks>
    /// <para><b>GDPR Compliance:</b></para>
    /// <para>
    /// Dates of birth and other personal dates are considered personal data under GDPR.
    /// This rule provides multiple strategies to minimize data while maintaining analytical utility.
    /// </para>
    /// <para><b>HIPAA Safe Harbor Compliance:</b></para>
    /// <para>
    /// Per 45 CFR §164.514(b)(2):
    /// - All elements of dates (except year) must be removed or generalized
    /// - Ages over 89 must be aggregated to "90+" category
    /// - Dates can be shifted by a consistent random offset (±365 days) per individual
    /// </para>
    /// <para><b>Masking Modes:</b></para>
    /// <list type="bullet">
    /// <item><description><b>YearOnly:</b> Keeps year, masks month/day ? "1982-**-**"</description></item>
    /// <item><description><b>DateShift:</b> Shifts date by consistent offset (requires seed) ? "1982-08-15"</description></item>
    /// <item><description><b>Redact:</b> Completely removes date ? "[REDACTED]"</description></item>
    /// </list>
    /// <para><b>Age Handling:</b></para>
    /// <list type="bullet">
    /// <item><description>Ages ?90 are always masked as "90+"</description></item>
    /// <item><description>Optionally bucket ages into ranges (e.g., 0-5, 6-10, ...)</description></item>
    /// <item><description>Can compute age from date of birth automatically</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Year-only masking (default)
    /// var rule1 = new DateAgeMaskRule();
    /// var result1 = rule1.Apply("1982-11-23");
    /// // result1 = "1982-**-**"
    ///
    /// // Date shifting with consistent offset per patient
    /// var rule2 = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.DateShift, daysRange: 180);
    /// rule2.SeedProvider = value => patientId.GetHashCode();
    /// var result2 = rule2.Apply("1982-11-23");
    /// // result2 = "1983-04-12" (example - consistent per patient)
    ///
    /// // Age bucketing
    /// var rule3 = new DateAgeMaskRule(ageBucketing: true);
    /// var result3 = rule3.ApplyAge(42);
    /// // result3 = "40-49"
    ///
    /// // Ages over 89
    /// var result4 = rule3.ApplyAge(94);
    /// // result4 = "90+"
    ///
    /// // Handle datetime strings with timestamps
    /// var rule4 = new DateAgeMaskRule();
    /// var result5 = rule4.Apply("2023-09-18T14:23:00Z");
    /// // result5 = "2023-**-**"
    /// </code>
    /// </example>
    public class DateAgeMaskRule : IMaskRule<string, string>, ISeededMaskRule<string>
    {
        /// <summary>
        /// Defines the masking strategy for dates.
        /// </summary>
        public enum MaskingMode
        {
            /// <summary>
            /// Keeps year only, masks month and day as "**". Example: "1982-**-**"
            /// HIPAA compliant for most use cases.
            /// </summary>
            YearOnly,

            /// <summary>
            /// Shifts the date by a random offset (requires seed for consistency).
            /// Preserves day/month relationships. Example: "1982-11-23" ? "1983-04-15"
            /// HIPAA compliant when using consistent seed per individual.
            /// </summary>
            DateShift,

            /// <summary>
            /// Completely redacts the date. Example: "[REDACTED]"
            /// Maximum privacy, minimum utility.
            /// </summary>
            Redact
        }

        private readonly MaskingMode _mode;
        private readonly int _daysRange;
        private readonly bool _ageBucketing;
        private readonly int[] _ageBreaks;
        private readonly string[] _ageLabels;
        private readonly string _maskChar;
        private readonly string _separator;

        /// <summary>
        /// Gets or sets the seed provider for deterministic date shifting.
        /// Required when using DateShift mode for HIPAA compliance.
        /// </summary>
        /// <remarks>
        /// For HIPAA compliance, use a seed based on patient/subject ID:
        /// <code>rule.SeedProvider = value => patientId.GetHashCode();</code>
        /// This ensures all dates for the same individual are shifted by the same amount.
        /// </remarks>
        public SeedProvider<string>? SeedProvider { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateAgeMaskRule"/> class.
        /// </summary>
        /// <param name="mode">The masking strategy to use (default: YearOnly)</param>
        /// <param name="daysRange">
        /// For DateShift mode: maximum days to shift in either direction.
        /// For HIPAA compliance, should be ? 365. Default: 180.
        /// </param>
        /// <param name="ageBucketing">
        /// If true, ages are bucketed into ranges. If false, ages ?90 still become "90+".
        /// Default: false.
        /// </param>
        /// <param name="customAgeBreaks">
        /// Custom age bucket boundaries. If null, uses standard buckets:
        /// 0-5, 6-10, 11-20, 21-30, 31-40, 41-50, 51-60, 61-70, 71-80, 81-89, 90+
        /// </param>
        /// <param name="customAgeLabels">
        /// Custom age bucket labels. Must have length = customAgeBreaks.Length - 1.
        /// </param>
        /// <param name="maskChar">Character to use for masking month/day (default: "*")</param>
        /// <param name="separator">Separator for date components (default: "-")</param>
        /// <exception cref="ArgumentException">
        /// Thrown when daysRange is negative or customAgeLabels length doesn't match customAgeBreaks.
        /// </exception>
        public DateAgeMaskRule(
            MaskingMode mode = MaskingMode.YearOnly,
            int daysRange = 180,
            bool ageBucketing = false,
            int[]? customAgeBreaks = null,
            string[]? customAgeLabels = null,
            string maskChar = "*",
            string separator = "-")
        {
            if (daysRange < 0)
                throw new ArgumentException("daysRange must be non-negative", nameof(daysRange));

            _mode = mode;
            _daysRange = daysRange;
            _ageBucketing = ageBucketing;
            _maskChar = maskChar;
            _separator = separator;

            // Setup age bucketing
            if (customAgeBreaks != null && customAgeLabels != null)
            {
                if (customAgeBreaks.Length != customAgeLabels.Length + 1)
                    throw new ArgumentException("customAgeLabels must have length = customAgeBreaks.Length - 1");

                _ageBreaks = customAgeBreaks;
                _ageLabels = customAgeLabels;
            }
            else
            {
                // Standard HIPAA-compliant age buckets
                _ageBreaks = new[] { 0, 6, 11, 21, 31, 41, 51, 61, 71, 81, 90, 150 };
                _ageLabels = new[] { "0-5", "6-10", "11-20", "21-30", "31-40", "41-50", "51-60", "61-70", "71-80", "81-89", "90+" };
            }
        }

        /// <summary>
        /// Applies the date masking rule to the input string.
        /// </summary>
        /// <param name="input">
        /// The date string to mask. Supports formats:
        /// - ISO 8601: "1982-11-23", "2023-09-18T14:23:00Z"
        /// - Common: "11/23/1982", "23.11.1982"
        /// - Null and empty strings are returned unchanged
        /// </param>
        /// <returns>The masked date according to the configured mode</returns>
        /// <remarks>
        /// <para>
        /// The rule attempts to parse common date formats. If parsing fails,
        /// the original string is returned unchanged.
        /// </para>
        /// <para>
        /// For ISO 8601 timestamps with time component, the time is removed and
        /// only the date portion is masked.
        /// </para>
        /// </remarks>
        public string Apply(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Try to parse the date
            if (!TryParseDate(input, out DateTime date))
                return input; // Can't parse, return unchanged

            switch (_mode)
            {
                case MaskingMode.YearOnly:
                    return FormatYearOnly(date);

                case MaskingMode.DateShift:
                    var shifted = ShiftDate(date, input);
                    return shifted.ToString("yyyy-MM-dd");

                case MaskingMode.Redact:
                    return "[REDACTED]";

                default:
                    return input;
            }
        }

        /// <summary>
        /// Applies age masking rules to an integer age value.
        /// Ages ?90 are always masked as "90+".
        /// If ageBucketing is enabled, all ages are bucketed into ranges.
        /// </summary>
        /// <param name="age">The age to mask</param>
        /// <returns>The masked age string (e.g., "42", "40-49", or "90+")</returns>
        /// <example>
        /// <code>
        /// var rule = new DateAgeMaskRule(ageBucketing: true);
        /// var result1 = rule.ApplyAge(42);  // "40-49"
        /// var result2 = rule.ApplyAge(94);  // "90+"
        /// var result3 = rule.ApplyAge(7);   // "6-10"
        ///
        /// var simpleRule = new DateAgeMaskRule(ageBucketing: false);
        /// var result4 = simpleRule.ApplyAge(42);  // "42"
        /// var result5 = simpleRule.ApplyAge(94);  // "90+"
        /// </code>
        /// </example>
        public string ApplyAge(int age)
        {
            // If bucketing is disabled, enforce HIPAA 90+ rule
            if (!_ageBucketing && age >= 90)
                return "90+";

            // If bucketing is enabled, use the custom buckets (which may handle 90+ differently)
            if (_ageBucketing)
                return FindAgeBucket(age);

            // No bucketing and age < 90: return as-is
            return age.ToString();
        }

        /// <summary>
        /// Calculates age from a date of birth and applies age masking rules.
        /// Convenience method that combines age calculation with masking.
        /// </summary>
        /// <param name="dateOfBirth">The date of birth</param>
        /// <param name="referenceDate">
        /// The reference date to calculate age from (default: today).
        /// Useful for consistent age calculation in batch processing.
        /// </param>
        /// <returns>The masked age string</returns>
        /// <example>
        /// <code>
        /// var rule = new DateAgeMaskRule(ageBucketing: true);
        /// var result = rule.CalculateAndMaskAge(new DateTime(1982, 11, 23));
        /// // result = "40-49" (as of 2025)
        /// </code>
        /// </example>
        public string CalculateAndMaskAge(DateTime dateOfBirth, DateTime? referenceDate = null)
        {
            var reference = referenceDate ?? DateTime.Today;
            int age = CalculateAge(dateOfBirth, reference);
            return ApplyAge(age);
        }

        /// <summary>
        /// Tries to parse a date string from various common formats.
        /// </summary>
        private bool TryParseDate(string input, out DateTime date)
        {
            // Try ISO 8601 first (most common in APIs)
            if (DateTime.TryParse(input, out date))
                return true;

            // Try specific formats
            var formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "dd-MM-yyyy",
                "dd/MM/yyyy",
                "MM-dd-yyyy",
                "MM/dd/yyyy",
                "dd.MM.yyyy",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss.fffZ"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(input, format,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out date))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Formats a date as year-only with masked month/day.
        /// </summary>
        private string FormatYearOnly(DateTime date)
        {
            return $"{date.Year}{_separator}{_maskChar}{_maskChar}{_separator}{_maskChar}{_maskChar}";
        }

        /// <summary>
        /// Shifts a date by a random offset.
        /// If SeedProvider is set, uses deterministic shifting.
        /// </summary>
        private DateTime ShiftDate(DateTime date, string originalInput)
        {
            if (_daysRange == 0)
                return date;

            var rng = GetRandom(originalInput);
            int shiftDays = rng.Next(-_daysRange, _daysRange + 1);
            return date.AddDays(shiftDays);
        }

        /// <summary>
        /// Gets a Random instance with optional seed support.
        /// </summary>
        private Random GetRandom(string input)
        {
            if (SeedProvider != null)
            {
                int seed = SeedProvider(input);
                return new Random(seed);
            }
            else
            {
                return new Random(GenerateSecureRandomSeed());
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random seed value.
        /// </summary>
        private static int GenerateSecureRandomSeed()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                return BitConverter.ToInt32(bytes, 0);
            }
        }

        /// <summary>
        /// Finds the appropriate age bucket for a given age using binary search.
        /// </summary>
        private string FindAgeBucket(int age)
        {
            // Handle edge cases
            if (age < _ageBreaks[0])
                return _ageLabels[0];

            if (age >= _ageBreaks[_ageBreaks.Length - 1])
                return _ageLabels[_ageLabels.Length - 1];

            // Binary search for bucket
            int left = 0;
            int right = _ageBreaks.Length - 2;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                if (age >= _ageBreaks[mid] && age < _ageBreaks[mid + 1])
                    return _ageLabels[mid];
                else if (age < _ageBreaks[mid])
                    right = mid - 1;
                else
                    left = mid + 1;
            }

            return _ageLabels[_ageLabels.Length - 1];
        }

        /// <summary>
        /// Calculates age from date of birth to reference date.
        /// </summary>
        private int CalculateAge(DateTime dateOfBirth, DateTime referenceDate)
        {
            int age = referenceDate.Year - dateOfBirth.Year;

            // Adjust if birthday hasn't occurred yet this year
            if (referenceDate.Month < dateOfBirth.Month ||
                (referenceDate.Month == dateOfBirth.Month && referenceDate.Day < dateOfBirth.Day))
            {
                age--;
            }

            return age;
        }
    }
}
