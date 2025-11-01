using ITW.FluentMasker.MaskRules;
using Xunit;
using System.Collections.Generic;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for WhitelistCharsRule
    /// </summary>
    public class WhitelistCharsRuleTests
    {
        [Theory]
        [InlineData("Hello@World123!", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", "", "HelloWorld123")]
        [InlineData("Card: 1234-5678", "0123456789", "", "12345678")]
        [InlineData("User_Name-2024!", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", "", "UserName2024")]
        [InlineData("(555) 123-4567", "0123456789", "", "5551234567")]
        [InlineData("", "abc", "", "")]  // empty string
        [InlineData("abc", "abc", "", "abc")]  // all whitelisted
        [InlineData("xyz", "abc", "", "")]  // none whitelisted
        public void Apply_RemoveNonWhitelisted_ReturnsExpectedOutput(string input, string allowedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new WhitelistCharsRule(allowedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("Card: 1234-5678", "0123456789", "*", "******1234*5678")]
        [InlineData("ID: 12345", "0123456789", "#", "####12345")]
        [InlineData("Hello", "o", "[X]", "[X][X][X][X]o")]  // multi-char replacement
        [InlineData("Test123", "0123456789", "X", "XXXX123")]
        public void Apply_ReplaceNonWhitelisted_ReturnsExpectedOutput(string input, string allowedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new WhitelistCharsRule(allowedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new WhitelistCharsRule("abc");

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyInput_ReturnsEmpty()
        {
            // Arrange
            var rule = new WhitelistCharsRule("abc");

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData("Héllo@Wørld", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", "", "HlloWrld")]  // Unicode with accents
        [InlineData("你好123世界", "0123456789", "", "123")]  // Multi-byte Chinese characters
        [InlineData("Café-123", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", "", "Caf123")]
        public void Apply_UnicodeInput_HandlesCorrectly(string input, string allowedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new WhitelistCharsRule(allowedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_NullAllowedCharsString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new WhitelistCharsRule((string)null));
            Assert.Equal("allowedChars", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyAllowedCharsString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new WhitelistCharsRule(""));
            Assert.Equal("allowedChars", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullAllowedCharsEnumerable_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new WhitelistCharsRule((IEnumerable<char>)null));
            Assert.Equal("allowedChars", exception.ParamName);
        }

        [Fact]
        public void Constructor_IEnumerableChar_WorksCorrectly()
        {
            // Arrange
            var allowedList = new List<char> { 'a', 'e', 'i', 'o', 'u' };
            var rule = new WhitelistCharsRule(allowedList, "-");

            // Act
            var result = rule.Apply("Hello");

            // Assert
            Assert.Equal("-e--o", result);
        }

        [Fact]
        public void Constructor_NullReplaceWith_UsesEmptyString()
        {
            // Arrange
            var rule = new WhitelistCharsRule("abc", null);

            // Act
            var result = rule.Apply("abcxyz");

            // Assert
            Assert.Equal("abc", result);  // xyz should be removed
        }

        [Theory]
        [InlineData("Special!@#$%^&*()", "!@#$%^&*()", "", "!@#$%^&*()")]
        [InlineData("  Spaces  ", " ", "", "    ")]  // 2 + 2 = 4 spaces
        [InlineData("Tab\tNewline\n", "\t\n", "", "\t\n")]
        public void Apply_SpecialCharacters_HandlesCorrectly(string input, string allowedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new WhitelistCharsRule(allowedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NoMatchingChars_ReturnsEmpty()
        {
            // Arrange
            var rule = new WhitelistCharsRule("xyz");

            // Act
            var result = rule.Apply("Hello");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_AllCharsMatch_ReturnsOriginal()
        {
            // Arrange
            var rule = new WhitelistCharsRule("Hello");

            // Act
            var result = rule.Apply("Hello");

            // Assert
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void Apply_MultiCharReplacement_ExpandsCorrectly()
        {
            // Arrange
            var rule = new WhitelistCharsRule("o", "[MASK]");

            // Act
            var result = rule.Apply("Hello");

            // Assert
            // H -> [MASK], e -> [MASK], l -> [MASK], l -> [MASK], o -> o
            Assert.Equal("[MASK][MASK][MASK][MASK]o", result);
        }

        [Fact]
        public void Apply_LongString_PerformanceTest()
        {
            // Arrange
            var input = new string('x', 10000) + new string('y', 10000);
            var rule = new WhitelistCharsRule("x");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(10000, result.Length);  // Only x's remain
            Assert.All(result, c => Assert.Equal('x', c));
        }

        [Fact]
        public void Apply_DuplicateCharsInAllowedSet_HandledCorrectly()
        {
            // Arrange - HashSet should deduplicate
            var rule = new WhitelistCharsRule("aaabbbccc");

            // Act
            var result = rule.Apply("abcxyz");

            // Assert
            Assert.Equal("abc", result);
        }

        [Theory]
        [InlineData("Test123", "0123456789", "*", "****123")]  // Letters replaced
        [InlineData("Test123", "Test", "*", "Test***")]  // Digits replaced
        [InlineData("a1b2c3", "abc", "#", "a#b#c#")]  // Alternate digits replaced
        public void Apply_MixedContent_MasksCorrectParts(string input, string allowedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new WhitelistCharsRule(allowedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_CaseSensitive_PreservesCase()
        {
            // Arrange
            var rule = new WhitelistCharsRule("abc");  // lowercase only

            // Act
            var result = rule.Apply("AbC");

            // Assert
            Assert.Equal("b", result);  // Only lowercase 'b' remains, 'A' (uppercase) and 'C' (uppercase) removed
        }

        [Fact]
        public void Apply_EmptyReplaceWith_RemovesChars()
        {
            // Arrange
            var rule = new WhitelistCharsRule("0123456789", "");

            // Act
            var result = rule.Apply("Phone: 555-1234");

            // Assert
            Assert.Equal("5551234", result);
        }

        [Theory]
        [InlineData("emoji😀test", "abcdefghijklmnopqrstuvwxyz", "*", "emoji**test")]  // Emoji (multi-byte char) replaced with **
        [InlineData("line1\nline2", "12", "#", "####1#####2")]  // Only digits 1 and 2 remain, rest replaced
        public void Apply_EdgeCaseCharacters_HandlesCorrectly(string input, string allowedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new WhitelistCharsRule(allowedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
