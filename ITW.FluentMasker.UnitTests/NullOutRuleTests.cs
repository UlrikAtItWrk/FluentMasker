using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for NullOutRule
    /// </summary>
    public class NullOutRuleTests
    {
        [Fact]
        public void Apply_WithNonNullString_ReturnsNull()
        {
            // Arrange
            var rule = new NullOutRule();
            var input = "HelloWorld";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_WithEmptyString_ReturnsNull()
        {
            // Arrange
            var rule = new NullOutRule();
            var input = "";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_WithNullInput_ReturnsNull()
        {
            // Arrange
            var rule = new NullOutRule();
            string input = null;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_WithLongString_ReturnsNull()
        {
            // Arrange
            var rule = new NullOutRule();
            var input = "This is a very long string with many characters to test performance";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_WithSpecialCharacters_ReturnsNull()
        {
            // Arrange
            var rule = new NullOutRule();
            var input = "!@#$%^&*()_+-=[]{}|;:',.<>?/`~";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_WithUnicodeCharacters_ReturnsNull()
        {
            // Arrange
            var rule = new NullOutRule();
            var input = "Hello 世界 🌍";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Null(result);
        }
    }
}
