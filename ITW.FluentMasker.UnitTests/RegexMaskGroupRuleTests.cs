using ITW.FluentMasker.MaskRules;
using System;
using System.Text.RegularExpressions;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for RegexMaskGroupRule
    /// </summary>
    public class RegexMaskGroupRuleTests
    {
        [Theory]
        [InlineData("555-123-4567", @"(\d{3})-(\d{3})-(\d{4})", 1, "*", "***-123-4567")]  // Mask area code (group 1)
        [InlineData("555-123-4567", @"(\d{3})-(\d{3})-(\d{4})", 2, "*", "555-***-4567")]  // Mask prefix (group 2)
        [InlineData("555-123-4567", @"(\d{3})-(\d{3})-(\d{4})", 3, "*", "555-123-****")]  // Mask line number (group 3)
        [InlineData("123-45-6789", @"(\d{3})-(\d{2})-(\d{4})", 2, "X", "123-XX-6789")]    // Mask middle section of SSN
        [InlineData("Order 12345 ready", @"\d+", 0, "#", "Order ##### ready")]            // Mask entire match (group 0)
        [InlineData("abc123def456", @"\d+", 0, "*", "abc***def***")]                      // Multiple matches
        public void Apply_VariousPatterns_MasksSpecifiedGroup(string input, string pattern, int groupIndex, string maskChar, string expected)
        {
            // Arrange
            var rule = new RegexMaskGroupRule(pattern, groupIndex, maskChar);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new RegexMaskGroupRule(@"(\d+)", 1, "*");

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyInput_ReturnsEmpty()
        {
            // Arrange
            var rule = new RegexMaskGroupRule(@"(\d+)", 1, "*");

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_NoMatch_ReturnsOriginal()
        {
            // Arrange
            var input = "NoDigitsHere";
            var rule = new RegexMaskGroupRule(@"(\d+)", 1, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_GroupIndexOutOfRange_ReturnsOriginalMatch()
        {
            // Arrange
            var input = "123-456";
            var rule = new RegexMaskGroupRule(@"(\d{3})-(\d{3})", 5, "*"); // Group 5 doesn't exist

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result); // Original unchanged when group doesn't exist
        }

        [Fact]
        public void Apply_OptionalGroupNotMatched_ReturnsOriginalMatch()
        {
            // Arrange
            var input = "123-456";
            var rule = new RegexMaskGroupRule(@"(\d{3})-(\d{3})(\d{4})?", 3, "*"); // Group 3 is optional and not present

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result); // Original unchanged when optional group doesn't match
        }

        [Theory]
        [InlineData("USER123", @"(user)(\d+)", 1, "*", "****123")]  // Case-insensitive
        [InlineData("Hello World", @"(hello)", 1, "#", "##### World")]
        public void Apply_WithRegexOptions_RespectsCaseInsensitivity(string input, string pattern, int groupIndex, string maskChar, string expected)
        {
            // Arrange
            var rule = new RegexMaskGroupRule(pattern, groupIndex, maskChar, RegexOptions.IgnoreCase);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_MultipleGroupsInPattern_MasksOnlySpecifiedGroup()
        {
            // Arrange
            var input = "John.Doe@example.com";
            var rule = new RegexMaskGroupRule(@"(\w+)\.(\w+)@(\w+)\.(\w+)", 2, "*"); // Mask last name (group 2)

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("John.***@example.com", result);
        }

        [Fact]
        public void Apply_NestedGroups_MasksCorrectly()
        {
            // Arrange
            var input = "abc(def)ghi";
            var rule = new RegexMaskGroupRule(@"(\w+(\(\w+\)))", 2, "*"); // Group 2 is nested

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("abc*****ghi", result);
        }

        [Fact]
        public void Constructor_NullPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RegexMaskGroupRule(null, 1, "*"));
            Assert.Equal("pattern", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RegexMaskGroupRule("", 1, "*"));
            Assert.Equal("pattern", exception.ParamName);
        }

        [Fact]
        public void Constructor_NegativeGroupIndex_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RegexMaskGroupRule(@"\d+", -1, "*"));
            Assert.Equal("groupIndex", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new RegexMaskGroupRule(@"\d+", 1, null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RegexMaskGroupRule(@"\d+", 1, ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_InvalidPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RegexMaskGroupRule(@"[invalid(", 1, "*"));
            Assert.Equal("pattern", exception.ParamName);
            Assert.Contains("Invalid regex pattern", exception.Message);
        }

        [Theory]
        [InlineData("Test1234Test", @"(\d+)", 1, "XX", "TestXXXXTest")]  // Multi-char mask (uses first char only)
        [InlineData("Order 123 ref 456", @"(\d+)", 1, "AB", "Order AAA ref AAA")]  // All matches of group 1 are masked
        public void Apply_MultiCharMask_UsesFirstCharacter(string input, string pattern, int groupIndex, string maskChar, string expected)
        {
            // Arrange
            var rule = new RegexMaskGroupRule(pattern, groupIndex, maskChar);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_Group0_MasksEntireMatch()
        {
            // Arrange
            var input = "Price: $123.45 and $67.89";
            var rule = new RegexMaskGroupRule(@"\$\d+\.\d+", 0, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Price: ******* and ******", result);
        }

        [Fact]
        public void Apply_LongString_PerformanceTest()
        {
            // Arrange
            var input = string.Concat(System.Linq.Enumerable.Repeat("123-456-7890 ", 1000));
            var rule = new RegexMaskGroupRule(@"(\d{3})-(\d{3})-(\d{4})", 1, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("***-456-7890", result);
            Assert.DoesNotContain("123-456-7890", result);
        }

        [Theory]
        [InlineData("Email: user@example.com", @"(\w+)@(\w+)\.(\w+)", 1, "*", "Email: ****@example.com")]
        [InlineData("IP: 192.168.1.1", @"(\d+)\.(\d+)\.(\d+)\.(\d+)", 3, "*", "IP: 192.168.*.1")]
        public void Apply_RealWorldPatterns_WorksCorrectly(string input, string pattern, int groupIndex, string maskChar, string expected)
        {
            // Arrange
            var rule = new RegexMaskGroupRule(pattern, groupIndex, maskChar);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_UnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var input = "名前: 山田太郎 (Yamada Taro)";
            var rule = new RegexMaskGroupRule(@"(山田)(太郎)", 2, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("名前: 山田** (Yamada Taro)", result);
        }

        [Fact]
        public void Apply_SpecialCharactersInMatch_PreservesStructure()
        {
            // Arrange
            var input = "Code: ABC-123-XYZ";
            var rule = new RegexMaskGroupRule(@"([A-Z]+)-(\d+)-([A-Z]+)", 2, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Code: ABC-***-XYZ", result);
        }

        [Fact]
        public void Apply_OverlappingMatches_HandlesNonGreedy()
        {
            // Arrange
            var input = "aaa111aaa222aaa";
            var rule = new RegexMaskGroupRule(@"(\d+)", 1, "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("aaa***aaa***aaa", result);
        }

        [Fact]
        public void Apply_ZeroLengthMatch_HandlesGracefully()
        {
            // Arrange
            var input = "test";
            var rule = new RegexMaskGroupRule(@"(\b)", 1, "*"); // Zero-length assertion

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("test", result); // No visible change for zero-length matches
        }
    }
}
