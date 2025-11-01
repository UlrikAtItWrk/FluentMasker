using ITW.FluentMasker.MaskRules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for DateAgeMaskRule - tests GDPR and HIPAA-compliant date and age masking
    /// </summary>
    public class DateAgeMaskRuleTests
    {
        #region Year-Only Mode Tests

        [Theory]
        [InlineData("1982-11-23", "1982-**-**")]
        [InlineData("2023-09-18", "2023-**-**")]
        [InlineData("1995-01-01", "1995-**-**")]
        [InlineData("2000-12-31", "2000-**-**")]
        public void Apply_YearOnlyMode_MasksMonthAndDay(string input, string expected)
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.YearOnly);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("2023-09-18T14:23:00Z", "2023-**-**")]
        [InlineData("2023-09-18T14:23:00.000Z", "2023-**-**")]
        [InlineData("2025-01-15T09:30:00", "2025-**-**")]
        public void Apply_YearOnlyMode_HandlesTimestamps(string input, string expected)
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.YearOnly);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("11/23/1982", "1982-**-**")]
        [InlineData("23.11.1982", "1982-**-**")]
        [InlineData("23-11-1982", "1982-**-**")]
        public void Apply_YearOnlyMode_HandlesVariousFormats(string input, string expected)
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.YearOnly);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void Apply_YearOnlyMode_NullOrEmpty_ReturnsUnchanged(string input)
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.YearOnly);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_YearOnlyMode_InvalidDate_ReturnsUnchanged()
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.YearOnly);
            var input = "INVALID-DATE";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_YearOnlyMode_CustomSeparator_UsesCorrectFormat()
        {
            // Arrange
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.YearOnly,
                separator: "/",
                maskChar: "X");

            // Act
            var result = rule.Apply("1982-11-23");

            // Assert
            Assert.Equal("1982/XX/XX", result);
        }

        #endregion

        #region Date Shift Mode Tests

        [Fact]
        public void Apply_DateShiftMode_WithinRange()
        {
            // Arrange
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180);
            var input = "2025-01-15";

            // Act
            var result = rule.Apply(input);

            // Assert - should parse back to a valid date within range
            Assert.True(DateTime.TryParse(result, out DateTime resultDate));
            var originalDate = DateTime.Parse(input);
            var daysDiff = Math.Abs((resultDate - originalDate).Days);
            Assert.InRange(daysDiff, 0, 180);
        }

        [Fact]
        public void Apply_DateShiftMode_WithSeedProvider_Deterministic()
        {
            // Arrange
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180);
            rule.SeedProvider = value => value.GetHashCode();
            var input = "2025-01-15";

            // Act
            var result1 = rule.Apply(input);
            var result2 = rule.Apply(input);
            var result3 = rule.Apply(input);

            // Assert - should be deterministic
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        [Fact]
        public void Apply_DateShiftMode_WithoutSeedProvider_NonDeterministic()
        {
            // Arrange
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180);
            var input = "2025-01-15";

            // Act - run multiple times
            var results = new HashSet<string>();
            for (int i = 0; i < 10; i++)
            {
                results.Add(rule.Apply(input));
            }

            // Assert - should have at least some different values
            Assert.True(results.Count > 1, "Non-deterministic masking should produce varying results");
        }

        [Fact]
        public void Apply_DateShiftMode_HIPAACompliant_ConsistentShiftPerPatient()
        {
            // Arrange - simulating HIPAA requirement
            var patientId = "patient-12345";
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180);
            rule.SeedProvider = value => patientId.GetHashCode();

            var date1 = "2025-01-15";
            var date2 = "2025-01-20";
            var date3 = "2025-02-10";

            // Act
            var masked1 = DateTime.Parse(rule.Apply(date1));
            var masked2 = DateTime.Parse(rule.Apply(date2));
            var masked3 = DateTime.Parse(rule.Apply(date3));

            // Assert - all dates shifted by same amount
            var shift1 = (masked1 - DateTime.Parse(date1)).Days;
            var shift2 = (masked2 - DateTime.Parse(date2)).Days;
            var shift3 = (masked3 - DateTime.Parse(date3)).Days;

            Assert.Equal(shift1, shift2);
            Assert.Equal(shift2, shift3);
        }

        [Fact]
        public void Apply_DateShiftMode_HIPAACompliant_PreservesRelativeOrdering()
        {
            // Arrange
            var patientId = "patient-67890";
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180);
            rule.SeedProvider = value => patientId.GetHashCode();

            var date1 = "2025-01-10";
            var date2 = "2025-01-15";
            var date3 = "2025-01-20";

            // Act
            var masked1 = DateTime.Parse(rule.Apply(date1));
            var masked2 = DateTime.Parse(rule.Apply(date2));
            var masked3 = DateTime.Parse(rule.Apply(date3));

            // Assert - chronological order preserved
            Assert.True(masked1 < masked2);
            Assert.True(masked2 < masked3);
        }

        [Fact]
        public void Apply_DateShiftMode_HIPAACompliant_PreservesDuration()
        {
            // Arrange
            var patientId = "patient-99999";
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180);
            rule.SeedProvider = value => patientId.GetHashCode();

            var admission = "2025-01-15";
            var discharge = "2025-01-20"; // 5 days later

            // Act
            var maskedAdmission = DateTime.Parse(rule.Apply(admission));
            var maskedDischarge = DateTime.Parse(rule.Apply(discharge));

            // Assert - duration preserved
            var originalDuration = (DateTime.Parse(discharge) - DateTime.Parse(admission)).Days;
            var maskedDuration = (maskedDischarge - maskedAdmission).Days;
            Assert.Equal(originalDuration, maskedDuration);
        }

        [Theory]
        [InlineData(180)]  // HIPAA typical
        [InlineData(365)]  // HIPAA maximum
        [InlineData(90)]   // Smaller range
        public void Apply_DateShiftMode_VariousRanges_WorksCorrectly(int daysRange)
        {
            // Arrange
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: daysRange);
            rule.SeedProvider = value => 12345; // Deterministic
            var input = "2025-01-15";

            // Act
            var result = rule.Apply(input);

            // Assert
            var resultDate = DateTime.Parse(result);
            var originalDate = DateTime.Parse(input);
            var daysDiff = Math.Abs((resultDate - originalDate).Days);
            Assert.InRange(daysDiff, 0, daysRange);
        }

        [Fact]
        public void Apply_DateShiftMode_ZeroRange_ReturnsUnchangedDate()
        {
            // Arrange
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 0);
            var input = "2025-01-15";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        #endregion

        #region Redact Mode Tests

        [Theory]
        [InlineData("1982-11-23")]
        [InlineData("2023-09-18T14:23:00Z")]
        [InlineData("11/23/1982")]
        public void Apply_RedactMode_ReturnsRedacted(string input)
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.Redact);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("[REDACTED]", result);
        }

        [Fact]
        public void Apply_RedactMode_InvalidDate_ReturnsUnchanged()
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.Redact);
            var input = "INVALID-DATE";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        #endregion

        #region Age Masking Tests

        [Fact]
        public void ApplyAge_Age90OrAbove_Returns90Plus()
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: false);

            // Act & Assert
            Assert.Equal("90+", rule.ApplyAge(90));
            Assert.Equal("90+", rule.ApplyAge(94));
            Assert.Equal("90+", rule.ApplyAge(100));
            Assert.Equal("90+", rule.ApplyAge(120));
        }

        [Theory]
        [InlineData(25, "25")]
        [InlineData(42, "42")]
        [InlineData(89, "89")]
        [InlineData(0, "0")]
        public void ApplyAge_NoBucketing_BelowAge90_ReturnsAge(int age, string expected)
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: false);

            // Act
            var result = rule.ApplyAge(age);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(3, "0-5")]
        [InlineData(7, "6-10")]
        [InlineData(15, "11-20")]
        [InlineData(27, "21-30")]
        [InlineData(42, "41-50")]
        [InlineData(68, "61-70")]
        [InlineData(85, "81-89")]
        [InlineData(94, "90+")]
        public void ApplyAge_WithBucketing_ReturnsCorrectBucket(int age, string expected)
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: true);

            // Act
            var result = rule.ApplyAge(age);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0, "0-5")]
        [InlineData(5, "0-5")]
        [InlineData(6, "6-10")]
        [InlineData(10, "6-10")]
        [InlineData(11, "11-20")]
        [InlineData(20, "11-20")]
        [InlineData(89, "81-89")]
        [InlineData(90, "90+")]
        public void ApplyAge_WithBucketing_BoundaryValues_Correct(int age, string expected)
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: true);

            // Act
            var result = rule.ApplyAge(age);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ApplyAge_CustomBuckets_WorksCorrectly()
        {
            // Arrange - custom buckets for pediatrics
            var rule = new DateAgeMaskRule(
                ageBucketing: true,
                customAgeBreaks: new[] { 0, 1, 3, 6, 13, 18, 150 },
                customAgeLabels: new[] { "Infant", "Toddler", "Preschool", "School-age", "Teen", "Adult" });

            // Act & Assert
            Assert.Equal("Infant", rule.ApplyAge(0));
            Assert.Equal("Toddler", rule.ApplyAge(2));
            Assert.Equal("Preschool", rule.ApplyAge(4));
            Assert.Equal("School-age", rule.ApplyAge(10));
            Assert.Equal("Teen", rule.ApplyAge(15));
            Assert.Equal("Adult", rule.ApplyAge(25));
            Assert.Equal("Adult", rule.ApplyAge(94)); // Even ages over 90 use custom buckets
        }

        [Fact]
        public void Constructor_CustomBuckets_InvalidLength_ThrowsException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new DateAgeMaskRule(
                    ageBucketing: true,
                    customAgeBreaks: new[] { 0, 18, 65, 150 },
                    customAgeLabels: new[] { "Young", "Adult" })); // Wrong length
        }

        #endregion

        #region CalculateAndMaskAge Tests

        [Fact]
        public void CalculateAndMaskAge_CalculatesAgeCorrectly()
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: false);
            var dob = new DateTime(1982, 11, 23);
            var referenceDate = new DateTime(2025, 1, 15);

            // Act
            var result = rule.CalculateAndMaskAge(dob, referenceDate);

            // Assert - should be 42 years old
            Assert.Equal("42", result);
        }

        [Fact]
        public void CalculateAndMaskAge_BeforeBirthday_CalculatesCorrectly()
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: false);
            var dob = new DateTime(1982, 11, 23);
            var referenceDate = new DateTime(2025, 11, 20); // 3 days before birthday

            // Act
            var result = rule.CalculateAndMaskAge(dob, referenceDate);

            // Assert - should still be 42, not 43 yet
            Assert.Equal("42", result);
        }

        [Fact]
        public void CalculateAndMaskAge_OnBirthday_CalculatesCorrectly()
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: false);
            var dob = new DateTime(1982, 11, 23);
            var referenceDate = new DateTime(2025, 11, 23); // On birthday

            // Act
            var result = rule.CalculateAndMaskAge(dob, referenceDate);

            // Assert - should be 43 on birthday
            Assert.Equal("43", result);
        }

        [Fact]
        public void CalculateAndMaskAge_WithBucketing_ReturnsCorrectBucket()
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: true);
            var dob = new DateTime(1982, 11, 23);
            var referenceDate = new DateTime(2025, 1, 15); // Age 42

            // Act
            var result = rule.CalculateAndMaskAge(dob, referenceDate);

            // Assert
            Assert.Equal("41-50", result);
        }

        [Fact]
        public void CalculateAndMaskAge_Over90_Returns90Plus()
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: true);
            var dob = new DateTime(1930, 1, 1);
            var referenceDate = new DateTime(2025, 1, 15); // Age 95

            // Act
            var result = rule.CalculateAndMaskAge(dob, referenceDate);

            // Assert
            Assert.Equal("90+", result);
        }

        [Fact]
        public void CalculateAndMaskAge_DefaultReferenceDate_UsesToday()
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: false);
            var dob = DateTime.Today.AddYears(-25);

            // Act
            var result = rule.CalculateAndMaskAge(dob);

            // Assert - should be 25 or 24 depending on if birthday has passed
            Assert.True(result == "25" || result == "24");
        }

        #endregion

        #region Edge Cases and Validation Tests

        [Fact]
        public void Constructor_NegativeDaysRange_ThrowsException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new DateAgeMaskRule(daysRange: -10));
        }

        [Fact]
        public void Apply_LeapYearDate_HandlesCorrectly()
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.YearOnly);

            // Act
            var result = rule.Apply("2024-02-29"); // Leap year date

            // Assert
            Assert.Equal("2024-**-**", result);
        }

        [Theory]
        [InlineData("1900-01-01")]  // Very old date
        [InlineData("2099-12-31")]  // Future date
        public void Apply_ExtremeDates_HandlesCorrectly(string input)
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.YearOnly);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.StartsWith(input.Substring(0, 4), result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(200)]
        [InlineData(999)]
        public void ApplyAge_UnusualAges_HandlesGracefully(int age)
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: true);

            // Act
            var result = rule.ApplyAge(age);

            // Assert - should not throw and return something
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        #endregion

        #region Real-World HIPAA Scenario Tests

        [Fact]
        public void RealWorldScenario_PatientDataset_HIPAACompliant()
        {
            // Arrange - simulating a real healthcare dataset
            var patientId = "patient-realworld-001";
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180,
                ageBucketing: true);
            rule.SeedProvider = value => patientId.GetHashCode();

            // Patient data
            var dob = "1945-06-15";  // DOB (will be Age 79)
            var initialConsult = "2024-11-10";
            var diagnosis = "2024-11-20";
            var treatmentStart = "2024-12-01";

            // Act - mask all dates
            var maskedDob = rule.Apply(dob);
            var maskedConsult = rule.Apply(initialConsult);
            var maskedDiagnosis = rule.Apply(diagnosis);
            var maskedTreatment = rule.Apply(treatmentStart);

            // Calculate and mask age
            var age = rule.CalculateAndMaskAge(
                DateTime.Parse(dob),
                DateTime.Parse(initialConsult));

            // Assert HIPAA compliance
            // 1. All dates shifted by same amount
            var shift1 = (DateTime.Parse(maskedConsult) - DateTime.Parse(initialConsult)).Days;
            var shift2 = (DateTime.Parse(maskedDiagnosis) - DateTime.Parse(diagnosis)).Days;
            var shift3 = (DateTime.Parse(maskedTreatment) - DateTime.Parse(treatmentStart)).Days;
            Assert.Equal(shift1, shift2);
            Assert.Equal(shift2, shift3);

            // 2. Chronological order preserved
            Assert.True(DateTime.Parse(maskedConsult) < DateTime.Parse(maskedDiagnosis));
            Assert.True(DateTime.Parse(maskedDiagnosis) < DateTime.Parse(maskedTreatment));

            // 3. Age is bucketed
            Assert.Equal("71-80", age);

            // 4. Shift within permitted range
            Assert.InRange(Math.Abs(shift1), 0, 180);
        }

        [Fact]
        public void RealWorldScenario_ElderlyPatient_Age90Plus()
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: true);
            var dob = new DateTime(1930, 3, 15); // Would be 94 years old in 2024

            // Act
            var maskedAge = rule.CalculateAndMaskAge(dob, new DateTime(2024, 11, 10));

            // Assert - HIPAA requires ages ?90 to be "90+"
            Assert.Equal("90+", maskedAge);
        }

        [Fact]
        public void RealWorldScenario_MultiplePatients_DifferentShifts()
        {
            // Arrange - different patients should get different shifts
            var patient1 = "patient-001";
            var patient2 = "patient-002";

            var rule1 = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180);
            rule1.SeedProvider = value => patient1.GetHashCode();

            var rule2 = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180);
            rule2.SeedProvider = value => patient2.GetHashCode();

            var date = "2024-11-10";

            // Act
            var masked1 = rule1.Apply(date);
            var masked2 = rule2.Apply(date);

            // Assert - different patients should have different shifts
            Assert.NotEqual(masked1, masked2);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Apply_Performance_YearOnly_Fast()
        {
            // Arrange
            var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.YearOnly);
            var input = "1982-11-23";
            var iterations = 100000;

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                _ = rule.Apply(input);
            }
            stopwatch.Stop();

            // Assert - should be very fast
            var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
            Assert.True(opsPerSecond >= 50000,
                $"Performance target not met. Expected ?50,000 ops/sec, got {opsPerSecond:N0} ops/sec");
        }

        [Fact]
        public void ApplyAge_Performance_Bucketing_Fast()
        {
            // Arrange
            var rule = new DateAgeMaskRule(ageBucketing: true);
            var iterations = 100000;

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                _ = rule.ApplyAge(i % 100); // Test various ages
            }
            stopwatch.Stop();

            // Assert
            var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
            Assert.True(opsPerSecond >= 100000,
                $"Performance target not met. Expected ?100,000 ops/sec, got {opsPerSecond:N0} ops/sec");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Integration_YearOnlyWithAgeBucketing_WorksTogether()
        {
            // Arrange
            var dateRule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.YearOnly,
                ageBucketing: true);
            var dob = "1982-11-23";

            // Act
            var maskedDate = dateRule.Apply(dob);
            var maskedAge = dateRule.CalculateAndMaskAge(
                DateTime.Parse(dob),
                new DateTime(2025, 1, 15));

            // Assert
            Assert.Equal("1982-**-**", maskedDate);
            Assert.Equal("41-50", maskedAge);
        }

        [Fact]
        public void Integration_DateShiftWithAgeBucketing_WorksTogether()
        {
            // Arrange
            var patientId = "patient-integration-001";
            var rule = new DateAgeMaskRule(
                mode: DateAgeMaskRule.MaskingMode.DateShift,
                daysRange: 180,
                ageBucketing: true);
            rule.SeedProvider = value => patientId.GetHashCode();

            var dob = "1982-11-23";

            // Act
            var maskedDate = rule.Apply(dob);
            var maskedAge = rule.CalculateAndMaskAge(
                DateTime.Parse(dob),
                new DateTime(2025, 1, 15));

            // Assert
            Assert.NotEqual("1982-**-**", maskedDate); // Should be shifted
            Assert.True(DateTime.TryParse(maskedDate, out _)); // Should be valid date
            Assert.Equal("41-50", maskedAge);
        }

        #endregion
    }
}
