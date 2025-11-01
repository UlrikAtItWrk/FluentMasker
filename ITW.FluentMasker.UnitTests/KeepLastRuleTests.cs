using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for KeepLastRule
    /// </summary>
    public class KeepLastRuleTests
    {
        [Theory]
        [InlineData("HelloWorld", 2, "*", "********ld")]
        [InlineData("JohnDoe", 3, "*", "****Doe")]
        [InlineData("Test", 0, "*", "****")]  // zero keep = full masking
        [InlineData("Test", 10, "*", "Test")]  // keepCount >= length
        [InlineData("", 2, "*", "")]  // empty string
        [InlineData("A", 1, "*", "A")]  // single char, keep 1
        [InlineData("AB", 1, "#", "#B")]
        public void Apply_VariousInputs_ReturnsExpectedOutput(string input, int keepCount, string mask, string expected)
        {
            // Arrange
            var rule = new KeepLastRule(keepCount, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new KeepLastRule(2);

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Héllo", 2, "*", "***lo")]  // Unicode with accents
        [InlineData("你好世界", 2, "*", "**世界")]  // Multi-byte Chinese characters
        [InlineData("Café", 2, "#", "##fé")]
        public void Apply_UnicodeInput_HandlesCorrectly(string input, int keepCount, string mask, string expected)
        {
            // Arrange
            var rule = new KeepLastRule(keepCount, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_NegativeKeepCount_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new KeepLastRule(-1));
            Assert.Equal("keepCount", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new KeepLastRule(2, null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new KeepLastRule(2, ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Apply_ZeroKeepCount_MasksEntireString()
        {
            // Arrange
            var input = "Hello";
            var rule = new KeepLastRule(0, "*");

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
            var rule = new KeepLastRule(5, "*");

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
            var rule = new KeepLastRule(10, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var rule = new KeepLastRule(5, "*");

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
            var rule = new KeepLastRule(100, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(10000, result.Length);
            Assert.Equal('*', result[0]);
            Assert.Equal('*', result[9899]);
            Assert.Equal('x', result[9900]);
            Assert.Equal('x', result[9999]);
        }

        [Theory]
        [InlineData("Special!@#$%", 3, "*", "*********#$%")]
        [InlineData("  Spaces  ", 2, "*", "********  ")]
        public void Apply_SpecialCharacters_HandlesCorrectly(string input, int keepCount, string mask, string expected)
        {
            // Arrange
            var rule = new KeepLastRule(keepCount, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("TestString", 6, "XX", "XXXXString")]  // Multi-char mask (uses first char only)
        [InlineData("Hello", 3, "AB", "AAllo")]  // Multi-char mask
        public void Apply_MultiCharMask_UsesFirstCharacter(string input, int keepCount, string mask, string expected)
        {
            // Arrange
            var rule = new KeepLastRule(keepCount, mask);

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
            var rule = new KeepLastRule(1, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*********d", result);
        }

        [Fact]
        public void Apply_SingleCharacter_KeepZero()
        {
            // Arrange
            var input = "X";
            var rule = new KeepLastRule(0, "#");

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
            var rule = new KeepLastRule(1, "#");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("X", result);
        }

        [Fact]
        public void Apply_KeepAlmostAll_MasksOnlyFirst()
        {
            // Arrange
            var input = "Test";
            var rule = new KeepLastRule(3, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*est", result);
        }

        [Fact]
        public void Apply_TwoCharacters_KeepOne()
        {
            // Arrange
            var input = "AB";
            var rule = new KeepLastRule(1, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*B", result);
        }

        [Fact]
        public void Apply_KeepExactlyHalf_MasksFirstHalf()
        {
            // Arrange
            var input = "12345678";  // 8 characters
            var rule = new KeepLastRule(4, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("****5678", result);
        }
    }
}
