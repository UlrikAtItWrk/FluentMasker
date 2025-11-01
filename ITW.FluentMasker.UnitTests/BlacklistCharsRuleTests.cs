using ITW.FluentMasker.MaskRules;
using Xunit;
using System.Collections.Generic;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for BlacklistCharsRule
    /// </summary>
    public class BlacklistCharsRuleTests
    {
        [Theory]
        [InlineData("Email: test@example.com", "@.", "*", "Email: test*example*com")]  // Task requirement test case
        [InlineData("Hello@World!", "@!", "*", "Hello*World*")]
        [InlineData("Card: 1234-5678", ":-", "", "Card 12345678")]
        [InlineData("User_Name-2024!", "_-!", "*", "User*Name*2024*")]
        [InlineData("", "abc", "*", "")]  // empty string
        [InlineData("abc", "abc", "*", "***")]  // all blacklisted
        [InlineData("xyz", "abc", "*", "xyz")]  // none blacklisted
        public void Apply_MaskBlacklisted_ReturnsExpectedOutput(string input, string blacklistedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new BlacklistCharsRule(blacklistedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("ID: 12345", ":", "#", "ID# 12345")]
        [InlineData("Hello", "l", "[X]", "He[X][X]o")]  // multi-char replacement
        [InlineData("Test123", "0123456789", "X", "TestXXX")]
        [InlineData("(555) 123-4567", "()- ", "", "5551234567")]  // remove special chars
        public void Apply_ReplaceBlacklisted_ReturnsExpectedOutput(string input, string blacklistedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new BlacklistCharsRule(blacklistedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new BlacklistCharsRule("abc");

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyInput_ReturnsEmpty()
        {
            // Arrange
            var rule = new BlacklistCharsRule("abc");

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData("Héllo@Wørld", "@", "*", "Héllo*Wørld")]  // Unicode with accents
        [InlineData("你好123世界", "123", "*", "你好***世界")]  // Multi-byte Chinese characters
        [InlineData("Café-123", "-", "*", "Café*123")]
        public void Apply_UnicodeInput_HandlesCorrectly(string input, string blacklistedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new BlacklistCharsRule(blacklistedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Constructor_NullBlacklistedCharsString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new BlacklistCharsRule((string)null));
            Assert.Equal("blacklistedChars", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyBlacklistedCharsString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new BlacklistCharsRule(""));
            Assert.Equal("blacklistedChars", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullBlacklistedCharsEnumerable_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new BlacklistCharsRule((IEnumerable<char>)null));
            Assert.Equal("blacklistedChars", exception.ParamName);
        }

        [Fact]
        public void Constructor_IEnumerableChar_WorksCorrectly()
        {
            // Arrange
            var blacklistedList = new List<char> { 'a', 'e', 'i', 'o', 'u' };
            var rule = new BlacklistCharsRule(blacklistedList, "-");

            // Act
            var result = rule.Apply("Hello");

            // Assert
            Assert.Equal("H-ll-", result);
        }

        [Fact]
        public void Constructor_NullReplaceWith_UsesEmptyString()
        {
            // Arrange
            var rule = new BlacklistCharsRule("xyz", null);

            // Act
            var result = rule.Apply("abcxyz");

            // Assert
            Assert.Equal("abc", result);  // xyz should be removed
        }

        [Theory]
        [InlineData("Special!@#$%^&*()", "!@#$%^&*()", "*", "Special**********")]  // 10 special chars = 10 asterisks
        [InlineData("  Spaces  ", " ", "-", "--Spaces--")]
        [InlineData("Tab\tNewline\n", "\t\n", "", "TabNewline")]
        public void Apply_SpecialCharacters_HandlesCorrectly(string input, string blacklistedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new BlacklistCharsRule(blacklistedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NoMatchingChars_ReturnsOriginal()
        {
            // Arrange
            var rule = new BlacklistCharsRule("xyz");

            // Act
            var result = rule.Apply("Hello");

            // Assert
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void Apply_AllCharsBlacklisted_ReturnsAllMasked()
        {
            // Arrange
            var rule = new BlacklistCharsRule("Hello", "*");

            // Act
            var result = rule.Apply("Hello");

            // Assert
            Assert.Equal("*****", result);
        }

        [Fact]
        public void Apply_MultiCharReplacement_ExpandsCorrectly()
        {
            // Arrange
            var rule = new BlacklistCharsRule("l", "[MASK]");

            // Act
            var result = rule.Apply("Hello");

            // Assert
            // H -> H, e -> e, l -> [MASK], l -> [MASK], o -> o
            Assert.Equal("He[MASK][MASK]o", result);
        }

        [Fact]
        public void Apply_LongString_PerformanceTest()
        {
            // Arrange
            var input = new string('x', 10000) + new string('y', 10000);
            var rule = new BlacklistCharsRule("y", "*");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(20000, result.Length);  // x's remain + y's replaced with *
            Assert.Equal(10000, result.Count(c => c == 'x'));
            Assert.Equal(10000, result.Count(c => c == '*'));
        }

        [Fact]
        public void Apply_DuplicateCharsInBlacklistedSet_HandledCorrectly()
        {
            // Arrange - HashSet should deduplicate
            var rule = new BlacklistCharsRule("aaabbbccc", "*");

            // Act
            var result = rule.Apply("abcxyz");

            // Assert
            Assert.Equal("***xyz", result);
        }

        [Theory]
        [InlineData("Test123", "0123456789", "*", "Test***")]  // Digits masked
        [InlineData("Test123", "Test", "*", "****123")]  // Letters masked
        [InlineData("a1b2c3", "123", "#", "a#b#c#")]  // Digits masked
        public void Apply_MixedContent_MasksCorrectParts(string input, string blacklistedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new BlacklistCharsRule(blacklistedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_CaseSensitive_PreservesCase()
        {
            // Arrange
            var rule = new BlacklistCharsRule("abc", "");  // lowercase only, remove (not replace)

            // Act
            var result = rule.Apply("AbC");

            // Assert
            Assert.Equal("AC", result);  // Only lowercase 'b' is blacklisted and removed
        }

        [Fact]
        public void Apply_EmptyReplaceWith_RemovesChars()
        {
            // Arrange
            var rule = new BlacklistCharsRule(":-() ", "");

            // Act
            var result = rule.Apply("Phone: (555) 123-4567");

            // Assert
            Assert.Equal("Phone5551234567", result);
        }

        [Theory]
        [InlineData("emoji😀test", "😀", "*", "emoji**test")]  // Emoji (multi-byte) masked - treated as 2 chars
        [InlineData("line1\nline2", "\n", " ", "line1 line2")]  // Newline replaced with space
        public void Apply_EdgeCaseCharacters_HandlesCorrectly(string input, string blacklistedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new BlacklistCharsRule(blacklistedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_DefaultReplaceWith_UsesStar()
        {
            // Arrange
            var rule = new BlacklistCharsRule("@.");

            // Act
            var result = rule.Apply("test@example.com");

            // Assert
            Assert.Equal("test*example*com", result);
        }

        [Theory]
        [InlineData("PII: SSN 123-45-6789", "0123456789-", "*", "PII: SSN ***********")]
        [InlineData("Credit Card: 1234-5678-9012-3456", "0123456789", "X", "Credit Card: XXXX-XXXX-XXXX-XXXX")]
        [InlineData("user@domain.com", "@.", "[REDACTED]", "user[REDACTED]domain[REDACTED]com")]
        public void Apply_DataMaskingScenarios_WorksCorrectly(string input, string blacklistedChars, string replaceWith, string expected)
        {
            // Arrange
            var rule = new BlacklistCharsRule(blacklistedChars, replaceWith);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
