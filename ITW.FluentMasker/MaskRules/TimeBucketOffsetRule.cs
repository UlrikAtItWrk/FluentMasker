using System;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Generalizes timezone-aware timestamps (DateTimeOffset) to a specified time granularity.
    /// Preserves timezone offset information while reducing temporal precision.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This rule is the timezone-aware version of <see cref="TimeBucketRule"/>.
    /// It preserves the original timezone offset while bucketing the time component.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Bucket with timezone preservation
    /// var rule = new TimeBucketOffsetRule(TimeBucketOffsetRule.Granularity.Hour);
    /// var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, TimeSpan.FromHours(-5)); // EST
    /// var result = rule.Apply(input);
    /// // Result: 2025-10-31 14:00:00 -05:00 (timezone preserved)
    /// </code>
    /// </example>
    public class TimeBucketOffsetRule : IMaskRule<DateTimeOffset, DateTimeOffset>
    {
        private readonly Granularity _granularity;

        /// <summary>
        /// Defines the level of time granularity for bucketing.
        /// </summary>
        public enum Granularity
        {
            /// <summary>
            /// Bucket to the start of the hour
            /// </summary>
            Hour,

            /// <summary>
            /// Bucket to the start of the day
            /// </summary>
            Day,

            /// <summary>
            /// Bucket to the start of the week (Monday)
            /// </summary>
            Week,

            /// <summary>
            /// Bucket to the start of the month
            /// </summary>
            Month,

            /// <summary>
            /// Bucket to the start of the quarter
            /// </summary>
            Quarter,

            /// <summary>
            /// Bucket to the start of the year
            /// </summary>
            Year
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeBucketOffsetRule"/> class.
        /// </summary>
        /// <param name="granularity">The time granularity level for bucketing</param>
        public TimeBucketOffsetRule(Granularity granularity)
        {
            _granularity = granularity;
        }

        /// <summary>
        /// Applies the time bucketing rule to reduce temporal precision while preserving timezone.
        /// </summary>
        /// <param name="input">The timezone-aware timestamp to bucket</param>
        /// <returns>The timestamp rounded down to the start of the specified granularity bucket with original timezone offset</returns>
        public DateTimeOffset Apply(DateTimeOffset input)
        {
            var offset = input.Offset;

            var bucketed = _granularity switch
            {
                Granularity.Hour => new DateTimeOffset(input.Year, input.Month, input.Day, input.Hour, 0, 0, offset),
                Granularity.Day => new DateTimeOffset(input.Year, input.Month, input.Day, 0, 0, 0, offset),
                Granularity.Week => GetWeekStart(input),
                Granularity.Month => new DateTimeOffset(input.Year, input.Month, 1, 0, 0, 0, offset),
                Granularity.Quarter => GetQuarterStart(input),
                Granularity.Year => new DateTimeOffset(input.Year, 1, 1, 0, 0, 0, offset),
                _ => throw new ArgumentOutOfRangeException(nameof(_granularity), _granularity, "Invalid granularity value")
            };

            return bucketed;
        }

        /// <summary>
        /// Gets the Monday of the week containing the input date.
        /// </summary>
        private DateTimeOffset GetWeekStart(DateTimeOffset date)
        {
            // Calculate days to subtract to get to Monday
            int daysFromMonday = ((int)date.DayOfWeek + 6) % 7;

            // Subtract days and set time to midnight
            var weekStart = date.AddDays(-daysFromMonday);
            return new DateTimeOffset(weekStart.Year, weekStart.Month, weekStart.Day, 0, 0, 0, date.Offset);
        }

        /// <summary>
        /// Gets the start of the quarter containing the input date.
        /// </summary>
        private DateTimeOffset GetQuarterStart(DateTimeOffset date)
        {
            int quarterStartMonth = ((date.Month - 1) / 3) * 3 + 1;
            return new DateTimeOffset(date.Year, quarterStartMonth, 1, 0, 0, 0, date.Offset);
        }
    }
}
