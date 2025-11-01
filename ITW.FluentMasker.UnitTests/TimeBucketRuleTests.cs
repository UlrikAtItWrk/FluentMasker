using System;
using Xunit;
using ITW.FluentMasker.MaskRules;
using static ITW.FluentMasker.MaskRules.TimeBucketRule;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Comprehensive unit tests for TimeBucketRule covering all granularity levels,
    /// edge cases, and PRD requirements.
    /// </summary>
    public class TimeBucketRuleTests
    {
        #region Hour Granularity Tests

        [Fact]
        public void Hour_BucketsToStartOfHour()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Hour);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 10, 31, 14, 0, 0), result);
        }

        [Fact]
        public void Hour_PreservesDateKind_Utc()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Hour);
            var input = new DateTime(2025, 10, 31, 14, 32, 15, DateTimeKind.Utc);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        [Fact]
        public void Hour_PreservesDateKind_Local()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Hour);
            var input = new DateTime(2025, 10, 31, 14, 32, 15, DateTimeKind.Local);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(DateTimeKind.Local, result.Kind);
        }

        [Fact]
        public void Hour_HandlesAlreadyBucketed()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Hour);
            var input = new DateTime(2025, 10, 31, 14, 0, 0);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Hour_HandlesMidnight()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Hour);
            var input = new DateTime(2025, 10, 31, 0, 59, 59);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 10, 31, 0, 0, 0), result);
        }

        [Fact]
        public void Hour_HandlesEndOfDay()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Hour);
            var input = new DateTime(2025, 10, 31, 23, 59, 59);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 10, 31, 23, 0, 0), result);
        }

        #endregion

        #region Day Granularity Tests

        [Fact]
        public void Day_BucketsToStartOfDay()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Day);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 10, 31, 0, 0, 0), result);
        }

        [Fact]
        public void Day_HandlesAlreadyBucketed()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Day);
            var input = new DateTime(2025, 10, 31, 0, 0, 0);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Day_PreservesDateKind()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Day);
            var input = new DateTime(2025, 10, 31, 14, 32, 15, DateTimeKind.Utc);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(DateTimeKind.Utc, result.Kind);
        }

        #endregion

        #region Week Granularity Tests

        [Theory]
        [InlineData("2025-10-27", "2025-10-27")] // Monday → same Monday
        [InlineData("2025-10-28", "2025-10-27")] // Tuesday → previous Monday
        [InlineData("2025-10-29", "2025-10-27")] // Wednesday → previous Monday
        [InlineData("2025-10-30", "2025-10-27")] // Thursday → previous Monday
        [InlineData("2025-10-31", "2025-10-27")] // Friday → previous Monday (PRD example)
        [InlineData("2025-11-01", "2025-10-27")] // Saturday → previous Monday
        [InlineData("2025-11-02", "2025-10-27")] // Sunday → previous Monday
        public void Week_BucketsToMonday(string inputStr, string expectedStr)
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Week);
            var input = DateTime.Parse(inputStr).AddHours(14).AddMinutes(32).AddSeconds(15);
            var expected = DateTime.Parse(expectedStr);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Week_PRDExample_FridayToMonday()
        {
            // Arrange - PRD example: 2025-10-31 (Friday) → 2025-10-27 (Monday)
            var rule = new TimeBucketRule(Granularity.Week);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 10, 27, 0, 0, 0), result);
            Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        }

        [Fact]
        public void Week_HandlesYearBoundary()
        {
            // Arrange - Sunday Jan 4, 2026 should bucket to Monday Dec 29, 2025
            var rule = new TimeBucketRule(Granularity.Week);
            var input = new DateTime(2026, 1, 4, 10, 30, 0); // Sunday

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 12, 29, 0, 0, 0), result);
            Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        }

        [Fact]
        public void Week_HandlesLeapYear()
        {
            // Arrange - Feb 29, 2024 (Thursday, leap year) → Feb 26, 2024 (Monday)
            var rule = new TimeBucketRule(Granularity.Week);
            var input = new DateTime(2024, 2, 29, 10, 30, 0); // Thursday

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2024, 2, 26, 0, 0, 0), result);
            Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        }

        #endregion

        #region Month Granularity Tests

        [Fact]
        public void Month_BucketsToStartOfMonth()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Month);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 10, 1, 0, 0, 0), result);
        }

        [Fact]
        public void Month_HandlesFirstDayOfMonth()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Month);
            var input = new DateTime(2025, 10, 1, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 10, 1, 0, 0, 0), result);
        }

        [Fact]
        public void Month_HandlesLastDayOfMonth()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Month);
            var input = new DateTime(2025, 2, 28, 14, 32, 15); // Feb 28 (non-leap year)

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 2, 1, 0, 0, 0), result);
        }

        [Fact]
        public void Month_HandlesLeapYear()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Month);
            var input = new DateTime(2024, 2, 29, 14, 32, 15); // Feb 29 (leap year)

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2024, 2, 1, 0, 0, 0), result);
        }

        #endregion

        #region Quarter Granularity Tests

        [Theory]
        [InlineData(1, 1)]  // Q1: Jan → Jan 1
        [InlineData(2, 1)]  // Q1: Feb → Jan 1
        [InlineData(3, 1)]  // Q1: Mar → Jan 1
        [InlineData(4, 4)]  // Q2: Apr → Apr 1
        [InlineData(5, 4)]  // Q2: May → Apr 1
        [InlineData(6, 4)]  // Q2: Jun → Apr 1
        [InlineData(7, 7)]  // Q3: Jul → Jul 1
        [InlineData(8, 7)]  // Q3: Aug → Jul 1
        [InlineData(9, 7)]  // Q3: Sep → Jul 1
        [InlineData(10, 10)] // Q4: Oct → Oct 1 (PRD example)
        [InlineData(11, 10)] // Q4: Nov → Oct 1
        [InlineData(12, 10)] // Q4: Dec → Oct 1
        public void Quarter_BucketsToQuarterStart(int inputMonth, int expectedMonth)
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Quarter);
            var input = new DateTime(2025, inputMonth, 15, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, expectedMonth, 1, 0, 0, 0), result);
        }

        [Fact]
        public void Quarter_PRDExample_OctoberToQ4Start()
        {
            // Arrange - PRD example: 2025-10-31 14:32:15 → 2025-10-01 00:00:00 (Q4)
            var rule = new TimeBucketRule(Granularity.Quarter);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 10, 1, 0, 0, 0), result);
        }

        [Fact]
        public void Quarter_HandlesAlreadyBucketed()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Quarter);
            var input = new DateTime(2025, 10, 1, 0, 0, 0);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        #endregion

        #region Year Granularity Tests

        [Fact]
        public void Year_BucketsToStartOfYear()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Year);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0), result);
        }

        [Fact]
        public void Year_HandlesFirstDayOfYear()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Year);
            var input = new DateTime(2025, 1, 1, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0), result);
        }

        [Fact]
        public void Year_HandlesLastDayOfYear()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Year);
            var input = new DateTime(2025, 12, 31, 23, 59, 59);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0), result);
        }

        [Fact]
        public void Year_HandlesLeapYear()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Year);
            var input = new DateTime(2024, 2, 29, 14, 32, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0), result);
        }

        #endregion

        #region Boundary Value Tests

        [Fact]
        public void BoundaryValue_MinDateTime()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Day);
            var input = DateTime.MinValue;

            // Act & Assert - Should not throw
            var result = rule.Apply(input);

            // MinValue is already at start of day, so should return MinValue
            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        public void BoundaryValue_MaxDateTime()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Day);
            var input = DateTime.MaxValue;

            // Act & Assert - Should not throw
            var result = rule.Apply(input);

            // Should return the start of the day for MaxValue
            Assert.Equal(new DateTime(9999, 12, 31, 0, 0, 0), result);
        }

        [Fact]
        public void BoundaryValue_Year1_January1()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Week);
            var input = new DateTime(1, 1, 1, 12, 0, 0);

            // Act
            var result = rule.Apply(input);

            // Assert - Should not throw, should handle early dates
            Assert.True(result.Year >= 1);
        }

        #endregion

        #region Real-World Scenarios

        [Fact]
        public void RealWorld_LogMasking_HourlyBucketing()
        {
            // Scenario: Mask audit log timestamps to hourly precision
            var rule = new TimeBucketRule(Granularity.Hour);
            var logEntries = new[]
            {
                new DateTime(2025, 10, 31, 14, 05, 32),
                new DateTime(2025, 10, 31, 14, 23, 47),
                new DateTime(2025, 10, 31, 14, 58, 12),
            };

            // All entries should bucket to same hour
            var results = Array.ConvertAll(logEntries, e => rule.Apply(e));

            Assert.All(results, r => Assert.Equal(new DateTime(2025, 10, 31, 14, 0, 0), r));
        }

        [Fact]
        public void RealWorld_Analytics_DailyAggregation()
        {
            // Scenario: Aggregate events by day for analytics
            var rule = new TimeBucketRule(Granularity.Day);
            var events = new[]
            {
                new DateTime(2025, 10, 31, 02, 15, 0),
                new DateTime(2025, 10, 31, 09, 30, 0),
                new DateTime(2025, 10, 31, 18, 45, 0),
                new DateTime(2025, 10, 31, 23, 50, 0),
            };

            // All events on same day should bucket to same date
            var results = Array.ConvertAll(events, e => rule.Apply(e));

            Assert.All(results, r => Assert.Equal(new DateTime(2025, 10, 31, 0, 0, 0), r));
        }

        [Fact]
        public void RealWorld_Reporting_QuarterlyBucketing()
        {
            // Scenario: Financial reporting by quarter
            var rule = new TimeBucketRule(Granularity.Quarter);
            var transactions = new[]
            {
                new DateTime(2025, 10, 1),  // Q4 start
                new DateTime(2025, 11, 15), // Q4 middle
                new DateTime(2025, 12, 31), // Q4 end
            };

            // All Q4 transactions should bucket to Q4 start
            var results = Array.ConvertAll(transactions, t => rule.Apply(t));

            Assert.All(results, r => Assert.Equal(new DateTime(2025, 10, 1, 0, 0, 0), r));
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Performance_ProcessesAtLeast100kOperationsPerSecond()
        {
            // Arrange
            var rule = new TimeBucketRule(Granularity.Day);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);
            var iterations = 100_000;

            // Act
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++)
            {
                rule.Apply(input);
            }
            var elapsed = DateTime.UtcNow - startTime;

            // Assert - Should complete 100k operations in less than 1 second
            Assert.True(elapsed.TotalSeconds < 1.0,
                $"Performance test failed: {iterations} operations took {elapsed.TotalMilliseconds}ms (expected < 1000ms)");
        }

        [Fact]
        public void Performance_WeekCalculation_IsEfficient()
        {
            // Week calculation is the most complex, test it specifically
            var rule = new TimeBucketRule(Granularity.Week);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);
            var iterations = 100_000;

            var startTime = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++)
            {
                rule.Apply(input);
            }
            var elapsed = DateTime.UtcNow - startTime;

            // Should still be fast even for complex calculation
            Assert.True(elapsed.TotalSeconds < 1.0,
                $"Week performance test failed: {iterations} operations took {elapsed.TotalMilliseconds}ms");
        }

        #endregion

        #region PRD Examples

        [Fact]
        public void PRDExample_HourGranularity()
        {
            // PRD: 2025-10-31 14:32:15 → 2025-10-31 14:00:00
            var rule = new TimeBucketRule(Granularity.Hour);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);
            var expected = new DateTime(2025, 10, 31, 14, 0, 0);

            var result = rule.Apply(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void PRDExample_DayGranularity()
        {
            // PRD: → 2025-10-31 00:00:00
            var rule = new TimeBucketRule(Granularity.Day);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);
            var expected = new DateTime(2025, 10, 31, 0, 0, 0);

            var result = rule.Apply(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void PRDExample_WeekGranularity()
        {
            // PRD: → 2025-10-27 00:00:00 (Monday)
            var rule = new TimeBucketRule(Granularity.Week);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);
            var expected = new DateTime(2025, 10, 27, 0, 0, 0);

            var result = rule.Apply(input);

            Assert.Equal(expected, result);
            Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        }

        [Fact]
        public void PRDExample_MonthGranularity()
        {
            // PRD: → 2025-10-01 00:00:00
            var rule = new TimeBucketRule(Granularity.Month);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);
            var expected = new DateTime(2025, 10, 1, 0, 0, 0);

            var result = rule.Apply(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void PRDExample_QuarterGranularity()
        {
            // PRD: → 2025-10-01 00:00:00 (Q4)
            var rule = new TimeBucketRule(Granularity.Quarter);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);
            var expected = new DateTime(2025, 10, 1, 0, 0, 0);

            var result = rule.Apply(input);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void PRDExample_YearGranularity()
        {
            // PRD: → 2025-01-01 00:00:00
            var rule = new TimeBucketRule(Granularity.Year);
            var input = new DateTime(2025, 10, 31, 14, 32, 15);
            var expected = new DateTime(2025, 1, 1, 0, 0, 0);

            var result = rule.Apply(input);

            Assert.Equal(expected, result);
        }

        #endregion
    }
}
