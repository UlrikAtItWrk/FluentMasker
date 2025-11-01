using System;
using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for TemplateMaskRule
    /// </summary>
    public class TemplateMaskRuleTests
    {
        #region Acceptance Criteria Tests

        [Fact]
        public void Apply_AcceptanceCriteria1_FirstLastWithMask()
        {
            // Arrange - "{{F}}{{*x6}}{{L}}" + "JohnDoe" → "J******e"
            var rule = new TemplateMaskRule("{{F}}{{*x6}}{{L}}");
            var input = "JohnDoe";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("J******e", result);
        }

        [Fact]
        public void Apply_AcceptanceCriteria2_FirstTwoWithMask()
        {
            // Arrange - "{{F|2}}{{*x10}}" + "SarahJohnson" → "Sa**********"
            var rule = new TemplateMaskRule("{{F|2}}{{*x10}}");
            var input = "SarahJohnson";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Sa**********", result);
        }

        [Fact]
        public void Apply_AcceptanceCriteria3_PhoneNumberWithDigitExtraction()
        {
            // Arrange - "+{{digits|0-2}} ** ** {{digits|-2}}" + "+45 12 34 56 78" → "+45 ** ** 78"
            var rule = new TemplateMaskRule("+{{digits|0-2}} ** ** {{digits|-2}}");
            var input = "+45 12 34 56 78";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("+45 ** ** 78", result);
        }

        [Fact]
        public void Apply_UnknownToken_LeftUnchanged()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{F}} - {{unknown}} - {{L}}");
            var input = "Test";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("T - {{unknown}} - t", result);
        }

        #endregion

        #region First Token Tests

        [Fact]
        public void Apply_FirstToken_DefaultCount()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{F}}");
            var input = "Hello";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("H", result);
        }

        [Fact]
        public void Apply_FirstToken_CustomCount()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{F|3}}");
            var input = "HelloWorld";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hel", result);
        }

        [Fact]
        public void Apply_FirstToken_CountExceedsLength()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{F|10}}");
            var input = "Hi";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hi", result);
        }

        #endregion

        #region Last Token Tests

        [Fact]
        public void Apply_LastToken_DefaultCount()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{L}}");
            var input = "Hello";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("o", result);
        }

        [Fact]
        public void Apply_LastToken_CustomCount()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{L|3}}");
            var input = "HelloWorld";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("rld", result);
        }

        [Fact]
        public void Apply_LastToken_CountExceedsLength()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{L|10}}");
            var input = "Hi";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hi", result);
        }

        #endregion

        #region Mask Token Tests

        [Fact]
        public void Apply_MaskToken_VariousCounts()
        {
            // Arrange & Act & Assert
            Assert.Equal("*****", new TemplateMaskRule("{{*x5}}").Apply("anything"));
            Assert.Equal("**********", new TemplateMaskRule("{{*x10}}").Apply("test"));
            Assert.Equal("*", new TemplateMaskRule("{{*x1}}").Apply("x"));
        }

        [Fact]
        public void Apply_MaskToken_Zero()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{*x0}}");
            var input = "test";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("", result);
        }

        #endregion

        #region Digits Token Tests

        [Fact]
        public void Apply_DigitsToken_NoRange()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{digits}}");
            var input = "ABC123DEF456";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("123456", result);
        }

        [Fact]
        public void Apply_DigitsToken_WithRange()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{digits|0-3}}");
            var input = "Phone: 123-456-7890";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("123", result);
        }

        [Fact]
        public void Apply_DigitsToken_LastTwo()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{digits|-2}}");
            var input = "+45 12 34 56 78";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("78", result);
        }

        [Fact]
        public void Apply_DigitsToken_NoDigitsInInput()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{digits}}");
            var input = "NoDigitsHere";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("", result);
        }

        #endregion

        #region Letters Token Tests

        [Fact]
        public void Apply_LettersToken_NoRange()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{letters}}");
            var input = "ABC123DEF456";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("ABCDEF", result);
        }

        [Fact]
        public void Apply_LettersToken_WithRange()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{letters|0-3}}");
            var input = "Hello123World";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Hel", result);
        }

        [Fact]
        public void Apply_LettersToken_LastThree()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{letters|-3}}");
            var input = "Test123End";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("End", result);
        }

        [Fact]
        public void Apply_LettersToken_NoLettersInInput()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{letters}}");
            var input = "123456";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("", result);
        }

        #endregion

        #region Complex Template Tests

        [Fact]
        public void Apply_ComplexTemplate_CreditCard()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{digits|0-4}} {{*x4}} {{*x4}} {{digits|-4}}");
            var input = "1234-5678-9012-3456";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("1234 **** **** 3456", result);
        }

        [Fact]
        public void Apply_ComplexTemplate_NameMasking()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{letters|0-1}}{{*x8}}");
            var input = "JohnSmith123";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("J********", result);
        }

        [Fact]
        public void Apply_ComplexTemplate_MultipleTokens()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{F|2}}-{{*x5}}-{{L|2}}");
            var input = "TestString";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("Te-*****-ng", result);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void Apply_EmptyInput_ReturnsEmpty()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{F}}{{*x5}}{{L}}");
            var input = "";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{F}}{{*x5}}{{L}}");
            string input = null;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_TemplateWithoutTokens_ReturnsTemplateAsIs()
        {
            // Arrange
            var rule = new TemplateMaskRule("No tokens here");
            var input = "TestInput";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("No tokens here", result);
        }

        [Fact]
        public void Constructor_NullTemplate_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TemplateMaskRule(null));
        }

        [Fact]
        public void Apply_SingleCharacterInput_HandlesCorrectly()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{F}}{{*x3}}{{L}}");
            var input = "X";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("X***X", result);
        }

        [Fact]
        public void Apply_RangeExceedingLength_HandlesGracefully()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{digits|0-100}}");
            var input = "123";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal("123", result);
        }

        #endregion

        #region Consistency Tests

        [Fact]
        public void Apply_MultipleApplications_ProducesSameResult()
        {
            // Arrange
            var rule = new TemplateMaskRule("{{F|2}}{{*x5}}{{L|2}}");
            var input = "TestString";

            // Act
            var result1 = rule.Apply(input);
            var result2 = rule.Apply(input);
            var result3 = rule.Apply(input);

            // Assert
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
        }

        #endregion
    }
}
