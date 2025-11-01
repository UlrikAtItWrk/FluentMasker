using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for MaskMiddleRule
    /// </summary>
    public class MaskMiddleRuleTests
    {
        [Theory]
        [InlineData("HelloWorld", 2, 2, "*", "He******ld")]
        [InlineData("JohnDoe", 1, 1, "*", "J*****e")]
        [InlineData("Test", 1, 1, "#", "T##t")]
        [InlineData("Hello", 2, 2, "*", "He*lo")]  // keepFirst + keepLast < length
        [InlineData("Hi", 1, 1, "*", "Hi")]  // keepFirst + keepLast == length
        [InlineData("Test", 0, 0, "*", "****")]  // Full masking when both are 0
        [InlineData("", 2, 2, "*", "")]  // empty string
        [InlineData("ABC", 1, 0, "*", "A**")]
        [InlineData("ABC", 0, 1, "*", "**C")]
        public void Apply_VariousInputs_ReturnsExpectedOutput(string input, int keepFirst, int keepLast, string mask, string expected)
        {
            // Arrange
            var rule = new MaskMiddleRule(keepFirst, keepLast, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new MaskMiddleRule(2, 2);

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Héllo", 1, 1, "*", "H***o")]  // Unicode with accents
        [InlineData("你好世界", 1, 1, "*", "你**界")]  // Multi-byte Chinese characters
        [InlineData("Café", 1, 1, "#", "C##é")]
        public void Apply_UnicodeInput_HandlesCorrectly(string input, int keepFirst, int keepLast, string mask, string expected)
        {
            // Arrange
            var rule = new MaskMiddleRule(keepFirst, keepLast, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_NegativeKeepFirst_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskMiddleRule(-1, 2));
            Assert.Equal("keepFirst", exception.ParamName);
        }

        [Fact]
        public void Constructor_NegativeKeepLast_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskMiddleRule(2, -1));
            Assert.Equal("keepLast", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskMiddleRule(2, 2, null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskMiddleRule(2, 2, ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Apply_BothZero_MasksEntireString()
        {
            // Arrange
            var input = "Hello";
            var rule = new MaskMiddleRule(0, 0, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*****", result);
        }

        [Fact]
        public void Apply_KeepFirstAndLastGreaterThanLength_ReturnsOriginalString()
        {
            // Arrange
            var input = "Hi";
            var rule = new MaskMiddleRule(5, 5, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_KeepFirstAndLastEqualsLength_ReturnsOriginalString()
        {
            // Arrange
            var input = "Test";
            var rule = new MaskMiddleRule(2, 2, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var rule = new MaskMiddleRule(5, 5, "*");

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
            var rule = new MaskMiddleRule(100, 100, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(10000, result.Length);
            Assert.Equal('x', result[0]);
            Assert.Equal('x', result[99]);
            Assert.Equal('*', result[100]);
            Assert.Equal('*', result[9899]);
            Assert.Equal('x', result[9900]);
            Assert.Equal('x', result[9999]);
        }

        [Theory]
        [InlineData("Special!@#$%", 2, 2, "*", "Sp********$%")]
        [InlineData("  Spaces  ", 2, 2, "*", "  ******  ")]
        public void Apply_SpecialCharacters_HandlesCorrectly(string input, int keepFirst, int keepLast, string mask, string expected)
        {
            // Arrange
            var rule = new MaskMiddleRule(keepFirst, keepLast, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("TestString", 3, 3, "XX", "TesXXXXing")]  // Multi-char mask (uses first char only)
        [InlineData("Hello", 1, 1, "AB", "HAAAo")]  // Multi-char mask
        public void Apply_MultiCharMask_UsesFirstCharacter(string input, int keepFirst, int keepLast, string mask, string expected)
        {
            // Arrange
            var rule = new MaskMiddleRule(keepFirst, keepLast, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_OnlyKeepFirst_MasksRest()
        {
            // Arrange
            var input = "HelloWorld";
            var rule = new MaskMiddleRule(3, 0, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hel*******", result);
        }

        [Fact]
        public void Apply_OnlyKeepLast_MasksRest()
        {
            // Arrange
            var input = "HelloWorld";
            var rule = new MaskMiddleRule(0, 3, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*******rld", result);
        }

        [Fact]
        public void Apply_SingleCharacter_MasksWhenBothZero()
        {
            // Arrange
            var input = "X";
            var rule = new MaskMiddleRule(0, 0, "#");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("#", result);
        }

        [Fact]
        public void Apply_SingleCharacter_KeepsWhenOneNonZero()
        {
            // Arrange
            var input = "X";
            var rule = new MaskMiddleRule(1, 0, "#");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("X", result);
        }
    }
}
