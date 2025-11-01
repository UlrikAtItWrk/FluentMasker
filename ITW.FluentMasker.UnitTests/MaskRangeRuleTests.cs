using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for MaskRangeRule
    /// </summary>
    public class MaskRangeRuleTests
    {
        [Theory]
        [InlineData("HelloWorld", 2, 5, "*", "He*****rld")]
        [InlineData("JohnDoe", 0, 3, "*", "***nDoe")]
        [InlineData("Test", 1, 2, "#", "T##t")]
        [InlineData("Hello", 0, 0, "*", "Hello")]  // zero length
        [InlineData("Hello", 10, 5, "*", "Hello")]  // start >= length
        [InlineData("Test", 2, 10, "*", "Te**")]  // length > remaining chars
        [InlineData("", 0, 5, "*", "")]  // empty string
        [InlineData("ABC", 0, 3, "*", "***")]  // Full masking
        public void Apply_VariousInputs_ReturnsExpectedOutput(string input, int start, int length, string mask, string expected)
        {
            // Arrange
            var rule = new MaskRangeRule(start, length, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new MaskRangeRule(2, 3);

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Héllo", 1, 2, "*", "H**lo")]  // Unicode with accents
        [InlineData("你好世界", 1, 2, "*", "你**界")]  // Multi-byte Chinese characters
        [InlineData("Café", 1, 2, "#", "C##é")]
        public void Apply_UnicodeInput_HandlesCorrectly(string input, int start, int length, string mask, string expected)
        {
            // Arrange
            var rule = new MaskRangeRule(start, length, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_NegativeStart_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskRangeRule(-1, 5));
            Assert.Equal("start", exception.ParamName);
        }

        [Fact]
        public void Constructor_NegativeLength_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskRangeRule(2, -1));
            Assert.Equal("length", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskRangeRule(2, 3, null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskRangeRule(2, 3, ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Apply_ZeroLength_ReturnsOriginalString()
        {
            // Arrange
            var input = "Hello";
            var rule = new MaskRangeRule(2, 0, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_StartEqualsLength_ReturnsOriginalString()
        {
            // Arrange
            var input = "Hello";
            var rule = new MaskRangeRule(5, 3, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_StartGreaterThanLength_ReturnsOriginalString()
        {
            // Arrange
            var input = "Hi";
            var rule = new MaskRangeRule(10, 5, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_LengthExceedsRemaining_MasksToEnd()
        {
            // Arrange
            var input = "HelloWorld";
            var rule = new MaskRangeRule(5, 100, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hello*****", result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var rule = new MaskRangeRule(0, 5, "*");

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_LongString_PerformanceTest()
        {
            // Arrange
            var input = new string('x', 10000);
            var rule = new MaskRangeRule(1000, 5000, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(10000, result.Length);
            Assert.Equal('x', result[0]);
            Assert.Equal('x', result[999]);
            Assert.Equal('*', result[1000]);
            Assert.Equal('*', result[5999]);
            Assert.Equal('x', result[6000]);
            Assert.Equal('x', result[9999]);
        }

        [Theory]
        [InlineData("Special!@#$%", 3, 5, "*", "Spe*****@#$%")]
        [InlineData("  Spaces  ", 2, 6, "*", "  ******  ")]
        public void Apply_SpecialCharacters_HandlesCorrectly(string input, int start, int length, string mask, string expected)
        {
            // Arrange
            var rule = new MaskRangeRule(start, length, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("TestString", 2, 4, "XX", "TeXXXXring")]  // Multi-char mask (uses first char only)
        [InlineData("Hello", 1, 3, "AB", "HAAAo")]  // Multi-char mask
        public void Apply_MultiCharMask_UsesFirstCharacter(string input, int start, int length, string mask, string expected)
        {
            // Arrange
            var rule = new MaskRangeRule(start, length, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_StartAtBeginning_MasksFromStart()
        {
            // Arrange
            var input = "HelloWorld";
            var rule = new MaskRangeRule(0, 5, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*****World", result);
        }

        [Fact]
        public void Apply_SingleCharacterMask_WorksCorrectly()
        {
            // Arrange
            var input = "ABCDE";
            var rule = new MaskRangeRule(2, 1, "#");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("AB#DE", result);
        }

        [Fact]
        public void Apply_MaskEntireString_WorksCorrectly()
        {
            // Arrange
            var input = "Test";
            var rule = new MaskRangeRule(0, 4, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("****", result);
        }

        [Fact]
        public void Apply_StartAndLengthBothZero_ReturnsOriginalString()
        {
            // Arrange
            var input = "Test";
            var rule = new MaskRangeRule(0, 0, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Test", result);
        }
    }
}
