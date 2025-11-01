using ITW.FluentMasker.MaskRules;
using System;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for CardMaskRule - PCI-DSS compliant credit card masking
    /// </summary>
    public class CardMaskRuleTests
    {
        #region Basic Card Masking Tests

        [Theory]
        [InlineData("1234567890123456", "************3456")]  // Default: keepLast=4
        [InlineData("4532015112830366", "************0366")]  // Valid Visa
        [InlineData("5425233430109903", "************9903")]  // Valid MasterCard
        [InlineData("378282246310005", "***********0005")]   // Valid Amex (15 digits)
        public void Apply_BasicCardNumbers_DefaultKeepLast4_MasksCorrectly(string input, string expected)
        {
            // Arrange
            var rule = new CardMaskRule();  // Default: keepFirst=0, keepLast=4

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1234567890123456", 6, "123456******3456")]  // BIN + Last 4
        [InlineData("4532015112830366", 6, "453201******0366")]  // Visa BIN + Last 4
        [InlineData("5425233430109903", 6, "542523******9903")]  // MasterCard BIN + Last 4
        public void Apply_KeepFirst6AndLast4_ShowsBINAndLast4(string input, int keepFirst, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(keepFirst: keepFirst, keepLast: 4);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1234567890123456", 0, 6, "**********123456")]
        [InlineData("4532015112830366", 0, 10, "******5112830366")]  // Max PCI-DSS (10 digits) - last 10 digits
        [InlineData("1234567890123456", 2, 8, "12******90123456")]  // First 2 + last 8
        public void Apply_VariousKeepFirstKeepLast_MasksCorrectly(string input, int keepFirst, int keepLast, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(keepFirst: keepFirst, keepLast: keepLast);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Grouping Preservation Tests

        [Theory]
        [InlineData("1234 5678 9012 3456", "**** **** **** 3456")]  // Space grouping
        [InlineData("1234-5678-9012-3456", "****-****-****-3456")]  // Dash grouping
        [InlineData("1234 5678-9012 3456", "**** ****-**** 3456")]  // Mixed grouping
        public void Apply_PreserveGrouping_True_PreservesSpacesAndDashes(string input, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(preserveGrouping: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1234 5678 9012 3456", "************3456")]
        [InlineData("1234-5678-9012-3456", "************3456")]
        [InlineData("1234 5678-9012 3456", "************3456")]
        public void Apply_PreserveGrouping_False_RemovesAllGrouping(string input, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(preserveGrouping: false);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("4532 0151 1283 0366", 6, 4, "4532 01** **** 0366")]  // Visa with BIN
        [InlineData("5425-2334-3010-9903", 6, 4, "5425-23**-****-9903")]  // MasterCard with dashes
        [InlineData("3782 822463 10005", 6, 4, "3782 82**** *0005")]     // Amex (4-6-5 format): 6 first (378282) + 5 masked + 4 last (0005)
        public void Apply_PreserveGrouping_WithKeepFirst_PreservesFormat(string input, int keepFirst, int keepLast, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(keepFirst: keepFirst, keepLast: keepLast, preserveGrouping: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Luhn Validation Tests

        [Theory]
        [InlineData("4532015112830366")]  // Valid Visa
        [InlineData("5425233430109903")]  // Valid MasterCard
        [InlineData("378282246310005")]   // Valid Amex
        [InlineData("6011111111111117")]  // Valid Discover
        [InlineData("3530111333300000")]  // Valid JCB
        public void Apply_ValidLuhn_ValidateTrue_DoesNotThrow(string validCard)
        {
            // Arrange
            var rule = new CardMaskRule(validateLuhn: true);

            // Act
            var result = rule.Apply(validCard);

            // Assert - Should not throw and should mask correctly
            Assert.NotNull(result);
            Assert.EndsWith(validCard.Substring(validCard.Length - 4), result);
        }

        [Theory]
        [InlineData("1234567890123456")]  // Invalid Luhn checksum
        [InlineData("4532015112830367")]  // Off by 1 digit
        [InlineData("5425233430109904")]  // Invalid checksum
        public void Apply_InvalidLuhn_ValidateTrue_ThrowsFormatException(string invalidCard)
        {
            // Arrange
            var rule = new CardMaskRule(validateLuhn: true);

            // Act & Assert
            var exception = Assert.Throws<FormatException>(() => rule.Apply(invalidCard));
            Assert.Contains("Luhn check failed", exception.Message);
        }

        [Theory]
        [InlineData("1234567890123456")]  // Invalid Luhn but validation off
        [InlineData("0000000000000000")]  // All zeros
        public void Apply_InvalidLuhn_ValidateFalse_ProcessesWithoutValidation(string invalidCard)
        {
            // Arrange
            var rule = new CardMaskRule(validateLuhn: false);

            // Act
            var result = rule.Apply(invalidCard);

            // Assert - Should process without throwing
            Assert.NotNull(result);
            Assert.EndsWith(invalidCard.Substring(invalidCard.Length - 4), result);
        }

        [Theory]
        [InlineData("4532 0151 1283 0366")]  // Visa with spaces
        [InlineData("5425-2334-3010-9903")]  // MasterCard with dashes
        public void Apply_ValidLuhn_WithGrouping_ValidatesCorrectly(string validCardWithGrouping)
        {
            // Arrange
            var rule = new CardMaskRule(validateLuhn: true, preserveGrouping: true);

            // Act
            var result = rule.Apply(validCardWithGrouping);

            // Assert - Should validate digits only, ignore grouping characters
            Assert.NotNull(result);
        }

        #endregion

        #region Card Format Tests (Visa, MasterCard, Amex)

        [Theory]
        [InlineData("4532015112830366", "************0366")]  // Visa 16 digits
        [InlineData("4916338506082832", "************2832")]  // Another Visa
        [InlineData("4024007198964305", "************4305")]  // Visa Debit
        public void Apply_VisaCards_MasksCorrectly(string visaCard, string expected)
        {
            // Arrange
            var rule = new CardMaskRule();

            // Act
            var result = rule.Apply(visaCard);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("5425233430109903", "************9903")]  // MasterCard
        [InlineData("5105105105105100", "************5100")]  // Another MasterCard
        [InlineData("2720994326013722", "************3722")]  // MasterCard (starts with 2)
        public void Apply_MasterCardCards_MasksCorrectly(string masterCard, string expected)
        {
            // Arrange
            var rule = new CardMaskRule();

            // Act
            var result = rule.Apply(masterCard);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("378282246310005", "***********0005")]   // Amex 15 digits
        [InlineData("371449635398431", "***********8431")]   // Another Amex
        [InlineData("378734493671000", "***********1000")]   // Corporate Amex
        public void Apply_AmexCards_15Digits_MasksCorrectly(string amexCard, string expected)
        {
            // Arrange
            var rule = new CardMaskRule();  // Default keepLast=4

            // Act
            var result = rule.Apply(amexCard);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("4532 0151 1283 0366", "**** **** **** 0366")]         // Visa 4-4-4-4
        [InlineData("5425-2334-3010-9903", "****-****-****-9903")]         // MasterCard with dashes
        [InlineData("3782 822463 10005", "**** ****** *0005")]             // Amex 4-6-5 (15 digits, keepLast=4, so 11 masked + 4 kept)
        [InlineData("6011 1111 1111 1117", "**** **** **** 1117")]         // Discover
        public void Apply_VariousCardFormats_PreservesGrouping(string input, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(preserveGrouping: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region PCI-DSS Compliance Tests

        [Fact]
        public void Constructor_KeepFirstAndKeepLastExceed10_ThrowsArgumentException()
        {
            // Act & Assert - PCI-DSS allows max 10 digits visible
            var exception = Assert.Throws<ArgumentException>(() =>
                new CardMaskRule(keepFirst: 6, keepLast: 5));  // Total = 11, exceeds limit
            Assert.Contains("must not exceed 10", exception.Message);
            Assert.Contains("PCI-DSS", exception.Message);
        }

        [Theory]
        [InlineData(0, 10)]   // Max last 10 (edge case)
        [InlineData(10, 0)]   // Max first 10 (edge case)
        [InlineData(6, 4)]    // Standard BIN + last 4
        [InlineData(5, 5)]    // Split evenly
        public void Constructor_TotalVisible10OrLess_DoesNotThrow(int keepFirst, int keepLast)
        {
            // Act
            var rule = new CardMaskRule(keepFirst: keepFirst, keepLast: keepLast);

            // Assert
            Assert.NotNull(rule);
        }

        [Theory]
        [InlineData(11, 0)]
        [InlineData(0, 11)]
        [InlineData(7, 5)]
        [InlineData(100, 100)]
        public void Constructor_TotalVisibleExceeds10_ThrowsArgumentException(int keepFirst, int keepLast)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new CardMaskRule(keepFirst: keepFirst, keepLast: keepLast));
            Assert.Contains("PCI-DSS", exception.Message);
        }

        [Theory]
        [InlineData("1234567890123456", 0, 4, "************3456")]   // PCI-DSS compliant (4 digits)
        [InlineData("1234567890123456", 6, 4, "123456******3456")]   // PCI-DSS compliant (10 digits)
        [InlineData("1234567890123456", 0, 10, "******7890123456")]   // PCI-DSS limit (10 digits) - last 10 digits
        public void Apply_PCIDSSCompliantConfigurations_MasksCorrectly(string input, int keepFirst, int keepLast, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(keepFirst: keepFirst, keepLast: keepLast);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Edge Cases and Null/Empty Tests

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new CardMaskRule();

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmpty()
        {
            // Arrange
            var rule = new CardMaskRule();

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData("123", "123")]           // Too short (3 digits, keepFirst+keepLast=4, 4>=3, return as-is)
        [InlineData("1234", "1234")]         // Exactly keepLast (4 digits, keepFirst+keepLast=4, 4>=4, return as-is)
        [InlineData("12345", "*2345")]       // 5 digits: mask 1 digit
        [InlineData("123456789012", "********9012")] // 12 digits: mask 8 digits
        public void Apply_ShortCardNumbers_HandlesCorrectly(string shortCard, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(keepLast: 4);  // keepLast=4

            // Act
            var result = rule.Apply(shortCard);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("No digits here!")]
        [InlineData("abcdefghijklmnop")]
        [InlineData("---")]
        public void Apply_NoDigits_ReturnsOriginal(string noDigitsInput)
        {
            // Arrange
            var rule = new CardMaskRule();

            // Act
            var result = rule.Apply(noDigitsInput);

            // Assert - Should return original if no digits found
            Assert.Equal(noDigitsInput, result);
        }

        [Theory]
        [InlineData("Card: 1234567890123456", "Card: ************3456")]
        [InlineData("CC#1234-5678-9012-3456", "CC#****-****-****-3456")]
        [InlineData("Payment (1234 5678 9012 3456) processed", "Payment (**** **** **** 3456) processed")]
        public void Apply_CardWithText_MasksDigitsOnly(string input, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(preserveGrouping: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1234", "1234")]      // 4 digits (keepFirst+keepLast=4, 4>=4, return as-is)
        [InlineData("12345", "*2345")]    // 5 digits (keepFirst+keepLast=4, 4<5, mask 1 digit)
        public void Apply_CardLengthVariations_MasksCorrectly(string shortCard, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(keepLast: 4);

            // Act
            var result = rule.Apply(shortCard);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Custom Mask Character Tests

        [Theory]
        [InlineData("1234567890123456", "#", "############3456")]
        [InlineData("1234567890123456", "X", "XXXXXXXXXXXX3456")]
        [InlineData("1234567890123456", "-", "------------3456")]
        public void Apply_CustomMaskChar_UsesCustomCharacter(string input, string maskChar, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(maskChar: maskChar);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1234 5678 9012 3456", "#", "#### #### #### 3456")]
        [InlineData("1234-5678-9012-3456", "X", "XXXX-XXXX-XXXX-3456")]
        public void Apply_CustomMaskChar_WithGrouping_PreservesFormat(string input, string maskChar, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(maskChar: maskChar, preserveGrouping: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Constructor Validation Tests

        [Fact]
        public void Constructor_NegativeKeepFirst_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new CardMaskRule(keepFirst: -1));
            Assert.Equal("keepFirst", exception.ParamName);
        }

        [Fact]
        public void Constructor_NegativeKeepLast_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new CardMaskRule(keepLast: -1));
            Assert.Equal("keepLast", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new CardMaskRule(maskChar: null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new CardMaskRule(maskChar: ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            // Act
            var rule = new CardMaskRule(
                keepFirst: 6,
                keepLast: 4,
                preserveGrouping: true,
                validateLuhn: true,
                maskChar: "#");

            // Assert
            Assert.NotNull(rule);
        }

        #endregion

        #region Real-World Card Number Tests

        [Theory]
        [InlineData("4532 0151 1283 0366", true, "**** **** **** 0366")]  // Valid Visa with Luhn
        [InlineData("5425 2334 3010 9903", true, "**** **** **** 9903")]  // Valid MasterCard with Luhn
        [InlineData("3782 822463 10005", true, "**** ****** *0005")]     // Valid Amex with Luhn (15 digits, keepLast=4)
        public void Apply_RealWorldCards_WithLuhnValidation_MasksCorrectly(string input, bool validateLuhn, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(validateLuhn: validateLuhn, preserveGrouping: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("4532015112830366", 6, 4, "453201******0366")]  // Visa BIN display
        [InlineData("5425233430109903", 6, 4, "542523******9903")]  // MasterCard BIN display
        public void Apply_BINDisplayScenario_ShowsFirst6Last4(string input, int keepFirst, int keepLast, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(keepFirst: keepFirst, keepLast: keepLast);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Zero and Special Value Tests

        [Theory]
        [InlineData("0000000000000000")]
        [InlineData("1111111111111111")]
        [InlineData("9999999999999999")]
        public void Apply_RepeatingDigits_MasksCorrectly(string input)
        {
            // Arrange
            var rule = new CardMaskRule();

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.EndsWith(input.Substring(input.Length - 4), result);
            Assert.StartsWith("************", result);
        }

        [Theory]
        [InlineData("0000 0000 0000 0000", "**** **** **** 0000")]
        [InlineData("1111-1111-1111-1111", "****-****-****-1111")]
        public void Apply_RepeatingDigits_WithGrouping_PreservesFormat(string input, string expected)
        {
            // Arrange
            var rule = new CardMaskRule(preserveGrouping: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
