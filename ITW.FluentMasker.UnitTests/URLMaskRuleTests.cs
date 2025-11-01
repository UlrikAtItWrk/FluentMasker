using ITW.FluentMasker.MaskRules;
using ITW.FluentMasker.Extensions;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for URLMaskRule
    /// </summary>
    public class URLMaskRuleTests
    {
        #region Basic URL Tests

        [Fact]
        public void Apply_SimpleURL_NoMasking_ReturnsUnchanged()
        {
            // Arrange
            var rule = new URLMaskRule();
            var input = "https://example.com/path";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new URLMaskRule();

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmpty()
        {
            // Arrange
            var rule = new URLMaskRule();

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData("not-a-url")]
        [InlineData("missing-scheme.com")]
        [InlineData("/relative/path")]
        [InlineData("file.txt")]
        public void Apply_InvalidURL_ReturnsUnchanged(string input)
        {
            // Arrange
            var rule = new URLMaskRule(hideQuery: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        #endregion

        #region Hide Query String Tests

        [Theory]
        [InlineData("https://example.com/path?token=abc123", "https://example.com/path")]
        [InlineData("https://example.com?key=value", "https://example.com/")]
        [InlineData("http://api.example.com/users?id=123&token=secret", "http://api.example.com/users")]
        public void Apply_HideQuery_RemovesEntireQueryString(string input, string expected)
        {
            // Arrange
            var rule = new URLMaskRule(hideQuery: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_HideQuery_NoQueryString_ReturnsUnchanged()
        {
            // Arrange
            var rule = new URLMaskRule(hideQuery: true);
            var input = "https://example.com/path";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_HideQuery_WithFragment_PreservesFragment()
        {
            // Arrange
            var rule = new URLMaskRule(hideQuery: true);
            var input = "https://example.com/path?token=abc#section";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://example.com/path#section", result);
        }

        #endregion

        #region Mask Specific Query Keys Tests

        [Fact]
        public void Apply_MaskSingleQueryKey_MasksCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "token" });
            var input = "https://api.example.com/users?token=abc123&user=john";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("token=%2A%2A%2A", result); // *** URL encoded
            Assert.Contains("user=john", result);
        }

        [Fact]
        public void Apply_MaskMultipleQueryKeys_MasksAllSpecified()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "token", "apiKey" });
            var input = "https://api.example.com/data?token=secret&apiKey=key123&user=john";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("token=%2A%2A%2A", result);
            Assert.Contains("apiKey=%2A%2A%2A", result);
            Assert.Contains("user=john", result);
        }

        [Fact]
        public void Apply_MaskQueryKey_KeyNotPresent_ReturnsUnchanged()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "password" });
            var input = "https://api.example.com/users?user=john&id=123";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("user=john", result);
            Assert.Contains("id=123", result);
        }

        [Fact]
        public void Apply_MaskQueryKey_CaseSensitive()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "Token" });
            var input = "https://api.example.com/users?token=abc123";

            // Act
            var result = rule.Apply(input);

            // Assert
            // Should NOT mask because "Token" != "token"
            Assert.Contains("token=abc123", result);
        }

        [Fact]
        public void Apply_MaskQueryKey_EmptyValue_HandlesCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "token" });
            var input = "https://api.example.com/users?token=&user=john";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("token=%2A%2A%2A", result);
            Assert.Contains("user=john", result);
        }

        [Fact]
        public void Apply_MaskQueryKey_ParameterWithoutValue_HandlesCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "flag" });
            var input = "https://api.example.com/users?flag&user=john";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("flag=%2A%2A%2A", result);
            Assert.Contains("user=john", result);
        }

        #endregion

        #region Mask Path Segments Tests

        [Fact]
        public void Apply_MaskSinglePathSegment_MasksCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 1 });
            var input = "https://api.example.com/users/12345/profile";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://api.example.com/users/***/profile", result);
        }

        [Fact]
        public void Apply_MaskMultiplePathSegments_MasksAllSpecified()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 1, 3 });
            var input = "https://api.example.com/users/12345/orders/67890";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://api.example.com/users/***/orders/***", result);
        }

        [Fact]
        public void Apply_MaskPathSegment_IndexOutOfRange_IgnoresInvalidIndex()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 10 });
            var input = "https://api.example.com/users/12345";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_MaskPathSegment_NegativeIndex_IgnoresNegativeIndex()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { -1, 1 });
            var input = "https://api.example.com/users/12345";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://api.example.com/users/***", result);
        }

        [Fact]
        public void Apply_MaskPathSegment_RootPath_ReturnsUnchanged()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 0 });
            var input = "https://api.example.com/";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_MaskPathSegment_FirstSegment_MasksCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 0 });
            var input = "https://api.example.com/users/12345";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://api.example.com/***/12345", result);
        }

        #endregion

        #region Combined Masking Tests

        [Fact]
        public void Apply_MaskQueryAndPath_BothMaskedCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(
                maskQueryKeys: new[] { "token" },
                maskPathSegments: new[] { 1 }
            );
            var input = "https://api.example.com/users/12345?token=secret&user=john";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("/users/***", result);
            Assert.Contains("token=%2A%2A%2A", result);
            Assert.Contains("user=john", result);
        }

        [Fact]
        public void Apply_HideQueryTakesPrecedence_IgnoresMaskQueryKeys()
        {
            // Arrange
            var rule = new URLMaskRule(
                hideQuery: true,
                maskQueryKeys: new[] { "token" }
            );
            var input = "https://api.example.com/users?token=secret&user=john";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://api.example.com/users", result);
        }

        #endregion

        #region URL Components Preservation Tests

        [Theory]
        [InlineData("http://example.com/path", "http")]
        [InlineData("https://example.com/path", "https")]
        [InlineData("ftp://example.com/path", "ftp")]
        public void Apply_PreservesScheme(string input, string expectedScheme)
        {
            // Arrange
            var rule = new URLMaskRule(hideQuery: true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.StartsWith(expectedScheme + "://", result);
        }

        [Fact]
        public void Apply_PreservesPort()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "token" });
            var input = "https://api.example.com:8080/users?token=secret";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains(":8080", result);
        }

        [Fact]
        public void Apply_PreservesFragment()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "token" });
            var input = "https://example.com/page?token=secret#section";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.EndsWith("#section", result);
        }

        [Fact]
        public void Apply_PreservesHostname()
        {
            // Arrange
            var rule = new URLMaskRule(hideQuery: true);
            var input = "https://api.example.com/path?query=value";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("api.example.com", result);
        }

        [Fact]
        public void Apply_PreservesAuthentication()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "token" });
            var input = "https://user:pass@example.com/path?token=secret";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("user:pass@", result);
        }

        #endregion

        #region URL Encoding Tests

        [Fact]
        public void Apply_EncodedQueryValue_MasksCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "redirect" });
            var input = "https://example.com/login?redirect=https%3A%2F%2Fother.com";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("redirect=%2A%2A%2A", result);
        }

        [Fact]
        public void Apply_SpecialCharsInQuery_HandlesCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "search" });
            var input = "https://example.com/search?search=hello%20world&page=1";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("search=%2A%2A%2A", result);
            Assert.Contains("page=1", result);
        }

        [Fact]
        public void Apply_PercentInPathSegment_HandlesCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 1 });
            var input = "https://example.com/files/file%20name.txt";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("/files/***", result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Apply_MultipleQueryParametersWithSameKey_MasksAll()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "id" });
            var input = "https://example.com/search?id=1&id=2&id=3";

            // Act
            var result = rule.Apply(input);

            // Assert
            // All id parameters should be masked
            var idCount = result.Split(new[] { "id=" }, StringSplitOptions.None).Length - 1;
            Assert.Equal(3, idCount);
            Assert.Contains("id=%2A%2A%2A", result);
        }

        [Fact]
        public void Apply_EmptyQueryString_HandlesCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "token" });
            var input = "https://example.com/path?";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://example.com/path", result);
        }

        [Fact]
        public void Apply_TrailingSlashInPath_Preserved()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 0 });
            var input = "https://example.com/users/";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://example.com/***/", result);
        }

        [Fact]
        public void Apply_VeryLongURL_HandlesCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "data" });
            var longValue = new string('a', 1000);
            var input = $"https://example.com/path?data={longValue}&other=value";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("data=%2A%2A%2A", result);
            Assert.Contains("other=value", result);
        }

        #endregion

        #region Custom Mask Value Tests

        [Fact]
        public void Apply_CustomMaskValue_Query_UsesCustomValue()
        {
            // Arrange
            var rule = new URLMaskRule(
                maskQueryKeys: new[] { "token" },
                maskValue: "[REDACTED]"
            );
            var input = "https://example.com/path?token=secret";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("token=%5BREDACTED%5D", result); // URL encoded
        }

        [Fact]
        public void Apply_CustomMaskValue_Path_UsesCustomValue()
        {
            // Arrange
            var rule = new URLMaskRule(
                maskPathSegments: new[] { 1 },
                maskValue: "XXX"
            );
            var input = "https://example.com/users/12345/profile";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://example.com/users/XXX/profile", result);
        }

        [Fact]
        public void Apply_NullMaskValue_UsesDefault()
        {
            // Arrange
            var rule = new URLMaskRule(
                maskQueryKeys: new[] { "token" },
                maskValue: null
            );
            var input = "https://example.com/path?token=secret";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("token=%2A%2A%2A", result); // Default ***
        }

        #endregion

        #region International URLs Tests

        [Fact]
        public void Apply_InternationalDomainName_HandlesCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "token" });
            // Using punycode representation of international domain
            var input = "https://xn--r8jz45g.jp/path?token=secret&user=john";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("token=%2A%2A%2A", result);
            Assert.Contains("user=john", result);
        }

        [Fact]
        public void Apply_UnicodeInPath_HandlesCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 1 });
            // Using URL-encoded path segments
            var input = "https://example.com/users/%E7%94%A8%E6%88%B7123/profile";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("/users/***", result);
            Assert.Contains("profile", result);
        }

        #endregion

        #region Performance Consideration Tests

        [Fact]
        public void Apply_ManyQueryParameters_HandlesEfficiently()
        {
            // Arrange
            var rule = new URLMaskRule(maskQueryKeys: new[] { "token" });
            var queryParams = string.Join("&", Enumerable.Range(1, 50).Select(i => $"param{i}=value{i}"));
            var input = $"https://example.com/path?token=secret&{queryParams}";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("token=%2A%2A%2A", result);
            Assert.Contains("param1=value1", result);
            Assert.Contains("param50=value50", result);
        }

        [Fact]
        public void Apply_DeepPathNesting_HandlesEfficiently()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 5 });
            var deepPath = string.Join("/", Enumerable.Range(1, 10).Select(i => $"level{i}"));
            var input = $"https://example.com/{deepPath}";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("/level5/", result);
            Assert.Contains("/***/", result);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Apply_Performance_MeetsRequirement()
        {
            // Arrange
            var rule = new URLMaskRule(
                maskQueryKeys: new[] { "token", "apiKey", "password" },
                maskPathSegments: new[] { 1, 3 }
            );
            var input = "https://api.example.com/users/12345/orders/67890?token=secret&apiKey=key123&user=john";
            var iterations = 10000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                rule.Apply(input);
            }
            stopwatch.Stop();

            // Assert - Should complete 10,000 iterations in less than 2 seconds (5,000 ops/sec)
            var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
            Assert.True(opsPerSecond >= 5000, 
                $"Performance requirement not met: {opsPerSecond:N0} ops/sec (required: ? 5,000 ops/sec)");
        }

        #endregion

        #region Constructor Edge Cases

        [Fact]
        public void Constructor_EmptyArrays_CreatesValidInstance()
        {
            // Act
            var rule = new URLMaskRule(
                maskQueryKeys: new string[0],
                maskPathSegments: new int[0]
            );

            // Assert
            Assert.NotNull(rule);
        }

        [Fact]
        public void Constructor_NullArrays_CreatesValidInstance()
        {
            // Act
            var rule = new URLMaskRule(
                maskQueryKeys: null,
                maskPathSegments: null
            );
            var input = "https://example.com/path?query=value";

            // Assert
            Assert.NotNull(rule);
            var result = rule.Apply(input);
            Assert.Equal(input, result);
        }

        [Fact]
        public void Constructor_DuplicateIndices_HandlesCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(maskPathSegments: new[] { 1, 1, 1 });
            var input = "https://example.com/users/12345/profile";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("https://example.com/users/***/profile", result);
        }

        #endregion

        #region Fluent API Integration Tests

        [Fact]
        public void Apply_DirectRuleUsage_WorksCorrectly()
        {
            // Arrange
            var rule = new URLMaskRule(
                maskQueryKeys: new[] { "token", "apiKey" },
                maskPathSegments: new[] { 1 }
            );
            var input = "https://api.example.com/users/12345?token=secret&user=john";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("/users/***", result);
            Assert.Contains("token=%2A%2A%2A", result);
            Assert.Contains("user=john", result);
        }

        #endregion
    }
}
