using ITW.FluentMasker.MaskRules;
using System;
using System.Text.RegularExpressions;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for RegexReplaceRule
    /// </summary>
    public class RegexReplaceRuleTests
    {
        [Theory]
        [InlineData("Order123", @"\d", "X", "OrderXXX")]  // Replace all digits
        [InlineData("Hello World", @"\s", "_", "Hello_World")]  // Replace whitespace
        [InlineData("Test-123-ABC", @"\d+", "###", "Test-###-ABC")]  // Replace digit sequences
        [InlineData("user@gmail.com", @"@[\w.-]+", "@example.com", "user@example.com")]  // Replace email domain
        [InlineData("Price: $100.50", @"\$\d+\.\d+", "$XX.XX", "Price: $XX.XX")]  // Replace price
        [InlineData("ID: ABC-123-XYZ", @"[A-Z]{3}", "***", "ID: ***-123-***")]  // Replace letter sequences
        public void Apply_VariousPatterns_ReplacesCorrectly(string input, string pattern, string replacement, string expected)
        {
            // Arrange
            var rule = new RegexReplaceRule(pattern, replacement);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new RegexReplaceRule(@"\d", "X");

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyInput_ReturnsEmpty()
        {
            // Arrange
            var rule = new RegexReplaceRule(@"\d", "X");

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
            var rule = new RegexReplaceRule(@"\d", "X");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Theory]
        [InlineData("Hello World", @"hello", "HELLO", RegexOptions.IgnoreCase, "HELLO World")]
        [InlineData("TEST test TeSt", @"test", "XXX", RegexOptions.IgnoreCase, "XXX XXX XXX")]
        [InlineData("ABC abc Abc", @"abc", "***", RegexOptions.IgnoreCase, "*** *** ***")]
        public void Apply_WithRegexOptions_RespectsCaseInsensitivity(string input, string pattern, string replacement, RegexOptions options, string expected)
        {
            // Arrange
            var rule = new RegexReplaceRule(pattern, replacement, options);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("123-45-6789", @"(\d{3})-(\d{2})-(\d{4})", "$1-XX-$3", "123-XX-6789")]  // Mask middle section of SSN
        [InlineData("John Doe", @"(\w+) (\w+)", "$2, $1", "Doe, John")]  // Swap first and last name
        [InlineData("user@example.com", @"(\w+)@(\w+)\.(\w+)", "$1@$2.xxx", "user@example.xxx")]  // Replace TLD
        [InlineData("(555) 123-4567", @"\((\d{3})\) (\d{3})-(\d{4})", "$1-$2-$3", "555-123-4567")]  // Format phone number
        public void Apply_WithCaptureGroups_UsesBackreferences(string input, string pattern, string replacement, string expected)
        {
            // Arrange
            var rule = new RegexReplaceRule(pattern, replacement);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_MultipleMatches_ReplacesAll()
        {
            // Arrange
            var input = "The numbers are 1, 2, 3, and 4.";
            var rule = new RegexReplaceRule(@"\d", "#");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("The numbers are #, #, #, and #.", result);
        }

        [Fact]
        public void Apply_EmptyReplacement_RemovesMatches()
        {
            // Arrange
            var input = "Remove123All456Numbers789";
            var rule = new RegexReplaceRule(@"\d+", "");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("RemoveAllNumbers", result);
        }

        [Fact]
        public void Constructor_NullPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RegexReplaceRule(null, "X"));
            Assert.Equal("pattern", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RegexReplaceRule("", "X"));
            Assert.Equal("pattern", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullReplacement_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new RegexReplaceRule(@"\d", null));
            Assert.Equal("replacement", exception.ParamName);
        }

        [Fact]
        public void Constructor_InvalidPattern_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new RegexReplaceRule(@"[invalid(", "X"));
            Assert.Equal("pattern", exception.ParamName);
            Assert.Contains("Invalid regex pattern", exception.Message);
        }

        [Theory]
        [InlineData("Price: $19.99", @"\$(\d+\.\d+)", "USD $1", "Price: USD 19.99")]
        [InlineData("Date: 2025-10-31", @"(\d{4})-(\d{2})-(\d{2})", "$2/$3/$1", "Date: 10/31/2025")]
        [InlineData("RGB(255,128,0)", @"RGB\((\d+),(\d+),(\d+)\)", "R=$1 G=$2 B=$3", "R=255 G=128 B=0")]
        public void Apply_RealWorldPatterns_WorksCorrectly(string input, string pattern, string replacement, string expected)
        {
            // Arrange
            var rule = new RegexReplaceRule(pattern, replacement);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_UnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var input = "価格: ¥1000 for 商品";
            var rule = new RegexReplaceRule(@"¥\d+", "¥XXX");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("価格: ¥XXX for 商品", result);
        }

        [Fact]
        public void Apply_SpecialCharacters_EscapesCorrectly()
        {
            // Arrange
            var input = "Email: test@example.com";
            var rule = new RegexReplaceRule(@"@", " [AT] ");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Email: test [AT] example.com", result);
        }

        [Fact]
        public void Apply_LongString_PerformanceTest()
        {
            // Arrange
            var input = string.Concat(System.Linq.Enumerable.Repeat("abc123def456 ", 1000));
            var rule = new RegexReplaceRule(@"\d+", "XXX");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("abcXXXdefXXX", result);
            Assert.DoesNotContain("123", result);
            Assert.DoesNotContain("456", result);
        }

        [Fact]
        public void Apply_ComplexPattern_WorksCorrectly()
        {
            // Arrange
            var input = "Phone: (555) 123-4567, Cell: (555) 987-6543";
            var rule = new RegexReplaceRule(@"\(\d{3}\) \d{3}-\d{4}", "[REDACTED]");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Phone: [REDACTED], Cell: [REDACTED]", result);
        }

        [Theory]
        [InlineData("test@example.com", @"(\w+)@(\w+)\.(\w+)", "$1@[hidden].$3", "test@[hidden].com")]
        [InlineData("192.168.1.1", @"(\d+)\.(\d+)\.(\d+)\.(\d+)", "$1.$2.X.X", "192.168.X.X")]
        [InlineData("Card: 1234-5678-9012-3456", @"(\d{4})-(\d{4})-(\d{4})-(\d{4})", "$1-****-****-$4", "Card: 1234-****-****-3456")]
        public void Apply_MaskingSensitiveData_WorksCorrectly(string input, string pattern, string replacement, string expected)
        {
            // Arrange
            var rule = new RegexReplaceRule(pattern, replacement);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_WhitespaceNormalization_WorksCorrectly()
        {
            // Arrange
            var input = "This   has    multiple     spaces";
            var rule = new RegexReplaceRule(@"\s+", " ");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("This has multiple spaces", result);
        }

        [Fact]
        public void Apply_RemoveHtmlTags_WorksCorrectly()
        {
            // Arrange
            var input = "<p>Hello <strong>World</strong>!</p>";
            var rule = new RegexReplaceRule(@"<[^>]+>", "");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hello World!", result);
        }

        [Fact]
        public void Apply_ReplaceWithLiteral_WorksCorrectly()
        {
            // Arrange
            var input = "Cost: $50.00";
            var rule = new RegexReplaceRule(@"\$\d+\.\d+", "[PRICE]");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Cost: [PRICE]", result);
        }

        [Theory]
        [InlineData("Version 1.2.3", @"(\d+)\.(\d+)\.(\d+)", "v$1.$2.$3", "Version v1.2.3")]
        [InlineData("https://example.com", @"https?://", "", "example.com")]
        [InlineData("Code: AB-CD-EF", @"([A-Z]{2})-([A-Z]{2})-([A-Z]{2})", "$1$2$3", "Code: ABCDEF")]
        public void Apply_VariousReplacements_WorksCorrectly(string input, string pattern, string replacement, string expected)
        {
            // Arrange
            var rule = new RegexReplaceRule(pattern, replacement);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_MultilinePattern_WorksWithSingleLine()
        {
            // Arrange
            var input = "Line 1\nLine 2\nLine 3";
            var rule = new RegexReplaceRule(@"Line \d", "Row X");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Row X\nRow X\nRow X", result);
        }

        [Fact]
        public void Apply_WordBoundaries_WorksCorrectly()
        {
            // Arrange
            var input = "The theater is there";
            var rule = new RegexReplaceRule(@"\bthe\b", "THE", RegexOptions.IgnoreCase);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("THE theater is there", result);
        }

        [Fact]
        public void Apply_NamedGroups_WorksCorrectly()
        {
            // Arrange
            var input = "John Doe, age 30";
            var rule = new RegexReplaceRule(@"(?<first>\w+) (?<last>\w+), age (?<age>\d+)", "${last}, ${first} (${age})");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Doe, John (30)", result);
        }

        [Fact]
        public void Apply_GreedyVsNonGreedy_WorksCorrectly()
        {
            // Arrange
            var input = "<div>content</div>";
            var ruleGreedy = new RegexReplaceRule(@"<.*>", "[TAG]");
            var ruleNonGreedy = new RegexReplaceRule(@"<.*?>", "[TAG]");

            // Act
            var resultGreedy = ruleGreedy.Apply(input);
            var resultNonGreedy = ruleNonGreedy.Apply(input);

            // Assert
            Assert.Equal("[TAG]", resultGreedy);  // Matches entire string
            Assert.Equal("[TAG]content[TAG]", resultNonGreedy);  // Matches each tag separately
        }

        [Fact]
        public void Apply_LookaheadLookbehind_WorksCorrectly()
        {
            // Arrange
            var input = "Price: $100 USD";
            var rule = new RegexReplaceRule(@"(?<=\$)\d+", "XXX");  // Positive lookbehind

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Price: $XXX USD", result);
        }

        [Fact]
        public void Apply_EscapedReplacementString_WorksCorrectly()
        {
            // Arrange
            var input = "Path: /home/user/file.txt";
            var rule = new RegexReplaceRule(@"/", @"\");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(@"Path: \home\user\file.txt", result);
        }

        [Theory]
        [InlineData("ABC123xyz", @"[a-z]+", "***", "ABC123***")]
        [InlineData("ABC123xyz", @"[A-Z]+", "***", "***123xyz")]
        [InlineData("test123TEST456", @"[a-z]+", "XXX", "XXX123TEST456")]  // Only lowercase matches
        public void Apply_CaseSensitiveByDefault_WorksCorrectly(string input, string pattern, string replacement, string expected)
        {
            // Arrange
            var rule = new RegexReplaceRule(pattern, replacement);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
