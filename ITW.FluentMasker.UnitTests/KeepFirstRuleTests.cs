using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for KeepFirstRule
    /// </summary>
    public class KeepFirstRuleTests
    {
        [Theory]
        [InlineData("HelloWorld", 2, "*", "He********")]
        [InlineData("JohnDoe", 4, "*", "John***")]
        [InlineData("Test", 0, "*", "****")]  // zero keep = full masking
        [InlineData("Test", 10, "*", "Test")]  // keepCount >= length
        [InlineData("", 2, "*", "")]  // empty string
        [InlineData("A", 1, "*", "A")]  // single char, keep 1
        [InlineData("AB", 1, "#", "A#")]
        public void Apply_VariousInputs_ReturnsExpectedOutput(string input, int keepCount, string mask, string expected)
        {
            // Arrange
            var rule = new KeepFirstRule(keepCount, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new KeepFirstRule(2);

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Héllo", 2, "*", "Hé***")]  // Unicode with accents
        [InlineData("你好世界", 2, "*", "你好**")]  // Multi-byte Chinese characters
        [InlineData("Café", 3, "#", "Caf#")]
        public void Apply_UnicodeInput_HandlesCorrectly(string input, int keepCount, string mask, string expected)
        {
            // Arrange
            var rule = new KeepFirstRule(keepCount, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_NegativeKeepCount_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new KeepFirstRule(-1));
            Assert.Equal("keepCount", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new KeepFirstRule(2, null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new KeepFirstRule(2, ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Apply_ZeroKeepCount_MasksEntireString()
        {
            // Arrange
            var input = "Hello";
            var rule = new KeepFirstRule(0, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*****", result);
            Assert.Equal(input.Length, result.Length);
        }

        [Fact]
        public void Apply_KeepCountEqualsLength_ReturnsOriginalString()
        {
            // Arrange
            var input = "Hello";
            var rule = new KeepFirstRule(5, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_KeepCountGreaterThanLength_ReturnsOriginalString()
        {
            // Arrange
            var input = "Hi";
            var rule = new KeepFirstRule(10, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var rule = new KeepFirstRule(5, "*");

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
            var rule = new KeepFirstRule(100, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(10000, result.Length);
            Assert.Equal('x', result[0]);
            Assert.Equal('x', result[99]);
            Assert.Equal('*', result[100]);
            Assert.Equal('*', result[9999]);
        }

        [Theory]
        [InlineData("Special!@#$%", 3, "*", "Spe*********")]
        [InlineData("  Spaces  ", 2, "*", "  ********")]
        public void Apply_SpecialCharacters_HandlesCorrectly(string input, int keepCount, string mask, string expected)
        {
            // Arrange
            var rule = new KeepFirstRule(keepCount, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("TestString", 4, "XX", "TestXXXXXX")]  // Multi-char mask (uses first char only)
        [InlineData("Hello", 2, "AB", "HeAAA")]  // Multi-char mask
        public void Apply_MultiCharMask_UsesFirstCharacter(string input, int keepCount, string mask, string expected)
        {
            // Arrange
            var rule = new KeepFirstRule(keepCount, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_KeepOne_MasksRest()
        {
            // Arrange
            var input = "HelloWorld";
            var rule = new KeepFirstRule(1, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("H*********", result);
        }

        [Fact]
        public void Apply_SingleCharacter_KeepZero()
        {
            // Arrange
            var input = "X";
            var rule = new KeepFirstRule(0, "#");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("#", result);
        }

        [Fact]
        public void Apply_SingleCharacter_KeepOne()
        {
            // Arrange
            var input = "X";
            var rule = new KeepFirstRule(1, "#");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("X", result);
        }

        [Fact]
        public void Apply_KeepAlmostAll_MasksOnlyLast()
        {
            // Arrange
            var input = "Test";
            var rule = new KeepFirstRule(3, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Tes*", result);
        }
    }
}
