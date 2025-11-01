using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for PhoneMaskRule covering international phone number formats
    /// Includes E.164, North American, and European formats
    /// </summary>
    public class PhoneMaskRuleTests
    {
        #region Basic Functionality Tests

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: 2);

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmpty()
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: 2);

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_NoDigits_ReturnsUnchanged()
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: 2);
            string input = "ABC-DEF-GHIJ";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_KeepLastExceedsDigitCount_ReturnsUnchanged()
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: 100);
            string input = "+1-555-1234";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_KeepLastZero_MasksAllDigits()
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: 0);
            string input = "+1-555-1234";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("+*-***-****", result);
        }

        #endregion

        #region Separator Preservation Tests

        [Theory]
        [InlineData("+45 12 34 56 78", 2, "+** ** ** ** 78")]  // Danish format
        [InlineData("(555) 123-4567", 2, "(***) ***-**67")]    // North American with parentheses
        [InlineData("+1-555-123-4567", 4, "+*-***-***-4567")]  // North American with dashes
        [InlineData("+442071234567", 4, "+********4567")]      // E.164 UK format
        [InlineData("+33.1.23.45.67.89", 3, "+**.*.**.**.*7.89")]  // French with dots (11 digits)
        [InlineData("+1 (555) 123-4567", 4, "+* (***) ***-4567")]  // US mixed format
        [InlineData("+44 20 7123 4567", 3, "+** ** **** *567")]    // UK with spaces
        [InlineData("+49 30 12345678", 4, "+** ** ****5678")]      // German format
        [InlineData("+33 1 23 45 67 89", 4, "+** * ** ** 67 89")]  // French spaced (11 digits)
        [InlineData("1-800-FLOWERS", 2, "*-*00-FLOWERS")]          // Vanity number: 4 digits (1,8,0,0), keep last 2 (0,0)
        public void Apply_PreserveSeparators_MasksDigitsCorrectly(string input, int keepLast, string expected)
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: keepLast, preserveSeparators: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Non-Preserving Mode Tests

        [Theory]
        [InlineData("(555) 123-4567", 2, "********67")]    // 10 digits: 8 masked + 2 visible
        [InlineData("+1-555-1234", 3, "*****234")]         // 8 digits: 5 masked + 3 visible
        [InlineData("+442071234567", 4, "********4567")]   // 12 digits: 8 masked + 4 visible
        [InlineData("123.456.7890", 4, "******7890")]      // 10 digits: 6 masked + 4 visible
        [InlineData("+33 1 23 45 67 89", 2, "*********89")] // 11 digits: 9 masked + 2 visible
        public void Apply_NonPreservingMode_ReturnsDigitsOnly(string input, int keepLast, string expected)
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: keepLast, preserveSeparators: false);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region International Format Tests (15+ formats)

        [Theory]
        // North American formats
        [InlineData("+1 555 123 4567", 4, "+* *** *** 4567", "North American E.164")]
        [InlineData("(555) 123-4567", 4, "(***) ***-4567", "US with parentheses")]
        [InlineData("555-123-4567", 4, "***-***-4567", "US without area code parens")]

        // European formats
        [InlineData("+44 20 7123 4567", 4, "+** ** **** 4567", "UK London")]
        [InlineData("+49 30 12345678", 4, "+** ** ****5678", "Germany Berlin")]
        [InlineData("+33 1 23 45 67 89", 4, "+** * ** ** 67 89", "France Paris")]
        [InlineData("+39 06 1234 5678", 4, "+** ** **** 5678", "Italy Rome")]
        [InlineData("+34 91 123 45 67", 4, "+** ** *** 45 67", "Spain Madrid")]  // 10 digits: keep last 4 (4567)

        // Nordic formats
        [InlineData("+45 12 34 56 78", 4, "+** ** ** 56 78", "Denmark")]
        [InlineData("+46 8 123 456 78", 4, "+** * *** *56 78", "Sweden Stockholm")]  // 11 digits: keep last 4 (5678)
        [InlineData("+47 22 12 34 56", 4, "+** ** ** 34 56", "Norway Oslo")]

        // Asian formats
        [InlineData("+81 3 1234 5678", 4, "+** * **** 5678", "Japan Tokyo")]
        [InlineData("+86 10 1234 5678", 4, "+** ** **** 5678", "China Beijing")]
        [InlineData("+91 11 1234 5678", 4, "+** ** **** 5678", "India Delhi")]

        // Other formats
        [InlineData("+61 2 1234 5678", 4, "+** * **** 5678", "Australia Sydney")]
        [InlineData("+55 11 1234 5678", 4, "+** ** **** 5678", "Brazil Sao Paulo")]
        [InlineData("+27 11 123 4567", 4, "+** ** *** 4567", "South Africa Johannesburg")]

        // E.164 without spaces
        [InlineData("+442071234567", 4, "+********4567", "UK E.164 no spaces")]
        [InlineData("+15551234567", 4, "+*******4567", "US E.164 no spaces")]
        public void Apply_InternationalFormats_MasksCorrectly(string input, int keepLast, string expected, string formatDescription)
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: keepLast, preserveSeparators: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Custom Mask Character Tests

        [Theory]
        [InlineData("+1-555-1234", 3, "X", "+X-XXX-X234")]
        [InlineData("(555) 123-4567", 4, "#", "(###) ###-4567")]
        [InlineData("+44 20 7123 4567", 3, "-", "+-- -- ---- -567")]
        public void Apply_CustomMaskChar_UsesCustomMask(string input, int keepLast, string maskChar, string expected)
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: keepLast, preserveSeparators: true, maskChar: maskChar);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Constructor Validation Tests

        [Fact]
        public void Constructor_NegativeKeepLast_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new PhoneMaskRule(keepLast: -1));
            Assert.Equal("keepLast", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new PhoneMaskRule(maskChar: null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Apply_SingleDigit_MasksCorrectly()
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: 0);
            string input = "5";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*", result);
        }

        [Fact]
        public void Apply_OnlyPlusSign_ReturnsUnchanged()
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: 2);
            string input = "+";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("+", result);
        }

        [Theory]
        [InlineData("+1 (555) 123-4567 ext. 123", 4, "+* (***) ***-***7 ext. 123")]  // 14 digits total: keep last 4 (7,1,2,3)
        [InlineData("Call: +1-555-1234", 3, "Call: +*-***-*234")]  // Text prefix preserved
        [InlineData("+1-555-1234 (mobile)", 4, "+*-***-1234 (mobile)")]  // Text suffix preserved
        public void Apply_MixedTextAndNumbers_HandlesCorrectly(string input, int keepLast, string expected)
        {
            // Arrange
            var rule = new PhoneMaskRule(keepLast: keepLast, preserveSeparators: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region IMaskRule Interface Tests

        [Fact]
        public void PhoneMaskRule_ImplementsIMaskRule()
        {
            // Arrange
            var rule = new PhoneMaskRule();

            // Act & Assert
            Assert.IsAssignableFrom<IMaskRule>(rule);
        }

        [Fact]
        public void PhoneMaskRule_ImplementsGenericIMaskRule()
        {
            // Arrange
            var rule = new PhoneMaskRule();

            // Act & Assert
            Assert.IsAssignableFrom<IMaskRule<string, string>>(rule);
        }

        #endregion
    }
}
