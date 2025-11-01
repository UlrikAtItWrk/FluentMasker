using System;
using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for TruncateRule
    /// </summary>
    public class TruncateRuleTests
    {
        [Fact]
        public void Apply_WithShortString_ReturnsOriginal()
        {
            // Arrange
            var rule = new TruncateRule(10);
            var input = "Hello";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void Apply_WithStringEqualToMaxLength_ReturnsOriginal()
        {
            // Arrange
            var rule = new TruncateRule(5);
            var input = "Hello";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void Apply_WithLongString_TruncatesWithDefaultSuffix()
        {
            // Arrange
            var rule = new TruncateRule(10);
            var input = "A very long string";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("A very lo…", result);
            Assert.Equal(10, result.Length);
        }

        [Fact]
        public void Apply_WithLongString_TruncatesWithCustomSuffix()
        {
            // Arrange
            var rule = new TruncateRule(15, "...");
            var input = "This is a very long comment";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("This is a ve...", result);
            Assert.Equal(15, result.Length);
        }

        [Fact]
        public void Apply_WithSuffixLongerThanMaxLength_ReturnsOnlySuffix()
        {
            // Arrange
            var rule = new TruncateRule(3, "...");
            var input = "Hello World";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("...", result);
            Assert.Equal(3, result.Length);
        }

        [Fact]
        public void Apply_WithEmptyString_ReturnsEmpty()
        {
            // Arrange
            var rule = new TruncateRule(10);
            var input = "";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_WithNullString_ReturnsNull()
        {
            // Arrange
            var rule = new TruncateRule(10);
            string input = null;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_WithZeroMaxLength_ReturnsEmpty()
        {
            // Arrange
            var rule = new TruncateRule(0, "");
            var input = "Hello";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Constructor_WithNegativeMaxLength_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new TruncateRule(-1));
        }

        [Fact]
        public void Apply_WithNullSuffix_TreatsSuffixAsEmpty()
        {
            // Arrange
            var rule = new TruncateRule(10, null);
            var input = "A very long string";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("A very lon", result);
            Assert.Equal(10, result.Length);
        }

        [Fact]
        public void Apply_WithEmptySuffix_TruncatesWithoutSuffix()
        {
            // Arrange
            var rule = new TruncateRule(10, "");
            var input = "A very long string";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("A very lon", result);
            Assert.Equal(10, result.Length);
        }

        [Fact]
        public void Apply_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var rule = new TruncateRule(10, "…");
            var input = "Hello 世界 🌍 test";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(10, result.Length);
            Assert.True(result.EndsWith("…"));
        }

        [Fact]
        public void Apply_TaskExampleTest_ProducesExpectedResult()
        {
            // Arrange - From task.md acceptance criteria
            var rule = new TruncateRule(10, "…");
            var input = "A very long string";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("A very lo…", result);
        }

        [Fact]
        public void Apply_WithLongSuffix_ConsidersSuffixLength()
        {
            // Arrange
            var rule = new TruncateRule(10, "[...]");
            var input = "Hello World, this is a test";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hello[...]", result);
            Assert.Equal(10, result.Length);
        }

        [Fact]
        public void Apply_MultipleApplications_ProducesSameResult()
        {
            // Arrange
            var rule = new TruncateRule(10);
            var input = "A very long string";

            // Act
            var result1 = rule.Apply(input);
            var result2 = rule.Apply(input);
            var result3 = rule.Apply(input);

            // Assert
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }
    }
}
