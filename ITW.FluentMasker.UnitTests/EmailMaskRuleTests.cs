using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for EmailMaskRule
    /// </summary>
    public class EmailMaskRuleTests
    {
        #region Basic Email Masking Tests

        [Theory]
        [InlineData("user@example.com", 1, EmailDomainStrategy.KeepRoot, "*", "u***@example.com")]
        [InlineData("john@example.com", 2, EmailDomainStrategy.KeepRoot, "*", "jo**@example.com")]
        [InlineData("admin@example.com", 3, EmailDomainStrategy.KeepRoot, "*", "adm**@example.com")]
        [InlineData("a@example.com", 1, EmailDomainStrategy.KeepRoot, "*", "a@example.com")]  // localKeep >= length
        [InlineData("test@example.com", 0, EmailDomainStrategy.KeepRoot, "*", "****@example.com")]
        public void Apply_BasicEmails_KeepRoot_ReturnsExpectedOutput(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user@example.com", 1, EmailDomainStrategy.KeepFull, "*", "u***@example.com")]
        [InlineData("john@example.com", 2, EmailDomainStrategy.KeepFull, "*", "jo**@example.com")]
        [InlineData("admin@test.com", 1, EmailDomainStrategy.KeepFull, "*", "a****@test.com")]
        public void Apply_BasicEmails_KeepFull_ReturnsExpectedOutput(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user@example.com", 1, EmailDomainStrategy.MaskAll, "*", "u***@e******.c**")]
        [InlineData("john@test.com", 2, EmailDomainStrategy.MaskAll, "*", "jo**@t***.c**")]
        [InlineData("admin@x.co", 1, EmailDomainStrategy.MaskAll, "*", "a****@x.c*")]
        public void Apply_BasicEmails_MaskAll_ReturnsExpectedOutput(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Subdomain Handling Tests

        [Theory]
        [InlineData("user@mail.example.com", 1, EmailDomainStrategy.KeepRoot, "*", "u***@example.com")]
        [InlineData("admin@smtp.mail.example.com", 2, EmailDomainStrategy.KeepRoot, "*", "ad***@example.com")]
        [InlineData("test@sub1.sub2.sub3.example.com", 1, EmailDomainStrategy.KeepRoot, "*", "t***@example.com")]
        public void Apply_SubdomainEmails_KeepRoot_ExtractsRootDomain(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user@mail.example.com", 1, EmailDomainStrategy.KeepFull, "*", "u***@mail.example.com")]
        [InlineData("admin@smtp.mail.test.com", 2, EmailDomainStrategy.KeepFull, "*", "ad***@smtp.mail.test.com")]
        public void Apply_SubdomainEmails_KeepFull_PreservesFullDomain(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user@mail.example.com", 1, EmailDomainStrategy.MaskAll, "*", "u***@m***.e******.c**")]
        [InlineData("admin@sub.test.com", 1, EmailDomainStrategy.MaskAll, "*", "a****@s**.t***.c**")]
        public void Apply_SubdomainEmails_MaskAll_MasksAllDomainParts(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Plus Addressing Tests

        [Theory]
        [InlineData("user+newsletter@example.com", 1, EmailDomainStrategy.KeepRoot, "*", "u***+newsletter@example.com")]
        [InlineData("john+work@test.com", 2, EmailDomainStrategy.KeepRoot, "*", "jo**+work@test.com")]
        [InlineData("test+tag123@example.com", 1, EmailDomainStrategy.KeepRoot, "*", "t***+tag123@example.com")]
        [InlineData("admin+special@mail.example.com", 2, EmailDomainStrategy.KeepRoot, "*", "ad***+special@example.com")]
        public void Apply_PlusAddressing_PreservesTag(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user+tag@example.com", 10, EmailDomainStrategy.KeepRoot, "*", "user+tag@example.com")]  // localKeep > base length
        [InlineData("ab+tag@test.com", 5, EmailDomainStrategy.KeepRoot, "*", "ab+tag@test.com")]
        public void Apply_PlusAddressing_LargeLocalKeep_KeepsEntireLocal(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Edge Cases and Null/Empty Tests

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new EmailMaskRule(1, EmailDomainStrategy.KeepRoot, "*", true);

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmpty()
        {
            // Arrange
            var rule = new EmailMaskRule(1, EmailDomainStrategy.KeepRoot, "*", true);

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData("a@b.c", 1, EmailDomainStrategy.KeepRoot, "*", "a@b.c")]  // Shortest valid email
        [InlineData("x@y.z", 0, EmailDomainStrategy.KeepRoot, "*", "*@y.z")]
        public void Apply_ShortestValidEmail_HandlesCorrectly(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("verylongemailaddress12345@example.com", 1, EmailDomainStrategy.KeepRoot, "*", "v************************@example.com")]
        [InlineData("longlocalpart@verylongdomainname.com", 2, EmailDomainStrategy.KeepRoot, "*", "lo***********@verylongdomainname.com")]
        public void Apply_LongEmails_HandlesCorrectly(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Custom Mask Character Tests

        [Theory]
        [InlineData("user@example.com", 1, EmailDomainStrategy.KeepRoot, "#", "u###@example.com")]
        [InlineData("john@test.com", 2, EmailDomainStrategy.KeepRoot, "X", "joXX@test.com")]
        [InlineData("test@example.com", 1, EmailDomainStrategy.MaskAll, "-", "t---@e------.c--")]
        public void Apply_CustomMaskChar_UsesCustomCharacter(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Email Validation Tests

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("no-at-sign.com")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        [InlineData("user @example.com")]  // space in email
        [InlineData("user@domain")]  // no TLD
        public void Apply_InvalidEmailFormat_ValidateTrue_ThrowsFormatException(string invalidEmail)
        {
            // Arrange
            var rule = new EmailMaskRule(1, EmailDomainStrategy.KeepRoot, "*", validateFormat: true);

            // Act & Assert
            var exception = Assert.Throws<FormatException>(() => rule.Apply(invalidEmail));
            Assert.Contains("Invalid email format", exception.Message);
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("not-an-email")]
        public void Apply_InvalidEmailFormat_ValidateFalse_ProcessesWithoutValidation(string invalidEmail)
        {
            // Arrange
            var rule = new EmailMaskRule(1, EmailDomainStrategy.KeepRoot, "*", validateFormat: false);

            // Act
            var result = rule.Apply(invalidEmail);

            // Assert - Should return input unchanged since no @ found
            Assert.Equal(invalidEmail, result);
        }

        #endregion

        #region International Domain Names (IDN) Tests

        [Theory]
        [InlineData("user@münchen.de", 1, EmailDomainStrategy.KeepRoot, "*", "u***@münchen.de")]
        [InlineData("test@例え.jp", 2, EmailDomainStrategy.KeepRoot, "*", "te**@例え.jp")]
        [InlineData("admin@тест.рф", 1, EmailDomainStrategy.KeepFull, "*", "a****@тест.рф")]
        public void Apply_InternationalDomains_HandlesCorrectly(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("用户@example.com", 1, EmailDomainStrategy.KeepRoot, "*", "用*@example.com")]  // Chinese characters in local part
        [InlineData("josé@example.com", 2, EmailDomainStrategy.KeepRoot, "*", "jo**@example.com")]  // Accented characters
        public void Apply_InternationalLocalPart_HandlesCorrectly(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Constructor Validation Tests

        [Fact]
        public void Constructor_NegativeLocalKeep_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new EmailMaskRule(localKeep: -1));
            Assert.Equal("localKeep", exception.ParamName);
        }

        [Fact]
        public void Constructor_NullMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new EmailMaskRule(maskChar: null));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_EmptyMaskChar_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new EmailMaskRule(maskChar: ""));
            Assert.Equal("maskChar", exception.ParamName);
        }

        [Fact]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            // Act
            var rule = new EmailMaskRule(2, EmailDomainStrategy.KeepFull, "#", false);

            // Assert
            Assert.NotNull(rule);
        }

        #endregion

        #region Domain Part Edge Cases

        [Theory]
        [InlineData("user@x.com", 1, EmailDomainStrategy.MaskAll, "*", "u***@x.c**")]  // Single char domain part
        [InlineData("user@example.c", 1, EmailDomainStrategy.MaskAll, "*", "u***@e******.c")]  // Single char TLD
        public void Apply_SingleCharDomainParts_HandlesCorrectly(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user@example.com", 100, EmailDomainStrategy.KeepRoot, "*", "user@example.com")]  // localKeep way larger than local length
        public void Apply_LocalKeepExceedsLength_KeepsEntireLocalPart(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Multiple At Signs (Edge Case)

        [Fact]
        public void Apply_MultipleAtSigns_ValidateFalse_ProcessesFirstAt()
        {
            // Arrange - This is an invalid email format, but with validation off, it should process
            var rule = new EmailMaskRule(1, EmailDomainStrategy.KeepRoot, "*", validateFormat: false);
            var input = "user@domain@example.com";

            // Act
            var result = rule.Apply(input);

            // Assert - Should process using first @ sign
            Assert.Contains("@", result);
        }

        #endregion

        #region Special Characters in Local Part

        [Theory]
        [InlineData("user.name@example.com", 1, EmailDomainStrategy.KeepRoot, "*", "u********@example.com")]
        [InlineData("first.last@test.com", 2, EmailDomainStrategy.KeepRoot, "*", "fi********@test.com")]
        [InlineData("user_name@example.com", 1, EmailDomainStrategy.KeepRoot, "*", "u********@example.com")]
        public void Apply_SpecialCharsInLocalPart_HandlesCorrectly(
            string input, int localKeep, EmailDomainStrategy strategy, string maskChar, string expected)
        {
            // Arrange
            var rule = new EmailMaskRule(localKeep, strategy, maskChar, true);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
