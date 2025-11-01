using System;
using Xunit;
using ITW.FluentMasker.MaskRules;
using static ITW.FluentMasker.MaskRules.TimeBucketOffsetRule;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Comprehensive unit tests for TimeBucketOffsetRule covering timezone-aware timestamp bucketing.
    /// </summary>
    public class TimeBucketOffsetRuleTests
    {
        #region Timezone Preservation Tests

        [Fact]
        public void Hour_PreservesTimezone_EST()
        {
            // Arrange
            var rule = new TimeBucketOffsetRule(Granularity.Hour);
            var offset = TimeSpan.FromHours(-5); // EST
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTimeOffset(2025, 10, 31, 14, 0, 0, offset), result);
            Assert.Equal(offset, result.Offset);
        }

        [Fact]
        public void Hour_PreservesTimezone_PST()
        {
            // Arrange
            var rule = new TimeBucketOffsetRule(Granularity.Hour);
            var offset = TimeSpan.FromHours(-8); // PST
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTimeOffset(2025, 10, 31, 14, 0, 0, offset), result);
            Assert.Equal(offset, result.Offset);
        }

        [Fact]
        public void Hour_PreservesTimezone_CET()
        {
            // Arrange
            var rule = new TimeBucketOffsetRule(Granularity.Hour);
            var offset = TimeSpan.FromHours(1); // CET
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTimeOffset(2025, 10, 31, 14, 0, 0, offset), result);
            Assert.Equal(offset, result.Offset);
        }

        [Fact]
        public void Hour_PreservesTimezone_UTC()
        {
            // Arrange
            var rule = new TimeBucketOffsetRule(Granularity.Hour);
            var offset = TimeSpan.Zero; // UTC
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTimeOffset(2025, 10, 31, 14, 0, 0, offset), result);
            Assert.Equal(offset, result.Offset);
        }

        [Fact]
        public void Day_PreservesTimezone()
        {
            // Arrange
            var rule = new TimeBucketOffsetRule(Granularity.Day);
            var offset = TimeSpan.FromHours(5.5); // IST (India Standard Time)
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTimeOffset(2025, 10, 31, 0, 0, 0, offset), result);
            Assert.Equal(offset, result.Offset);
        }

        [Fact]
        public void Week_PreservesTimezone()
        {
            // Arrange
            var rule = new TimeBucketOffsetRule(Granularity.Week);
            var offset = TimeSpan.FromHours(10); // AEST (Australian Eastern Standard Time)
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset); // Friday

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(new DateTimeOffset(2025, 10, 27, 0, 0, 0, offset), result);
            Assert.Equal(offset, result.Offset);
            Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        }

        #endregion

        #region All Granularity Tests with Timezone

        [Fact]
        public void Hour_BucketsCorrectly_WithTimezone()
        {
            var rule = new TimeBucketOffsetRule(Granularity.Hour);
            var offset = TimeSpan.FromHours(-5);
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            var result = rule.Apply(input);

            Assert.Equal(14, result.Hour);
            Assert.Equal(0, result.Minute);
            Assert.Equal(0, result.Second);
        }

        [Fact]
        public void Day_BucketsCorrectly_WithTimezone()
        {
            var rule = new TimeBucketOffsetRule(Granularity.Day);
            var offset = TimeSpan.FromHours(-5);
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            var result = rule.Apply(input);

            Assert.Equal(new DateTimeOffset(2025, 10, 31, 0, 0, 0, offset), result);
        }

        [Fact]
        public void Week_BucketsToMonday_WithTimezone()
        {
            var rule = new TimeBucketOffsetRule(Granularity.Week);
            var offset = TimeSpan.FromHours(-5);
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset); // Friday

            var result = rule.Apply(input);

            Assert.Equal(new DateTimeOffset(2025, 10, 27, 0, 0, 0, offset), result);
            Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
        }

        [Fact]
        public void Month_BucketsCorrectly_WithTimezone()
        {
            var rule = new TimeBucketOffsetRule(Granularity.Month);
            var offset = TimeSpan.FromHours(-5);
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            var result = rule.Apply(input);

            Assert.Equal(new DateTimeOffset(2025, 10, 1, 0, 0, 0, offset), result);
        }

        [Theory]
        [InlineData(1, 1)]  // Q1
        [InlineData(4, 4)]  // Q2
        [InlineData(7, 7)]  // Q3
        [InlineData(10, 10)] // Q4
        public void Quarter_BucketsCorrectly_WithTimezone(int inputMonth, int expectedMonth)
        {
            var rule = new TimeBucketOffsetRule(Granularity.Quarter);
            var offset = TimeSpan.FromHours(-5);
            var input = new DateTimeOffset(2025, inputMonth, 15, 14, 32, 15, offset);

            var result = rule.Apply(input);

            Assert.Equal(new DateTimeOffset(2025, expectedMonth, 1, 0, 0, 0, offset), result);
        }

        [Fact]
        public void Year_BucketsCorrectly_WithTimezone()
        {
            var rule = new TimeBucketOffsetRule(Granularity.Year);
            var offset = TimeSpan.FromHours(-5);
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            var result = rule.Apply(input);

            Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, offset), result);
        }

        #endregion

        #region Cross-Timezone Scenarios

        [Fact]
        public void SameInstant_DifferentTimezones_BucketDifferently()
        {
            // Arrange - Same instant in time, different timezones
            var rule = new TimeBucketOffsetRule(Granularity.Hour);
            var utcTime = new DateTimeOffset(2025, 10, 31, 19, 32, 15, TimeSpan.Zero); // 7:32 PM UTC
            var estTime = new DateTimeOffset(2025, 10, 31, 14, 32, 15, TimeSpan.FromHours(-5)); // 2:32 PM EST (same instant)

            // Act
            var utcResult = rule.Apply(utcTime);
            var estResult = rule.Apply(estTime);

            // Assert - They should bucket to different local hours
            Assert.Equal(19, utcResult.Hour); // 7 PM UTC
            Assert.Equal(14, estResult.Hour); // 2 PM EST
            Assert.Equal(utcTime.UtcDateTime.Hour, estTime.UtcDateTime.Hour); // Verify same instant
        }

        [Fact]
        public void DaylightSavingTime_HandlesCorrectly()
        {
            // Arrange - Time during DST transition (example)
            var rule = new TimeBucketOffsetRule(Granularity.Day);
            var beforeDST = new DateTimeOffset(2025, 3, 9, 1, 30, 0, TimeSpan.FromHours(-8)); // PST
            var afterDST = new DateTimeOffset(2025, 3, 9, 3, 30, 0, TimeSpan.FromHours(-7)); // PDT

            // Act
            var resultBefore = rule.Apply(beforeDST);
            var resultAfter = rule.Apply(afterDST);

            // Assert - Should preserve their respective offsets
            Assert.Equal(TimeSpan.FromHours(-8), resultBefore.Offset);
            Assert.Equal(TimeSpan.FromHours(-7), resultAfter.Offset);
        }

        #endregion

        #region Real-World Scenarios

        [Fact]
        public void RealWorld_GlobalLogs_PreservesLocalTime()
        {
            // Scenario: Logging system with servers in multiple timezones
            var rule = new TimeBucketOffsetRule(Granularity.Hour);

            var usLog = new DateTimeOffset(2025, 10, 31, 14, 32, 15, TimeSpan.FromHours(-5)); // US East
            var euLog = new DateTimeOffset(2025, 10, 31, 20, 32, 15, TimeSpan.FromHours(1));  // EU Central
            var asiaLog = new DateTimeOffset(2025, 11, 1, 3, 32, 15, TimeSpan.FromHours(8));  // Asia

            var usResult = rule.Apply(usLog);
            var euResult = rule.Apply(euLog);
            var asiaResult = rule.Apply(asiaLog);

            // Each should preserve local timezone
            Assert.Equal(TimeSpan.FromHours(-5), usResult.Offset);
            Assert.Equal(TimeSpan.FromHours(1), euResult.Offset);
            Assert.Equal(TimeSpan.FromHours(8), asiaResult.Offset);

            // Each should bucket to their local hour
            Assert.Equal(14, usResult.Hour);
            Assert.Equal(20, euResult.Hour);
            Assert.Equal(3, asiaResult.Hour);
        }

        [Fact]
        public void RealWorld_FinancialReporting_QuarterlyBucketing()
        {
            // Scenario: Financial transactions in different timezones
            var rule = new TimeBucketOffsetRule(Granularity.Quarter);

            var nyTransaction = new DateTimeOffset(2025, 10, 15, 9, 0, 0, TimeSpan.FromHours(-4)); // NY
            var londonTransaction = new DateTimeOffset(2025, 11, 20, 14, 0, 0, TimeSpan.Zero);     // London
            var tokyoTransaction = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.FromHours(9)); // Tokyo

            var nyResult = rule.Apply(nyTransaction);
            var londonResult = rule.Apply(londonTransaction);
            var tokyoResult = rule.Apply(tokyoTransaction);

            // All Q4 transactions should bucket to Q4 start in their local timezone
            Assert.Equal(10, nyResult.Month);
            Assert.Equal(10, londonResult.Month);
            Assert.Equal(10, tokyoResult.Month);

            // Timezones should be preserved
            Assert.Equal(TimeSpan.FromHours(-4), nyResult.Offset);
            Assert.Equal(TimeSpan.Zero, londonResult.Offset);
            Assert.Equal(TimeSpan.FromHours(9), tokyoResult.Offset);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Performance_ProcessesAtLeast100kOperationsPerSecond()
        {
            var rule = new TimeBucketOffsetRule(Granularity.Day);
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, TimeSpan.FromHours(-5));
            var iterations = 100_000;

            var startTime = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++)
            {
                rule.Apply(input);
            }
            var elapsed = DateTime.UtcNow - startTime;

            Assert.True(elapsed.TotalSeconds < 1.0,
                $"Performance test failed: {iterations} operations took {elapsed.TotalMilliseconds}ms (expected < 1000ms)");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void EdgeCase_MinDateTimeOffset()
        {
            var rule = new TimeBucketOffsetRule(Granularity.Day);
            var input = DateTimeOffset.MinValue;

            // Act & Assert - Should not throw
            var result = rule.Apply(input);

            // MinValue is already at start of day
            Assert.Equal(DateTimeOffset.MinValue, result);
        }

        [Fact]
        public void EdgeCase_MaxDateTimeOffset()
        {
            var rule = new TimeBucketOffsetRule(Granularity.Day);
            var input = DateTimeOffset.MaxValue;

            // Act & Assert - Should not throw
            var result = rule.Apply(input);

            // Should return the start of the day for MaxValue
            Assert.Equal(new DateTimeOffset(9999, 12, 31, 0, 0, 0, TimeSpan.Zero), result);
        }

        [Fact]
        public void EdgeCase_PositiveOffset_Fractional()
        {
            // Some timezones have fractional hour offsets (e.g., India +5:30)
            var rule = new TimeBucketOffsetRule(Granularity.Hour);
            var offset = TimeSpan.FromHours(5.5);
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            var result = rule.Apply(input);

            Assert.Equal(offset, result.Offset);
            Assert.Equal(14, result.Hour);
        }

        [Fact]
        public void EdgeCase_NegativeOffset_Fractional()
        {
            // Newfoundland has -3:30 offset
            var rule = new TimeBucketOffsetRule(Granularity.Hour);
            var offset = TimeSpan.FromHours(-3.5);
            var input = new DateTimeOffset(2025, 10, 31, 14, 32, 15, offset);

            var result = rule.Apply(input);

            Assert.Equal(offset, result.Offset);
            Assert.Equal(14, result.Hour);
        }

        #endregion

        #region Week Edge Cases with Timezone

        [Fact]
        public void Week_CrossesDateBoundary_WithTimezone()
        {
            // Sunday in PST might be Monday in UTC
            var rule = new TimeBucketOffsetRule(Granularity.Week);
            var sundayPST = new DateTimeOffset(2025, 11, 2, 23, 0, 0, TimeSpan.FromHours(-8)); // Sunday 11PM PST

            var result = rule.Apply(sundayPST);

            // Should still bucket to Monday in local (PST) time
            Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
            Assert.Equal(TimeSpan.FromHours(-8), result.Offset);
        }

        #endregion
    }
}
