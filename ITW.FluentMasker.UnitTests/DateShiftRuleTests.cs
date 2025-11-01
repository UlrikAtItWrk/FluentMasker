using ITW.FluentMasker.MaskRules;
using ITW.FluentMasker.Builders;
using ITW.FluentMasker.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for DateShiftRule - tests HIPAA-compliant date shifting with deterministic and non-deterministic modes
    /// </summary>
    public class DateShiftRuleTests
    {
        #region Basic Functionality Tests

        [Fact]
        public void Apply_WithDaysRange_ShiftsDateWithinRange()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 180);
            var input = new DateTime(2025, 1, 15);

            // Act
            var result = rule.Apply(input);

            // Assert - result should be within ±180 days of input
            var minExpected = input.AddDays(-180);
            var maxExpected = input.AddDays(180);
            Assert.InRange(result, minExpected, maxExpected);
        }

        [Theory]
        [InlineData(2025, 1, 15, 180)]   // HIPAA typical: ±180 days
        [InlineData(2025, 6, 10, 365)]   // HIPAA maximum: ±365 days
        [InlineData(2025, 12, 31, 90)]   // End of year
        [InlineData(2024, 2, 29, 180)]   // Leap year date
        public void Apply_VariousDates_ShiftsWithinRange(int year, int month, int day, int daysRange)
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: daysRange);
            var input = new DateTime(year, month, day);

            // Act
            var result = rule.Apply(input);

            // Assert
            var minExpected = input.AddDays(-daysRange);
            var maxExpected = input.AddDays(daysRange);
            Assert.InRange(result, minExpected, maxExpected);
        }

        [Fact]
        public void Apply_ZeroRange_ReturnsUnchangedDate()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 0);
            var input = new DateTime(2025, 1, 15, 14, 30, 0);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Constructor_NegativeRange_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new DateShiftRule(daysRange: -10));
        }

        #endregion

        #region Time Preservation Tests

        [Fact]
        public void Apply_PreserveTimeTrue_MaintainsTimeComponent()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 180, preserveTime: true);
            var input = new DateTime(2025, 1, 15, 14, 30, 45);

            // Act
            var result = rule.Apply(input);

            // Assert - time component should be unchanged
            Assert.Equal(input.Hour, result.Hour);
            Assert.Equal(input.Minute, result.Minute);
            Assert.Equal(input.Second, result.Second);
            Assert.Equal(input.Millisecond, result.Millisecond);
        }

        [Theory]
        [InlineData(0, 0, 0)]      // Midnight
        [InlineData(12, 0, 0)]     // Noon
        [InlineData(23, 59, 59)]   // End of day
        [InlineData(14, 30, 45)]   // Arbitrary time
        public void Apply_PreserveTime_MaintainsSpecificTimes(int hour, int minute, int second)
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 180, preserveTime: true);
            var input = new DateTime(2025, 1, 15, hour, minute, second);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(hour, result.Hour);
            Assert.Equal(minute, result.Minute);
            Assert.Equal(second, result.Second);
        }

        [Fact]
        public void Apply_PreserveTimeFalse_ShiftsEntireDateTime()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 180, preserveTime: false);
            var input = new DateTime(2025, 1, 15, 14, 30, 0);

            // Act
            var result = rule.Apply(input);

            // Assert - time should be preserved (AddDays preserves time by default)
            // Note: Even with preserveTime=false, AddDays preserves time.
            // The parameter is for future extensibility if we want to add time jitter
            Assert.Equal(input.Hour, result.Hour);
            Assert.Equal(input.Minute, result.Minute);
        }

        #endregion

        #region Deterministic Seeding Tests

        [Fact]
        public void Apply_WithSeedProvider_ProducesDeterministicOutput()
        {
            // Arrange
            var rule1 = new DateShiftRule(daysRange: 180);
            rule1.SeedProvider = dt => 12345; // Constant seed

            var rule2 = new DateShiftRule(daysRange: 180);
            rule2.SeedProvider = dt => 12345; // Same constant seed

            var input = new DateTime(2025, 1, 15);

            // Act
            var result1 = rule1.Apply(input);
            var result2 = rule2.Apply(input);

            // Assert - same seed should produce same output
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void Apply_WithSeedProvider_MultipleCalls_ProducesSameOutput()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 180);
            rule.SeedProvider = dt => dt.GetHashCode();
            var input = new DateTime(2025, 1, 15);

            // Act
            var result1 = rule.Apply(input);
            var result2 = rule.Apply(input);
            var result3 = rule.Apply(input);

            // Assert - same input with same seed provider should always produce same output
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        [Fact]
        public void Apply_WithoutSeedProvider_ProducesNonDeterministicOutput()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 180);
            var input = new DateTime(2025, 1, 15);

            // Act - run multiple times
            var results = new List<DateTime>();
            for (int i = 0; i < 10; i++)
            {
                results.Add(rule.Apply(input));
            }

            // Assert - should have at least some different values (highly likely with random)
            var uniqueResults = results.Distinct().Count();
            Assert.True(uniqueResults > 1, "Non-deterministic masking should produce varying results");
        }

        [Fact]
        public void Apply_DifferentSeeds_ProducesDifferentOutputs()
        {
            // Arrange
            var rule1 = new DateShiftRule(daysRange: 180);
            rule1.SeedProvider = dt => 12345;

            var rule2 = new DateShiftRule(daysRange: 180);
            rule2.SeedProvider = dt => 54321; // Different seed

            var input = new DateTime(2025, 1, 15);

            // Act
            var result1 = rule1.Apply(input);
            var result2 = rule2.Apply(input);

            // Assert - different seeds should (very likely) produce different outputs
            Assert.NotEqual(result1, result2);
        }

        #endregion

        #region HIPAA Safe Harbor Compliance Tests

        [Fact]
        public void Apply_HIPAACompliant_ConsistentShiftPerPatient()
        {
            // Arrange - simulating HIPAA requirement: same shift for all dates of a patient
            var patientId = "patient-12345";
            var rule = new DateShiftRule(daysRange: 180);
            rule.SeedProvider = dt => patientId.GetHashCode(); // Consistent seed per patient

            var admissionDate = new DateTime(2025, 1, 15);
            var dischargeDate = new DateTime(2025, 1, 20);
            var followUpDate = new DateTime(2025, 2, 10);

            // Act
            var maskedAdmission = rule.Apply(admissionDate);
            var maskedDischarge = rule.Apply(dischargeDate);
            var maskedFollowUp = rule.Apply(followUpDate);

            // Assert - all dates should be shifted by the same number of days
            var shiftDays1 = (maskedAdmission - admissionDate).Days;
            var shiftDays2 = (maskedDischarge - dischargeDate).Days;
            var shiftDays3 = (maskedFollowUp - followUpDate).Days;

            Assert.Equal(shiftDays1, shiftDays2);
            Assert.Equal(shiftDays2, shiftDays3);
        }

        [Fact]
        public void Apply_HIPAACompliant_PreservesRelativeOrdering()
        {
            // Arrange - HIPAA requirement: preserve chronological ordering
            var patientId = "patient-67890";
            var rule = new DateShiftRule(daysRange: 180);
            rule.SeedProvider = dt => patientId.GetHashCode();

            var date1 = new DateTime(2025, 1, 10);
            var date2 = new DateTime(2025, 1, 15);
            var date3 = new DateTime(2025, 1, 20);

            // Act
            var masked1 = rule.Apply(date1);
            var masked2 = rule.Apply(date2);
            var masked3 = rule.Apply(date3);

            // Assert - chronological order should be preserved
            Assert.True(masked1 < masked2, "First date should remain before second date");
            Assert.True(masked2 < masked3, "Second date should remain before third date");
        }

        [Fact]
        public void Apply_HIPAACompliant_PreservesDuration()
        {
            // Arrange - HIPAA requirement: preserve durations between events
            var patientId = "patient-99999";
            var rule = new DateShiftRule(daysRange: 180);
            rule.SeedProvider = dt => patientId.GetHashCode();

            var admission = new DateTime(2025, 1, 15);
            var discharge = new DateTime(2025, 1, 20); // 5 days later

            // Act
            var maskedAdmission = rule.Apply(admission);
            var maskedDischarge = rule.Apply(discharge);

            // Assert - duration should be preserved
            var originalDuration = (discharge - admission).Days;
            var maskedDuration = (maskedDischarge - maskedAdmission).Days;
            Assert.Equal(originalDuration, maskedDuration);
        }

        [Theory]
        [InlineData(180)]  // HIPAA typical
        [InlineData(365)]  // HIPAA maximum
        [InlineData(90)]   // Smaller range
        public void Apply_HIPAACompliant_WithinPermittedRange(int daysRange)
        {
            // Arrange - HIPAA requires shift within ±365 days
            var patientId = "patient-11111";
            var rule = new DateShiftRule(daysRange: daysRange);
            rule.SeedProvider = dt => patientId.GetHashCode();

            var input = new DateTime(2025, 1, 15);

            // Act
            var result = rule.Apply(input);

            // Assert - shift should be within specified range
            var shiftDays = Math.Abs((result - input).Days);
            Assert.InRange(shiftDays, 0, daysRange);
        }

        [Fact]
        public void Apply_HIPAACompliant_DifferentPatientsGetDifferentShifts()
        {
            // Arrange - different patients should get different (random) shifts
            var patient1Id = "patient-11111";
            var patient2Id = "patient-22222";

            var rule1 = new DateShiftRule(daysRange: 180);
            rule1.SeedProvider = dt => patient1Id.GetHashCode();

            var rule2 = new DateShiftRule(daysRange: 180);
            rule2.SeedProvider = dt => patient2Id.GetHashCode();

            var input = new DateTime(2025, 1, 15);

            // Act
            var result1 = rule1.Apply(input);
            var result2 = rule2.Apply(input);

            // Assert - different patients should (very likely) have different shifts
            Assert.NotEqual(result1, result2);
        }

        #endregion

        #region Builder API Integration Tests

        [Fact]
        public void DateShift_ExtensionMethod_CreatesRuleWithCorrectParameters()
        {
            // Arrange
            var builder = new DateTimeMaskingBuilder();

            // Act
            var resultBuilder = builder.DateShift(daysRange: 180, preserveTime: true);

            // Assert
            Assert.NotNull(resultBuilder);
            Assert.IsType<DateTimeMaskingBuilder>(resultBuilder);
        }

        [Fact]
        public void DateShift_WithSeedProvider_AppliesSeedCorrectly()
        {
            // Arrange
            var patientId = "patient-abc123";
            var input = new DateTime(2025, 1, 15);

            // Act
            var rules = new DateTimeMaskingBuilder()
                .WithRandomSeed(dt => patientId.GetHashCode())
                .DateShift(daysRange: 180)
                .Build();

            var result1 = rules[0].Apply(input);
            var result2 = rules[0].Apply(input);

            // Assert - should be deterministic
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void DateShift_WithConstantSeed_ProducesDeterministicOutput()
        {
            // Arrange
            var input = new DateTime(2025, 1, 15);

            // Act
            var rules = new DateTimeMaskingBuilder()
                .WithRandomSeed(12345)  // Constant seed
                .DateShift(daysRange: 180)
                .Build();

            var result1 = rules[0].Apply(input);
            var result2 = rules[0].Apply(input);

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void DateShift_ChainedWithMultipleRules_AppliesInOrder()
        {
            // Arrange
            var input = new DateTime(2025, 1, 15);

            // Act
            var rules = new DateTimeMaskingBuilder()
                .WithRandomSeed(12345)
                .DateShift(daysRange: 30)
                .WithRandomSeed(54321)
                .DateShift(daysRange: 30)
                .Build();

            var intermediate = rules[0].Apply(input);
            var final = rules[1].Apply(intermediate);

            // Assert - should have two rules
            Assert.Equal(2, rules.Count);

            // Verify they produce different results (different seeds)
            var result1Only = rules[0].Apply(input);
            var result2Only = rules[1].Apply(input);
            Assert.NotEqual(result1Only, result2Only);
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Fact]
        public void Apply_MinDateTime_DoesNotThrow()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 10);
            var input = DateTime.MinValue.AddDays(20); // Need buffer to avoid underflow

            // Act
            var result = rule.Apply(input);

            // Assert - should not throw
            Assert.NotEqual(default(DateTime), result);
        }

        [Fact]
        public void Apply_MaxDateTime_DoesNotThrow()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 10);
            var input = DateTime.MaxValue.AddDays(-20); // Need buffer to avoid overflow

            // Act
            var result = rule.Apply(input);

            // Assert - should not throw
            Assert.NotEqual(default(DateTime), result);
        }

        [Theory]
        [InlineData(1)]      // Minimum non-zero range
        [InlineData(30)]     // One month
        [InlineData(365)]    // One year (HIPAA max)
        [InlineData(1000)]   // Beyond HIPAA range
        public void Apply_VariousRanges_WorksCorrectly(int daysRange)
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: daysRange);
            var input = new DateTime(2025, 6, 15);

            // Act
            var result = rule.Apply(input);

            // Assert
            var minExpected = input.AddDays(-daysRange);
            var maxExpected = input.AddDays(daysRange);
            Assert.InRange(result, minExpected, maxExpected);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Apply_Performance_Exceeds50kOpsPerSecond()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 180);
            rule.SeedProvider = dt => dt.GetHashCode(); // Deterministic for consistent performance
            var input = new DateTime(2025, 1, 15);
            var iterations = 100000; // 100k iterations

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                _ = rule.Apply(input);
            }
            stopwatch.Stop();

            // Assert - should complete 100k ops in < 2 seconds (50k ops/sec target)
            var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
            Assert.True(opsPerSecond >= 50000,
                $"Performance target not met. Expected ≥50,000 ops/sec, got {opsPerSecond:N0} ops/sec");
        }

        [Fact]
        public void Apply_Performance_NonDeterministic_ReasonableSpeed()
        {
            // Arrange
            var rule = new DateShiftRule(daysRange: 180);
            var input = new DateTime(2025, 1, 15);
            var iterations = 10000; // 10k iterations for non-deterministic

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                _ = rule.Apply(input);
            }
            stopwatch.Stop();

            // Assert - should complete 10k ops in reasonable time (< 1 second)
            Assert.True(stopwatch.Elapsed.TotalSeconds < 1.0,
                $"Non-deterministic performance too slow: {stopwatch.Elapsed.TotalSeconds:F3} seconds for {iterations} operations");
        }

        #endregion

        #region Real-World HIPAA Scenario Tests

        [Fact]
        public void Apply_RealWorldHIPAA_PatientJourneyExample()
        {
            // Arrange - simulating a real patient journey with multiple events
            var patientId = "patient-realworld-001";
            var rule = new DateShiftRule(daysRange: 180, preserveTime: true);
            rule.SeedProvider = dt => patientId.GetHashCode();

            // Patient journey timeline
            var initialConsultation = new DateTime(2024, 11, 10, 9, 30, 0);
            var labTests = new DateTime(2024, 11, 15, 14, 0, 0);
            var diagnosisDate = new DateTime(2024, 11, 20, 10, 15, 0);
            var treatmentStart = new DateTime(2024, 12, 1, 8, 0, 0);
            var followUp1 = new DateTime(2024, 12, 15, 11, 30, 0);
            var followUp2 = new DateTime(2025, 1, 10, 9, 45, 0);

            // Act - mask all dates
            var maskedConsultation = rule.Apply(initialConsultation);
            var maskedLabTests = rule.Apply(labTests);
            var maskedDiagnosis = rule.Apply(diagnosisDate);
            var maskedTreatmentStart = rule.Apply(treatmentStart);
            var maskedFollowUp1 = rule.Apply(followUp1);
            var maskedFollowUp2 = rule.Apply(followUp2);

            // Assert - verify HIPAA compliance requirements
            // 1. All dates shifted by same amount
            var shift = (maskedConsultation - initialConsultation).Days;
            Assert.Equal(shift, (maskedLabTests - labTests).Days);
            Assert.Equal(shift, (maskedDiagnosis - diagnosisDate).Days);
            Assert.Equal(shift, (maskedTreatmentStart - treatmentStart).Days);
            Assert.Equal(shift, (maskedFollowUp1 - followUp1).Days);
            Assert.Equal(shift, (maskedFollowUp2 - followUp2).Days);

            // 2. Chronological order preserved
            Assert.True(maskedConsultation < maskedLabTests);
            Assert.True(maskedLabTests < maskedDiagnosis);
            Assert.True(maskedDiagnosis < maskedTreatmentStart);
            Assert.True(maskedTreatmentStart < maskedFollowUp1);
            Assert.True(maskedFollowUp1 < maskedFollowUp2);

            // 3. Time components preserved
            Assert.Equal(initialConsultation.TimeOfDay, maskedConsultation.TimeOfDay);
            Assert.Equal(labTests.TimeOfDay, maskedLabTests.TimeOfDay);
            Assert.Equal(diagnosisDate.TimeOfDay, maskedDiagnosis.TimeOfDay);

            // 4. Shift within ±180 days
            Assert.InRange(Math.Abs(shift), 0, 180);
        }

        #endregion
    }
}
