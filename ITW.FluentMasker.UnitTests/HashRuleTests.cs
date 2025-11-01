using ITW.FluentMasker.MaskRules;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for HashRule - cryptographic hashing for GDPR pseudonymization
    /// </summary>
    public class HashRuleTests
    {
        #region SHA256 Algorithm Tests

        [Fact]
        public void Apply_SHA256WithStaticSalt_Produces64CharHexString()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex);

            // Act
            var result = rule.Apply("test@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(64, result.Length);
            Assert.Matches("^[a-f0-9]{64}$", result); // Lowercase hex
        }

        [Theory]
        [InlineData(HashAlgorithmType.SHA256, 64)]
        [InlineData(HashAlgorithmType.SHA512, 128)]
        [InlineData(HashAlgorithmType.MD5, 32)]
        public void Apply_DifferentAlgorithms_ProducesCorrectHexLength(HashAlgorithmType algorithm, int expectedLength)
        {
            // Arrange
            var rule = new HashRule(algorithm, SaltMode.Static, OutputFormat.Hex);

            // Act
            var result = rule.Apply("sensitive-data");

            // Assert
            Assert.Equal(expectedLength, result.Length);
            Assert.Matches($"^[a-f0-9]{{{expectedLength}}}$", result);
        }

        #endregion

        #region Salt Mode Tests - Determinism

        [Fact]
        public void Apply_StaticSaltMode_IsDeterministic()
        {
            // Arrange
            var staticSalt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex, staticSalt);

            // Act
            var result1 = rule.Apply("john.doe@example.com");
            var result2 = rule.Apply("john.doe@example.com");

            // Assert
            Assert.Equal(result1, result2); // Same input produces same output
        }

        [Fact]
        public void Apply_StaticSaltModeWithDefaultSalt_IsDeterministic()
        {
            // Arrange - Create rule without providing staticSalt (uses generated default)
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex);

            // Act
            var result1 = rule.Apply("test-input");
            var result2 = rule.Apply("test-input");

            // Assert
            Assert.Equal(result1, result2); // Same input produces same output with same rule instance
        }

        [Fact]
        public void Apply_PerRecordSaltMode_IsNonDeterministic()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerRecord, OutputFormat.Hex);

            // Act
            var result1 = rule.Apply("same-input");
            var result2 = rule.Apply("same-input");

            // Assert
            Assert.NotEqual(result1, result2); // Same input produces different output
        }

        [Fact]
        public void Apply_PerFieldSaltMode_IsDeterministicPerField()
        {
            // Arrange
            var ruleEmail = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerField, OutputFormat.Hex, fieldName: "Email");
            var ruleSSN = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerField, OutputFormat.Hex, fieldName: "SSN");

            // Act
            var result1 = ruleEmail.Apply("user@example.com");
            var result2 = ruleEmail.Apply("user@example.com");
            var result3 = ruleSSN.Apply("user@example.com");

            // Assert
            Assert.Equal(result1, result2); // Same field, same input → same output
            Assert.NotEqual(result1, result3); // Different field → different output (prevents cross-field correlation)
        }

        #endregion

        #region Output Format Tests

        [Fact]
        public void Apply_HexOutputFormat_ProducesLowercaseHex()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex);

            // Act
            var result = rule.Apply("test");

            // Assert
            Assert.Matches("^[a-f0-9]+$", result);
        }

        [Fact]
        public void Apply_Base64OutputFormat_ProducesValidBase64()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Base64);

            // Act
            var result = rule.Apply("test");

            // Assert
            Assert.Matches("^[A-Za-z0-9+/]+=*$", result); // Valid Base64 pattern
            Assert.NotNull(Convert.FromBase64String(result)); // Can decode
        }

        [Fact]
        public void Apply_Base64UrlOutputFormat_ProducesUrlSafeBase64()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Base64Url);

            // Act
            var result = rule.Apply("test");

            // Assert
            Assert.DoesNotContain("+", result);
            Assert.DoesNotContain("/", result);
            Assert.DoesNotContain("=", result); // No padding
            Assert.Matches("^[A-Za-z0-9_-]+$", result); // URL-safe characters only
        }

        #endregion

        #region MD5 Warning Test

        [Fact]
        public void Constructor_MD5Algorithm_DisplaysWarning()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            var originalConsoleOut = Console.Out;
            Console.SetOut(consoleOutput);

            try
            {
                // Act
                var rule = new HashRule(HashAlgorithmType.MD5, SaltMode.Static, OutputFormat.Hex);

                // Assert
                var output = consoleOutput.ToString();
                Assert.Contains("WARNING", output, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("MD5", output);
                Assert.Contains("broken", output, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Console.SetOut(originalConsoleOut);
            }
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new HashRule();

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            var rule = new HashRule();

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData("Héllo Wörld")]
        [InlineData("你好世界")]
        [InlineData("Привет мир")]
        [InlineData("مرحبا بالعالم")]
        public void Apply_UnicodeInput_HandlesCorrectly(string input)
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(64, result.Length);
            Assert.Matches("^[a-f0-9]{64}$", result);
        }

        #endregion

        #region Constructor Validation Tests

        [Fact]
        public void Constructor_PerFieldModeWithoutFieldName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new HashRule(HashAlgorithmType.SHA256, SaltMode.PerField, OutputFormat.Hex, fieldName: null));

            Assert.Equal("fieldName", exception.ParamName);
        }

        [Fact]
        public void Constructor_PerFieldModeWithEmptyFieldName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new HashRule(HashAlgorithmType.SHA256, SaltMode.PerField, OutputFormat.Hex, fieldName: ""));

            Assert.Equal("fieldName", exception.ParamName);
        }

        [Fact]
        public void Constructor_StaticModeWithNullSalt_GeneratesDefaultSalt()
        {
            // Arrange & Act
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex, staticSalt: null);
            var result = rule.Apply("test");

            // Assert - Should not throw and should produce valid hash
            Assert.Equal(64, result.Length);
            Assert.Matches("^[a-f0-9]{64}$", result);
        }

        #endregion

        #region Algorithm/Salt/Format Combination Tests

        [Theory]
        [InlineData(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex)]
        [InlineData(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Base64)]
        [InlineData(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Base64Url)]
        [InlineData(HashAlgorithmType.SHA512, SaltMode.Static, OutputFormat.Hex)]
        [InlineData(HashAlgorithmType.SHA512, SaltMode.Static, OutputFormat.Base64)]
        [InlineData(HashAlgorithmType.SHA512, SaltMode.Static, OutputFormat.Base64Url)]
        [InlineData(HashAlgorithmType.MD5, SaltMode.Static, OutputFormat.Hex)]
        [InlineData(HashAlgorithmType.MD5, SaltMode.Static, OutputFormat.Base64)]
        [InlineData(HashAlgorithmType.MD5, SaltMode.Static, OutputFormat.Base64Url)]
        public void Apply_AllAlgorithmSaltFormatCombinations_ProducesValidOutput(
            HashAlgorithmType algorithm,
            SaltMode saltMode,
            OutputFormat outputFormat)
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput); // Capture MD5 warnings

            try
            {
                var rule = new HashRule(algorithm, saltMode, outputFormat);

                // Act
                var result = rule.Apply("test-data");

                // Assert
                Assert.NotNull(result);
                Assert.NotEmpty(result);

                // Verify output format pattern
                switch (outputFormat)
                {
                    case OutputFormat.Hex:
                        Assert.Matches("^[a-f0-9]+$", result);
                        break;
                    case OutputFormat.Base64:
                        Assert.Matches("^[A-Za-z0-9+/]+=*$", result);
                        break;
                    case OutputFormat.Base64Url:
                        Assert.Matches("^[A-Za-z0-9_-]+$", result);
                        break;
                }
            }
            finally
            {
                Console.SetOut(Console.Out);
            }
        }

        #endregion

        #region Cryptographic Security Tests

        [Fact]
        public void Apply_DifferentInputs_ProduceDifferentHashes()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex);

            // Act
            var result1 = rule.Apply("input1");
            var result2 = rule.Apply("input2");

            // Assert
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void Apply_SimilarInputs_ProduceDifferentHashes()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex);

            // Act
            var result1 = rule.Apply("test");
            var result2 = rule.Apply("Test"); // Only case difference

            // Assert
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void Apply_PerRecordMode_GeneratesUniqueSalts()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerRecord, OutputFormat.Hex);

            // Act - Multiple applications should produce different hashes
            var results = Enumerable.Range(0, 10)
                .Select(_ => rule.Apply("same-input"))
                .ToList();

            // Assert - All results should be unique
            Assert.Equal(10, results.Distinct().Count());
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Apply_SHA256_MeetsPerformanceTarget()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex);
            var input = "performance-test-input";
            var iterations = 1000;

            // Warm-up
            for (int i = 0; i < 100; i++)
            {
                rule.Apply(input);
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                rule.Apply(input);
            }
            stopwatch.Stop();

            // Assert - Should achieve at least 50,000 ops/sec
            var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
            Assert.True(opsPerSecond >= 50_000,
                $"Performance target not met. Achieved: {opsPerSecond:N0} ops/sec, Target: 50,000 ops/sec");
        }

        [Fact]
        public void Apply_LongString_HandlesEfficiently()
        {
            // Arrange
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex);
            var input = new string('x', 100000); // 100KB string

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(64, result.Length); // Output length independent of input length
            Assert.Matches("^[a-f0-9]{64}$", result);
        }

        #endregion

        #region Interface Implementation Tests

        [Fact]
        public void HashRule_ImplementsIStringMaskRule()
        {
            // Arrange
            var rule = new HashRule();

            // Assert
            Assert.IsAssignableFrom<IStringMaskRule>(rule);
        }

        [Fact]
        public void Apply_ThroughIMaskRuleInterface_WorksCorrectly()
        {
            // Arrange
            IMaskRule rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex);

            // Act
            var result = rule.Apply("interface-test");

            // Assert
            Assert.Equal(64, result.Length);
        }

        #endregion

        #region Real-World Use Cases

        [Fact]
        public void Apply_EmailPseudonymization_ProducesDeterministicHash()
        {
            // Arrange
            var staticSalt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static, OutputFormat.Hex, staticSalt);

            // Act
            var hash1 = rule.Apply("user@example.com");
            var hash2 = rule.Apply("user@example.com");

            // Assert - For GDPR right to be forgotten, we need deterministic hashes to find all records
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void Apply_SSNMasking_ProducesNonDeterministicHash()
        {
            // Arrange - Maximum privacy for sensitive data
            var rule = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerRecord, OutputFormat.Hex);

            // Act
            var hash1 = rule.Apply("123-45-6789");
            var hash2 = rule.Apply("123-45-6789");

            // Assert - Each masking produces unique output
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Apply_FieldSpecificPseudonymization_PreventsCorrelation()
        {
            // Arrange
            var emailRule = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerField, OutputFormat.Hex, fieldName: "Email");
            var phoneRule = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerField, OutputFormat.Hex, fieldName: "Phone");

            // Act - Same user data in different fields
            var emailHash = emailRule.Apply("john.doe@example.com");
            var phoneHash = phoneRule.Apply("john.doe@example.com");

            // Assert - Cannot correlate across fields
            Assert.NotEqual(emailHash, phoneHash);
        }

        #endregion
    }
}
