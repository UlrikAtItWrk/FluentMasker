using System;
using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for RedactRule
    /// </summary>
    public class RedactRuleTests
    {
        [Fact]
        public void Apply_WithDefaultRedactionText_ReturnsRedacted()
        {
            // Arrange
            var rule = new RedactRule();
            var input = "SensitiveData";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("[REDACTED]", result);
        }

        [Fact]
        public void Apply_WithCustomRedactionText_ReturnsCustomText()
        {
            // Arrange
            var rule = new RedactRule("[HIDDEN]");
            var input = "SensitiveData";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("[HIDDEN]", result);
        }

        [Fact]
        public void Apply_WithEmptyRedactionText_ReturnsEmptyString()
        {
            // Arrange
            var rule = new RedactRule("");
            var input = "SensitiveData";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_WithVariousInputs_AlwaysReturnsSameRedactionText()
        {
            // Arrange
            var rule = new RedactRule("[CENSORED]");

            // Act & Assert
            Assert.Equal("[CENSORED]", rule.Apply("input1"));
            Assert.Equal("[CENSORED]", rule.Apply("different input"));
            Assert.Equal("[CENSORED]", rule.Apply(""));
            Assert.Equal("[CENSORED]", rule.Apply(null));
            Assert.Equal("[CENSORED]", rule.Apply("very long string with many characters"));
        }

        [Fact]
        public void Constructor_WithNullRedactionText_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RedactRule(null));
        }

        [Fact]
        public void Apply_WithUnicodeRedactionText_ReturnsUnicodeText()
        {
            // Arrange
            var rule = new RedactRule("🔒 PRIVATE");
            var input = "SensitiveData";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("🔒 PRIVATE", result);
        }

        [Fact]
        public void Apply_WithLongRedactionText_ReturnsFullRedactionText()
        {
            // Arrange
            var longRedactionText = new string('X', 1000);
            var rule = new RedactRule(longRedactionText);
            var input = "short";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(longRedactionText, result);
            Assert.Equal(1000, result.Length);
        }

        [Fact]
        public void Apply_WithMultipleApplications_ReturnsSameResult()
        {
            // Arrange
            var rule = new RedactRule("[REDACTED]");
            var input = "SensitiveData";

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
