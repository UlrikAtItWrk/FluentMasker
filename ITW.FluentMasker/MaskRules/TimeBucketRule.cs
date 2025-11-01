using System;

namespace ITW.FluentMasker.MaskRules
{
    /// <summary>
    /// Generalizes timestamps to a specified time granularity (hour, day, week, month, quarter, year).
    /// Useful for privacy-preserving analytics where exact timestamps are not required.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This rule reduces temporal precision while preserving time-based patterns for analytics.
    /// Common use cases include:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description><b>Analytics Privacy</b>: Publish hourly/daily aggregations without exact timestamps</description>
    /// </item>
    /// <item>
    /// <description><b>Log Masking</b>: Reduce precision of audit logs for GDPR compliance</description>
    /// </item>
    /// <item>
    /// <description><b>Time-Series Data</b>: Align events to consistent time buckets</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Bucket to hour precision
    /// var hourRule = new TimeBucketRule(TimeBucketRule.Granularity.Hour);
    /// var result = hourRule.Apply(new DateTime(2025, 10, 31, 14, 32, 15));
    /// // Result: 2025-10-31 14:00:00
    ///
    /// // Bucket to week precision (Monday-aligned)
    /// var weekRule = new TimeBucketRule(TimeBucketRule.Granularity.Week);
    /// var result2 = weekRule.Apply(new DateTime(2025, 10, 31, 14, 32, 15)); // Friday
    /// // Result: 2025-10-27 00:00:00 (Previous Monday)
    ///
    /// // Bucket to quarter precision
    /// var quarterRule = new TimeBucketRule(TimeBucketRule.Granularity.Quarter);
    /// var result3 = quarterRule.Apply(new DateTime(2025, 10, 31, 14, 32, 15));
    /// // Result: 2025-10-01 00:00:00 (Q4 start)
    /// </code>
    /// </example>
    public class TimeBucketRule : IMaskRule<DateTime, DateTime>
    {
        private readonly Granularity _granularity;

        /// <summary>
        /// Defines the level of time granularity for bucketing.
        /// </summary>
        public enum Granularity
        {
            /// <summary>
            /// Bucket to the start of the hour (e.g., 2025-10-31 14:32:15 → 2025-10-31 14:00:00)
            /// </summary>
            Hour,

            /// <summary>
            /// Bucket to the start of the day (e.g., 2025-10-31 14:32:15 → 2025-10-31 00:00:00)
            /// </summary>
            Day,

            /// <summary>
            /// Bucket to the start of the week (Monday) (e.g., 2025-10-31 14:32:15 → 2025-10-27 00:00:00)
            /// </summary>
            Week,

            /// <summary>
            /// Bucket to the start of the month (e.g., 2025-10-31 14:32:15 → 2025-10-01 00:00:00)
            /// </summary>
            Month,

            /// <summary>
            /// Bucket to the start of the quarter (e.g., 2025-10-31 14:32:15 → 2025-10-01 00:00:00 for Q4)
            /// </summary>
            Quarter,

            /// <summary>
            /// Bucket to the start of the year (e.g., 2025-10-31 14:32:15 → 2025-01-01 00:00:00)
            /// </summary>
            Year
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeBucketRule"/> class.
        /// </summary>
        /// <param name="granularity">The time granularity level for bucketing</param>
        public TimeBucketRule(Granularity granularity)
        {
            _granularity = granularity;
        }

        /// <summary>
        /// Applies the time bucketing rule to reduce temporal precision.
        /// </summary>
        /// <param name="input">The timestamp to bucket</param>
        /// <returns>The timestamp rounded down to the start of the specified granularity bucket</returns>
        public DateTime Apply(DateTime input)
        {
            return _granularity switch
            {
                Granularity.Hour => new DateTime(input.Year, input.Month, input.Day, input.Hour, 0, 0, input.Kind),
                Granularity.Day => input.Date,
                Granularity.Week => GetWeekStart(input),
                Granularity.Month => new DateTime(input.Year, input.Month, 1, 0, 0, 0, input.Kind),
                Granularity.Quarter => GetQuarterStart(input),
                Granularity.Year => new DateTime(input.Year, 1, 1, 0, 0, 0, input.Kind),
                _ => throw new ArgumentOutOfRangeException(nameof(_granularity), _granularity, "Invalid granularity value")
            };
        }

        /// <summary>
        /// Gets the Monday of the week containing the input date.
        /// </summary>
        /// <param name="date">The date to find the week start for</param>
        /// <returns>The Monday (start of the week) containing the input date</returns>
        private DateTime GetWeekStart(DateTime date)
        {
            // Get the date component only
            var dateOnly = date.Date;

            // Calculate days to subtract to get to Monday
            // DayOfWeek: Sunday=0, Monday=1, Tuesday=2, ..., Saturday=6
            // For Sunday (0), we want to go back 6 days to Monday
            // For Monday (1), we want to stay at 0 days
            // For Tuesday (2), we want to go back 1 day to Monday, etc.
            int daysFromMonday = ((int)dateOnly.DayOfWeek + 6) % 7;

            return dateOnly.AddDays(-daysFromMonday);
        }

        /// <summary>
        /// Gets the start of the quarter containing the input date.
        /// Quarters: Q1=Jan-Mar, Q2=Apr-Jun, Q3=Jul-Sep, Q4=Oct-Dec
        /// </summary>
        /// <param name="date">The date to find the quarter start for</param>
        /// <returns>The first day of the quarter containing the input date</returns>
        private DateTime GetQuarterStart(DateTime date)
        {
            int quarterStartMonth = ((date.Month - 1) / 3) * 3 + 1;
            return new DateTime(date.Year, quarterStartMonth, 1, 0, 0, 0, date.Kind);
        }
    }
}
