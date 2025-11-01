using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for MaskCharClassRule
    /// </summary>
    public class MaskCharClassRuleTests
    {
        #region Digit Tests

        [Theory]
        [InlineData("Test123", "Test***")]  // Acceptance criteria test
        [InlineData("Hello123World456", "Hello***World***")]
        [InlineData("0123456789", "**********")]
        [InlineData("NoDigits", "NoDigits")]
        [InlineData("", "")]
        [InlineData("Code-42-ABC", "Code-**-ABC")]
        public void Apply_MaskDigits_MasksOnlyDigits(string input, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Digit, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_MaskDigits_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Digit, "*");

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Letter Tests

        [Theory]
        [InlineData("Test123", "****123")]  // Acceptance criteria test
        [InlineData("ABC123XYZ", "***123***")]
        [InlineData("HelloWorld", "**********")]
        [InlineData("123456", "123456")]
        [InlineData("", "")]
        [InlineData("Test-456", "****-456")]
        public void Apply_MaskLetters_MasksOnlyLetters(string input, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Letter, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Héllo", "*****")]  // Unicode with accents
        [InlineData("Café", "****")]
        [InlineData("你好世界", "****")]  // Multi-byte Chinese characters
        public void Apply_MaskLetters_UnicodeInput_HandlesCorrectly(string input, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Letter, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region LetterOrDigit Tests

        [Theory]
        [InlineData("Hello-123", "*****-***")]
        [InlineData("Test@456", "****@***")]
        [InlineData("ABC123", "******")]
        [InlineData("!@#$%", "!@#$%")]
        [InlineData("User_Name-2024", "****_****-****")]
        public void Apply_MaskLettersOrDigits_MasksAlphanumeric(string input, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.LetterOrDigit, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Whitespace Tests

        [Theory]
        [InlineData("Hello World", "Hello_World")]
        [InlineData("Hello   World", "Hello___World")]
        [InlineData("First Second Third", "First_Second_Third")]
        [InlineData("NoSpaces", "NoSpaces")]
        [InlineData("  LeadingSpaces", "__LeadingSpaces")]
        [InlineData("TrailingSpaces  ", "TrailingSpaces__")]
        public void Apply_MaskWhitespace_MasksSpaces(string input, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Whitespace, "_");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_MaskWhitespace_TabsAndNewlines_MasksCorrectly()
        {
            // Arrange
            var input = "Line1\tTab\nLine2";
            var rule = new MaskCharClassRule(CharClass.Whitespace, "_");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Line1_Tab_Line2", result);
        }

        #endregion

        #region Punctuation Tests

        [Theory]
        [InlineData("Hello, World!", "Hello* World*")]
        [InlineData("What?!?", "What***")]
        [InlineData("Test.", "Test*")]
        [InlineData("NoMask", "NoMask")]
        [InlineData("(555) 123-4567", "*555* 123*4567")]
        public void Apply_MaskPunctuation_MasksOnlyPunctuation(string input, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Punctuation, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Upper Tests

        [Theory]
        [InlineData("HelloWorld", "*ello*orld")]
        [InlineData("ABC123abc", "***123abc")]
        [InlineData("ALLUPPER", "********")]
        [InlineData("alllower", "alllower")]
        [InlineData("", "")]
        public void Apply_MaskUppercase_MasksOnlyUppercaseLetters(string input, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Upper, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Lower Tests

        [Theory]
        [InlineData("HelloWorld", "H****W****")]
        [InlineData("ABC123abc", "ABC123***")]
        [InlineData("alllower", "********")]
        [InlineData("ALLUPPER", "ALLUPPER")]
        [InlineData("", "")]
        public void Apply_MaskLowercase_MasksOnlyLowercaseLetters(string input, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Lower, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(
                () => new MaskCharClassRule(CharClass.Digit, null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new MaskCharClassRule(CharClass.Digit, ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Theory]
        [InlineData(CharClass.Digit)]
        [InlineData(CharClass.Letter)]
        [InlineData(CharClass.LetterOrDigit)]
        [InlineData(CharClass.Whitespace)]
        [InlineData(CharClass.Punctuation)]
        [InlineData(CharClass.Upper)]
        [InlineData(CharClass.Lower)]
        public void Constructor_AllValidCharClasses_DoesNotThrow(CharClass charClass)
        {
            // Act & Assert - should not throw
            var rule = new MaskCharClassRule(charClass, "*");
            Assert.NotNull(rule);
        }

        #endregion

        #region Multi-character Mask Tests

        [Theory]
        [InlineData("Test123", "XX", "TestXXX")]  // Multi-char mask (uses first char only)
        [InlineData("Hello123", "AB", "HelloAAA")]
        public void Apply_MultiCharMask_UsesFirstCharacter(string input, string mask, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Digit, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Custom Mask Character Tests

        [Theory]
        [InlineData("Test123", "#", "Test###")]
        [InlineData("Test123", "X", "TestXXX")]
        [InlineData("Test123", "-", "Test---")]
        public void Apply_CustomMaskChar_UsesSpecifiedCharacter(string input, string mask, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Digit, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Apply_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Digit, "*");

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_NoMatchingCharacters_ReturnsOriginal()
        {
            // Arrange
            var input = "NoDigitsHere";
            var rule = new MaskCharClassRule(CharClass.Digit, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_AllMatchingCharacters_MasksEntireString()
        {
            // Arrange
            var input = "12345";
            var rule = new MaskCharClassRule(CharClass.Digit, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*****", result);
            Assert.Equal(input.Length, result.Length);
        }

        [Fact]
        public void Apply_LongString_PerformanceTest()
        {
            // Arrange
            var input = new string('a', 5000) + new string('1', 5000);
            var rule = new MaskCharClassRule(CharClass.Digit, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(10000, result.Length);
            Assert.Equal('a', result[0]);
            Assert.Equal('a', result[4999]);
            Assert.Equal('*', result[5000]);
            Assert.Equal('*', result[9999]);
        }

        #endregion

        #region Mixed Content Tests

        [Theory]
        [InlineData("Email: user@example.com", CharClass.Letter, "*", "*****: ****@*******.***")]
        [InlineData("Phone: (555) 123-4567", CharClass.Digit, "X", "Phone: (XXX) XXX-XXXX")]
        [InlineData("Code: ABC-123-XYZ", CharClass.Upper, "#", "#ode: ###-123-###")]
        public void Apply_MixedContent_MasksCorrectCharClass(string input, CharClass charClass, string mask, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(charClass, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Special Characters Tests

        [Theory]
        [InlineData("Special!@#$%123", CharClass.Digit, "*", "Special!@#$%***")]
        [InlineData("  Spaces  123", CharClass.Digit, "*", "  Spaces  ***")]
        [InlineData("Newline\n123", CharClass.Digit, "*", "Newline\n***")]
        public void Apply_SpecialCharacters_HandlesCorrectly(string input, CharClass charClass, string mask, string expected)
        {
            // Arrange
            var rule = new MaskCharClassRule(charClass, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void Apply_ImplementsIMaskRuleInterface()
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Digit, "*");
            IMaskRule interfaceRef = rule;

            // Act
            var result = interfaceRef.Apply("Test123");

            // Assert
            Assert.Equal("Test***", result);
        }

        [Fact]
        public void Apply_ImplementsGenericIMaskRuleInterface()
        {
            // Arrange
            var rule = new MaskCharClassRule(CharClass.Digit, "*");
            IMaskRule<string, string> interfaceRef = rule;

            // Act
            var result = interfaceRef.Apply("Test123");

            // Assert
            Assert.Equal("Test***", result);
        }

        #endregion
    }
}
