using ITW.FluentMasker.MaskRules;
using System;
using System.Diagnostics;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for IBANMaskRule - tests IBAN masking with format preservation and checksum validation
    /// </summary>
    public class IBANMaskRuleTests
    {
        #region Basic Functionality Tests

        [Fact]
        public void Apply_ValidGermanIBAN_MasksCorrectly()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);
            var input = "DE89370400440532013000";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("DE89**************3000", result);
        }

        [Fact]
        public void Apply_ValidIBANWithSpaces_PreservesGrouping()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4, preserveGrouping: true);
            var input = "DE89 3704 0044 0532 0130 00";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("DE89 **** **** **** **30 00", result);
        }

        [Fact]
        public void Apply_ValidIBANWithSpaces_NoGroupingPreservation_RemovesSpaces()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4, preserveGrouping: false);
            var input = "DE89 3704 0044 0532 0130 00";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("DE89**************3000", result);
            Assert.DoesNotContain(" ", result);
        }

        [Theory]
        [InlineData(0, "DE89******************")]  // Keep no last digits
        [InlineData(2, "DE89****************00")]  // Keep last 2
        [InlineData(4, "DE89**************3000")] // Keep last 4 (default)
        [InlineData(6, "DE89************013000")] // Keep last 6
        public void Apply_VariousKeepLastValues_MasksCorrectly(int keepLast, string expected)
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: keepLast);
            var input = "DE89370400440532013000";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_CustomMaskChar_UsesSpecifiedCharacter()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4, maskChar: '#');
            var input = "DE89370400440532013000";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("DE89##############3000", result);
        }

        [Fact]
        public void Apply_LowercaseIBAN_ConvertsToUppercase()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);
            var input = "de89370400440532013000";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("DE89**************3000", result);
            Assert.Equal(result.ToUpperInvariant(), result);
        }

        #endregion

        #region Multiple Country Format Tests

        [Theory]
        [InlineData("GB82WEST12345698765432", "GB82**************5432", "United Kingdom")]
        [InlineData("FR1420041010050500013M02606", "FR14*******************2606", "France")]
        [InlineData("ES9121000418450200051332", "ES91****************1332", "Spain")]
        [InlineData("IT60X0542811101000000123456", "IT60*******************3456", "Italy")]
        [InlineData("NL91ABNA0417164300", "NL91**********4300", "Netherlands")]
        [InlineData("CH9300762011623852957", "CH93*************2957", "Switzerland")]
        [InlineData("AT611904300234573201", "AT61************3201", "Austria")]
        [InlineData("BE68539007547034", "BE68********7034", "Belgium")]
        [InlineData("DK5000400440116243", "DK50**********6243", "Denmark")]
        [InlineData("FI2112345600000785", "FI21**********0785", "Finland")]
        [InlineData("SE4550000000058398257466", "SE45****************7466", "Sweden")]
        [InlineData("NO9386011117947", "NO93*******7947", "Norway")]
        [InlineData("PL61109010140000071219812874", "PL61********************2874", "Poland")]
        [InlineData("IE29AIBK93115212345678", "IE29**************5678", "Ireland")]
        [InlineData("GR1601101250000000012300695", "GR16*******************0695", "Greece")]
        public void Apply_VariousCountryFormats_MasksCorrectly(string input, string expected, string countryName)
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);

            // Act
            var result = rule.Apply(input);

            // Assert - verify masking worked correctly
            Assert.Equal(expected, result);

            // Assert - verify country code and check digits preserved
            Assert.StartsWith(input.Substring(0, 4), result);
        }

        [Fact]
        public void Apply_ShortestValidIBAN_Norway_MasksCorrectly()
        {
            // Arrange - Norway has shortest IBAN at 15 characters
            var rule = new IBANMaskRule(keepLast: 4);
            var input = "NO9386011117947";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("NO93*******7947", result);
            Assert.Equal(15, result.Length);
        }

        [Fact]
        public void Apply_LongestValidIBAN_Malta_MasksCorrectly()
        {
            // Arrange - Malta has one of the longest IBANs at 31 characters
            var rule = new IBANMaskRule(keepLast: 4);
            var input = "MT84MALT011000012345MTLCAST001S";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("MT84***********************001S", result);
            Assert.Equal(31, result.Length);
        }

        #endregion

        #region Checksum Validation Tests

        [Theory]
        [InlineData("DE89370400440532013000")] // Valid German IBAN
        [InlineData("GB82WEST12345698765432")] // Valid UK IBAN
        [InlineData("FR1420041010050500013M02606")] // Valid French IBAN
        [InlineData("ES9121000418450200051332")] // Valid Spanish IBAN
        [InlineData("IT60X0542811101000000123456")] // Valid Italian IBAN
        [InlineData("NL91ABNA0417164300")] // Valid Dutch IBAN
        public void Apply_ValidChecksum_AcceptsIBAN(string validIban)
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);

            // Act
            var result = rule.Apply(validIban);

            // Assert - valid IBAN should be masked, not returned unchanged
            Assert.NotEqual(validIban, result);
            Assert.Contains("*", result);
        }

        [Theory]
        [InlineData("DE88370400440532013000")] // Invalid checksum (should be 89, not 88)
        [InlineData("GB83WEST12345698765432")] // Invalid checksum (should be 82, not 83)
        [InlineData("FR1320041010050500013M02606")] // Invalid checksum (should be 14, not 13)
        public void Apply_InvalidChecksum_ReturnsUnchanged(string invalidIban)
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);

            // Act
            var result = rule.Apply(invalidIban);

            // Assert - invalid IBAN should be returned unchanged
            Assert.Equal(invalidIban, result);
            Assert.DoesNotContain("*", result);
        }

        #endregion

        #region Length Validation Tests

        [Theory]
        [InlineData("DE8937040044053201300")] // Too short (21 chars, should be 22)
        [InlineData("DE893704004405320130001")] // Too long (23 chars, should be 22)
        [InlineData("GB82WEST1234569876543")] // Too short (21 chars, should be 22)
        [InlineData("GB82WEST123456987654321")] // Too long (23 chars, should be 22)
        public void Apply_WrongLength_ReturnsUnchanged(string wrongLengthIban)
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);

            // Act
            var result = rule.Apply(wrongLengthIban);

            // Assert - wrong length IBAN should be returned unchanged
            Assert.Equal(wrongLengthIban, result);
        }

        [Fact]
        public void Apply_TooShortToMask_ReturnsUnchanged()
        {
            // Arrange - IBAN where keepLast would overlap with country code + check digits
            var rule = new IBANMaskRule(keepLast: 20);
            var input = "DE89370400440532013000"; // 22 chars total

            // Act
            var result = rule.Apply(input);

            // Assert - when keepLast is too large, return unchanged
            Assert.Equal(input, result);
        }

        #endregion

        #region Invalid Format Tests

        [Theory]
        [InlineData("1234370400440532013000")] // Starts with digits instead of letters
        [InlineData("D389370400440532013000")] // Only one letter in country code
        [InlineData("DEAB370400440532013000")] // Letters in check digit position
        [InlineData("DE8937040044@532013000")] // Contains special characters
        [InlineData("DE89 3704 0044 0532 013G 00")] // Contains invalid character 'G' in numeric section (but this might actually be valid in some countries)
        public void Apply_InvalidFormat_ReturnsUnchanged(string invalidIban)
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);

            // Act
            var result = rule.Apply(invalidIban);

            // Assert - invalid format should be returned unchanged
            Assert.Equal(invalidIban, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Apply_NullOrEmpty_ReturnsUnchanged(string input)
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_TooShort_ReturnsUnchanged()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);
            var input = "DE8937040044"; // Only 12 characters (minimum is 15)

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_TooLong_ReturnsUnchanged()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);
            var input = "DE89370400440532013000123456789012"; // 35 characters (maximum is 34)

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Constructor_NegativeKeepLast_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new IBANMaskRule(keepLast: -1));
        }

        [Fact]
        public void Apply_UnknownCountryCode_WithValidFormat_ValidatesChecksumOnly()
        {
            // Arrange - Use a fictional country code "ZZ" with valid format
            // This tests forward compatibility for new country codes
            var rule = new IBANMaskRule(keepLast: 4);

            // Create a valid-looking IBAN for fictional country "ZZ"
            // Note: This will fail checksum validation unless we craft a valid one
            var input = "ZZ82WEST12345698765432"; // Using same format as GB

            // Act
            var result = rule.Apply(input);

            // Assert - without knowing the exact checksum, we expect it to return unchanged
            // (checksum validation will likely fail)
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_MixedCaseWithSpaces_NormalizesAndMasks()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4, preserveGrouping: true);
            var input = "De89 3704 0044 0532 0130 00"; // Mixed case

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("DE89 **** **** **** **30 00", result);
        }

        [Fact]
        public void Apply_IBANWithIrregularSpacing_NormalizesAndMasks()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4, preserveGrouping: true);
            var input = "DE89  3704   0044 0532 0130 00"; // Irregular spacing

            // Act
            var result = rule.Apply(input);

            // Assert - should normalize to regular 4-char groups
            Assert.Equal("DE89 **** **** **** **30 00", result);
        }

        #endregion

        #region Preservation Tests

        [Fact]
        public void Apply_AlwaysPreservesCountryCode()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 0); // Mask everything except country code + check digits
            var input = "DE89370400440532013000";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.StartsWith("DE", result);
            Assert.Equal("DE89******************", result);
        }

        [Fact]
        public void Apply_AlwaysPreservesCheckDigits()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 0);
            var input = "DE89370400440532013000";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.StartsWith("DE89", result);
        }

        [Theory]
        [InlineData("DE89370400440532013000", 4, "3000")]
        [InlineData("GB82WEST12345698765432", 4, "5432")]
        [InlineData("FR1420041010050500013M02606", 4, "2606")]
        public void Apply_PreservesLastNCharacters(string input, int keepLast, string expectedSuffix)
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: keepLast);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.EndsWith(expectedSuffix, result);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Apply_Performance_Meets10KOpsPerSecTarget()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4);
            var input = "DE89370400440532013000";
            var iterations = 10000;

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                rule.Apply(input);
            }
            stopwatch.Stop();

            // Assert - should complete 10,000 operations in less than 1 second
            Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                $"Performance test failed: {iterations} operations took {stopwatch.ElapsedMilliseconds}ms " +
                $"(target: <1000ms). Throughput: {iterations * 1000.0 / stopwatch.ElapsedMilliseconds:F0} ops/sec");
        }

        [Fact]
        public void Apply_PerformanceWithGrouping_Meets10KOpsPerSecTarget()
        {
            // Arrange
            var rule = new IBANMaskRule(keepLast: 4, preserveGrouping: true);
            var input = "DE89 3704 0044 0532 0130 00";
            var iterations = 10000;

            // Act
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                rule.Apply(input);
            }
            stopwatch.Stop();

            // Assert - should complete 10,000 operations in less than 1 second
            Assert.True(stopwatch.ElapsedMilliseconds < 1000,
                $"Performance test with grouping failed: {iterations} operations took {stopwatch.ElapsedMilliseconds}ms " +
                $"(target: <1000ms). Throughput: {iterations * 1000.0 / stopwatch.ElapsedMilliseconds:F0} ops/sec");
        }

        #endregion

        #region Real-World Scenario Tests

        [Fact]
        public void Apply_LoggingScenario_MasksMultipleIBANs()
        {
            // Arrange - simulating masking IBANs in log entries
            var rule = new IBANMaskRule(keepLast: 4);
            var ibans = new[]
            {
                "DE89370400440532013000",
                "GB82WEST12345698765432",
                "FR1420041010050500013M02606",
                "ES9121000418450200051332"
            };

            // Act & Assert
            foreach (var iban in ibans)
            {
                var masked = rule.Apply(iban);

                // Verify each IBAN is masked correctly
                Assert.Contains("*", masked);
                Assert.StartsWith(iban.Substring(0, 4), masked); // Country code + check digits preserved
                Assert.EndsWith(iban.Substring(iban.Length - 4), masked); // Last 4 chars preserved
            }
        }

        [Fact]
        public void Apply_DatabaseExport_ConsistentMasking()
        {
            // Arrange - simulating consistent masking for database export
            var rule = new IBANMaskRule(keepLast: 4);
            var iban = "DE89370400440532013000";

            // Act - apply masking multiple times
            var result1 = rule.Apply(iban);
            var result2 = rule.Apply(iban);
            var result3 = rule.Apply(iban);

            // Assert - results should be identical (deterministic)
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        [Fact]
        public void Apply_GDPRCompliance_PreservesBusinessUtility()
        {
            // Arrange - GDPR requires masking PII while maintaining business utility
            var rule = new IBANMaskRule(keepLast: 4);
            var input = "DE89370400440532013000";

            // Act
            var result = rule.Apply(input);

            // Assert - verify business utility preserved
            Assert.StartsWith("DE", result); // Country still visible for routing
            Assert.EndsWith("3000", result); // Last digits for customer verification
            Assert.Contains("*", result); // Account details masked for privacy
        }

        #endregion

        #region ISO Standard Compliance Tests

        [Fact]
        public void Apply_ISO13616Compliance_SupportsAllStandardCountries()
        {
            // Arrange - test sample IBANs from all major regions
            var rule = new IBANMaskRule(keepLast: 4);

            var testCases = new[]
            {
                // Western Europe
                ("DE89370400440532013000", "Germany"),
                ("FR1420041010050500013M02606", "France"),
                ("GB82WEST12345698765432", "United Kingdom"),

                // Southern Europe
                ("ES9121000418450200051332", "Spain"),
                ("IT60X0542811101000000123456", "Italy"),
                ("GR1601101250000000012300695", "Greece"),

                // Northern Europe
                ("SE4550000000058398257466", "Sweden"),
                ("NO9386011117947", "Norway"),
                ("DK5000400440116243", "Denmark"),

                // Eastern Europe
                ("PL61109010140000071219812874", "Poland"),

                // Other
                ("CH9300762011623852957", "Switzerland")
            };

            // Act & Assert
            foreach (var (iban, country) in testCases)
            {
                var result = rule.Apply(iban);

                // Verify masking occurred (contains asterisks)
                Assert.Contains("*", result);

                // Verify country code preserved
                Assert.StartsWith(iban.Substring(0, 2), result);
            }
        }

        #endregion
    }
}
