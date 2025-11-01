using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for MaskEndRule
    /// </summary>
    public class MaskEndRuleTests
    {
        [Theory]
        [InlineData("JohnDoe", 2, "*", "JohnD**")]
        [InlineData("AB", 3, "*", "**")]  // count > length
        [InlineData("", 2, "*", "")]      // empty
        [InlineData("Test", 0, "*", "Test")]  // zero count
        [InlineData("HelloWorld", 5, "*", "Hello*****")]
        [InlineData("A", 1, "*", "*")]
        [InlineData("Test", 4, "#", "####")]  // count == length
        public void Apply_VariousInputs_ReturnsExpectedOutput(string input, int count, string mask, string expected)
        {
            // Arrange
            var rule = new MaskEndRule(count, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new MaskEndRule(2);

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Héllo", 2, "*", "Hél**")]  // Unicode with accents
        [InlineData("你好世界", 2, "*", "你好**")]  // Multi-byte Chinese characters
        [InlineData("Café", 2, "#", "Ca##")]
        public void Apply_UnicodeInput_HandlesCorrectly(string input, int count, string mask, string expected)
        {
            // Arrange
            var rule = new MaskEndRule(count, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_NegativeCount_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskEndRule(-1));
            Assert.Equal("count", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskEndRule(2, null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskEndRule(2, ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Theory]
        [InlineData("TestString", 3, "XX", "TestStrXXX")]  // Multi-char mask (uses first char only)
        [InlineData("Hello", 2, "AB", "HelAA")]  // Multi-char mask
        public void Apply_MultiCharMask_UsesFirstCharacter(string input, int count, string mask, string expected)
        {
            // Arrange
            var rule = new MaskEndRule(count, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_CountEqualsLength_MasksEntireString()
        {
            // Arrange
            var input = "Hello";
            var rule = new MaskEndRule(5, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*****", result);
            Assert.Equal(input.Length, result.Length);
        }

        [Fact]
        public void Apply_CountGreaterThanLength_MasksEntireString()
        {
            // Arrange
            var input = "Hi";
            var rule = new MaskEndRule(10, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("**", result);
            Assert.Equal(input.Length, result.Length);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var rule = new MaskEndRule(5, "*");

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_ZeroCount_ReturnsOriginalString()
        {
            // Arrange
            var input = "NoMasking";
            var rule = new MaskEndRule(0, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_LongString_PerformanceTest()
        {
            // Arrange
            var input = new string('x', 10000);
            var rule = new MaskEndRule(5000, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(10000, result.Length);
            Assert.Equal('x', result[0]);
            Assert.Equal('x', result[4999]);
            Assert.Equal('*', result[5000]);
            Assert.Equal('*', result[9999]);
        }

        [Theory]
        [InlineData("Special!@#$%", 3, "*", "Special!@***")]
        [InlineData("  Spaces  ", 2, "*", "  Spaces**")]
        public void Apply_SpecialCharacters_HandlesCorrectly(string input, int count, string mask, string expected)
        {
            // Arrange
            var rule = new MaskEndRule(count, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_SingleCharacter_MasksCorrectly()
        {
            // Arrange
            var input = "X";
            var rule = new MaskEndRule(1, "#");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("#", result);
        }
    }
}
