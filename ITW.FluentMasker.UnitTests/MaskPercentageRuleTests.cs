using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for MaskPercentageRule
    /// </summary>
    public class MaskPercentageRuleTests
    {
        [Theory]
        [InlineData("HelloWorld", 0.5, MaskFrom.End, "*", "Hello*****")]  // 10 chars, 50% = 5 chars masked
        [InlineData("Test", 0.5, MaskFrom.Start, "*", "**st")]  // 4 chars, 50% = 2 chars masked
        [InlineData("Hello", 0.6, MaskFrom.Middle, "*", "H***o")]  // 5 chars, 60% = 3 chars masked
        [InlineData("ABC", 0.0, MaskFrom.Start, "*", "ABC")]  // 0% masking
        [InlineData("ABC", 1.0, MaskFrom.Start, "*", "***")]  // 100% masking
        [InlineData("", 0.5, MaskFrom.End, "*", "")]  // empty string
        public void Apply_VariousInputs_ReturnsExpectedOutput(string input, double percentage, MaskFrom from, string mask, string expected)
        {
            // Arrange
            var rule = new MaskPercentageRule(percentage, from, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new MaskPercentageRule(0.5, MaskFrom.End);

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Héllo", 0.4, MaskFrom.Start, "*", "**llo")]  // Unicode with accents, 5 chars * 0.4 = 2
        [InlineData("你好世界", 0.5, MaskFrom.End, "*", "你好**")]  // Multi-byte Chinese, 4 chars * 0.5 = 2
        public void Apply_UnicodeInput_HandlesCorrectly(string input, double percentage, MaskFrom from, string mask, string expected)
        {
            // Arrange
            var rule = new MaskPercentageRule(percentage, from, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_NegativePercentage_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskPercentageRule(-0.1));
            Assert.Equal("percentage", exception.ParamName);
        }

        [Fact]
        public void Constructor_PercentageGreaterThanOne_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskPercentageRule(1.5));
            Assert.Equal("percentage", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskPercentageRule(0.5, MaskFrom.End, null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new MaskPercentageRule(0.5, MaskFrom.End, ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Apply_ZeroPercentage_ReturnsOriginalString()
        {
            // Arrange
            var input = "HelloWorld";
            var rule = new MaskPercentageRule(0.0, MaskFrom.Start, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_OneHundredPercent_MasksEntireString()
        {
            // Arrange
            var input = "Hello";
            var rule = new MaskPercentageRule(1.0, MaskFrom.End, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*****", result);
        }

        [Fact]
        public void Apply_MaskFromStart_MasksFromBeginning()
        {
            // Arrange
            var input = "1234567890";  // 10 chars
            var rule = new MaskPercentageRule(0.3, MaskFrom.Start, "*");  // 30% = 3 chars

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("***4567890", result);
        }

        [Fact]
        public void Apply_MaskFromEnd_MasksFromEnd()
        {
            // Arrange
            var input = "1234567890";  // 10 chars
            var rule = new MaskPercentageRule(0.3, MaskFrom.End, "*");  // 30% = 3 chars

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("1234567***", result);
        }

        [Fact]
        public void Apply_MaskFromMiddle_MasksMiddlePart()
        {
            // Arrange
            var input = "1234567890";  // 10 chars
            var rule = new MaskPercentageRule(0.4, MaskFrom.Middle, "*");  // 40% = 4 chars, keep 3 on each side

            // Act
            var result = rule.Apply(input);

            // Assert
            // (10 - 4) / 2 = 3 kept on each side
            Assert.Equal("123****890", result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var rule = new MaskPercentageRule(0.5, MaskFrom.End, "*");

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_CeilingRounding_MasksCorrectly()
        {
            // Arrange
            var input = "Test";  // 4 chars
            var rule = new MaskPercentageRule(0.26, MaskFrom.Start, "*");  // 4 * 0.26 = 1.04, ceiling = 2

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("**st", result);
        }

        [Theory]
        [InlineData(MaskFrom.Start)]
        [InlineData(MaskFrom.End)]
        [InlineData(MaskFrom.Middle)]
        public void Apply_AllMaskFromOptions_WorkCorrectly(MaskFrom from)
        {
            // Arrange
            var input = "HelloWorld";
            var rule = new MaskPercentageRule(0.5, from, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(input.Length, result.Length);
        }

        [Fact]
        public void Apply_LongString_PerformanceTest()
        {
            // Arrange
            var input = new string('x', 10000);
            var rule = new MaskPercentageRule(0.5, MaskFrom.End, "*");

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
        [InlineData("Special!@#$%", 0.5, MaskFrom.Start, "*", "******l!@#$%")]
        public void Apply_SpecialCharacters_HandlesCorrectly(string input, double percentage, MaskFrom from, string mask, string expected)
        {
            // Arrange
            var rule = new MaskPercentageRule(percentage, from, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("TestString", 0.5, MaskFrom.Start, "XX", "XXXXXtring")]  // Multi-char mask (uses first char only)
        [InlineData("Hello", 0.4, MaskFrom.End, "AB", "HelAA")]  // Multi-char mask, 5 * 0.4 = 2
        public void Apply_MultiCharMask_UsesFirstCharacter(string input, double percentage, MaskFrom from, string mask, string expected)
        {
            // Arrange
            var rule = new MaskPercentageRule(percentage, from, mask);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_ValidBoundaryPercentages_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            var rule1 = new MaskPercentageRule(0.0);
            var rule2 = new MaskPercentageRule(1.0);
            var rule3 = new MaskPercentageRule(0.5);

            Assert.NotNull(rule1);
            Assert.NotNull(rule2);
            Assert.NotNull(rule3);
        }

        [Fact]
        public void Apply_SmallPercentage_CeilsToAtLeastOne()
        {
            // Arrange
            var input = "Test";  // 4 chars
            var rule = new MaskPercentageRule(0.01, MaskFrom.Start, "*");  // 4 * 0.01 = 0.04, ceiling = 1

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("*est", result);
        }

        [Fact]
        public void Apply_MiddleMaskWithOddChars_DistributesCorrectly()
        {
            // Arrange
            var input = "123456789";  // 9 chars (odd)
            var rule = new MaskPercentageRule(0.33, MaskFrom.Middle, "*");  // 33% of 9 = 3 chars

            // Act
            var result = rule.Apply(input);

            // Assert
            // (9 - 3) / 2 = 3 kept on each side
            Assert.Equal("123***789", result);
        }
    }
}
