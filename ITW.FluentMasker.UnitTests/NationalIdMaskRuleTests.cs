using ITW.FluentMasker.MaskRules;
using System;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for NationalIdMaskRule covering 100+ country formats
    /// </summary>
    public class NationalIdMaskRuleTests
    {
        #region Basic Functionality Tests

        [Fact]
        public void Apply_NullInput_ReturnsNull()
        {
            // Arrange
            var rule = new NationalIdMaskRule("US");

            // Act
            var result = rule.Apply(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Apply_EmptyString_ReturnsEmpty()
        {
            // Arrange
            var rule = new NationalIdMaskRule("US");

            // Act
            var result = rule.Apply("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Apply_InvalidFormat_ReturnsUnchanged()
        {
            // Arrange
            var rule = new NationalIdMaskRule("US");
            string input = "INVALID";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Apply_UnknownCountryCode_ReturnsUnchanged()
        {
            // Arrange
            var rule = new NationalIdMaskRule("ZZ"); // Non-existent country
            string input = "123456789";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        #endregion

        #region US (United States) Tests

        [Theory]
        [InlineData("123-45-6789", "***-**-6789")]     // Default: keepLast=4
        [InlineData("000-12-3456", "***-**-3456")]     // Edge case SSN
        [InlineData("999-99-9999", "***-**-9999")]     // Max SSN
        public void Apply_US_SSN_Formatted_MasksCorrectly(string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule("US");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("123456789", "*****6789")]         // Unformatted SSN
        [InlineData("987654321", "*****4321")]
        public void Apply_US_Unformatted_MasksCorrectly(string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule("US_UNFORMATTED");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("123-45-6789", 3, 2, "123-**-**89")]     // keepFirst=3 (1,2,3), keepLast=2 (8,9), so 4-5-6-7 are masked
        [InlineData("123-45-6789", 0, 6, "***-45-6789")]
        public void Apply_US_CustomKeepFirstLast_MasksCorrectly(string input, int keepFirst, int keepLast, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule("US", keepFirst: keepFirst, keepLast: keepLast);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region UK (United Kingdom) Tests

        [Theory]
        [InlineData("AB123456C", "AB******C")]         // Standard NINO
        [InlineData("ZZ123456D", "ZZ******D")]
        [InlineData("AA000000A", "AA******A")]
        public void Apply_UK_NINO_MasksCorrectly(string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule("UK");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Canada Tests

        [Theory]
        [InlineData("123-456-789", "***-***-789")]     // Standard SIN
        [InlineData("000-000-001", "***-***-001")]
        public void Apply_CA_SIN_MasksCorrectly(string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule("CA");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region EU Countries Tests

        [Theory]
        [InlineData("AT", "1234567890", "******7890")]           // Austria (SVNR)
        [InlineData("BE", "12345678901", "*******8901")]         // Belgium
        [InlineData("BG", "1234567890", "******7890")]           // Bulgaria (EGN)
        [InlineData("HR", "12345678901", "*******8901")]         // Croatia (OIB)
        [InlineData("CY", "12345678A", "******78A")]             // Cyprus - keepLast=3 means (7,8,A)
        [InlineData("DK", "123456-7890", "******-7890")]         // Denmark (CPR)
        [InlineData("EE", "12345678901", "*******8901")]         // Estonia
        [InlineData("FI", "121212-123A", "******-123A")]         // Finland (HETU)
        [InlineData("FR", "1234567890123", "*********0123")]     // France (NIR)
        [InlineData("DE", "12345678901", "*******8901")]         // Germany
        [InlineData("GR", "123456789", "******789")]             // Greece (AFM)
        [InlineData("HU", "1234567890", "******7890")]           // Hungary
        [InlineData("IE", "1234567A", "*****67A")]               // Ireland (PPSN) - keepLast=3 means last 3 alphanumeric (6,7,A)
        [InlineData("LV", "12345678901", "*******8901")]         // Latvia
        [InlineData("LT", "12345678901", "*******8901")]         // Lithuania
        [InlineData("LU", "1234567890123", "*********0123")]     // Luxembourg
        [InlineData("MT", "1234567A", "*****67A")]               // Malta - keepLast=3 means (6,7,A)
        [InlineData("NL", "123456789", "******789")]             // Netherlands (BSN)
        [InlineData("PL", "12345678901", "*******8901")]         // Poland (PESEL)
        [InlineData("PT", "123456789", "******789")]             // Portugal (NIF)
        [InlineData("RO", "1234567890123", "*********0123")]     // Romania (CNP)
        [InlineData("SI", "12345678", "*****678")]               // Slovenia
        [InlineData("ES", "12345678A", "******78A")]             // Spain (DNI) - keepLast=3 means (7,8,A)
        [InlineData("SE", "121212-1234", "******-1234")]         // Sweden
        public void Apply_EU_Countries_MasksCorrectly(string countryCode, string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("IT", "RSSMRA85T10A562S", "RSS**********62S")]  // Italy (Codice Fiscale) - keepFirst=3, keepLast=3, total 16 chars, 10 masked
        public void Apply_IT_CodiceFiscale_MasksCorrectly(string countryCode, string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("CZ", "1201010123", "******0123")]            // Czechia 10-digit format (no slash)
        [InlineData("SK", "1201010123", "******0123")]            // Slovakia (same format as CZ)
        public void Apply_CzechSlovak_RodneCislo_MasksCorrectly(string countryCode, string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Europe Non-EU Tests

        [Theory]
        [InlineData("CH", "756.1234.5678.90", "756.****.****.90")]    // Switzerland (formatted) - keepFirst=3, keepLast=2, dots preserved
        [InlineData("NO", "12345678901", "*******8901")]              // Norway
        [InlineData("IS", "123456-7890", "******-7890")]              // Iceland
        [InlineData("RU", "123-456-789 01", "***-***-*** 01")]       // Russia (SNILS formatted)
        [InlineData("RU_UNFORMATTED", "12345678901", "*********01")] // Russia unformatted
        [InlineData("TR", "12345678901", "*******8901")]              // Turkey (TCKN)
        [InlineData("UA", "1234567890", "******7890")]                // Ukraine (RNOKPP) - keepLast=4 in pattern
        public void Apply_EuropeNonEU_MasksCorrectly(string countryCode, string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Americas Tests

        [Theory]
        [InlineData("MX", "ABCD123456HMNLRR01", "ABCD************01")]  // Mexico (CURP) - keepFirst=4, keepLast=2 = 'ABCD' + '01'
        [InlineData("BR", "123.456.789-01", "***.***.*89-01")]         // Brazil (CPF formatted) - keepLast=4 means last 4 digits (8,9,0,1)
        [InlineData("BR_UNFORMATTED", "12345678901", "*******8901")]       // Brazil unformatted
        [InlineData("AR", "12345678", "*****678")]                         // Argentina (DNI)
        [InlineData("CL", "12.345.678-9", "**.***.*78-9")]                 // Chile (RUT formatted) - keepLast=3 means last 3 alphanumeric (7,8,9)
        [InlineData("CL_UNFORMATTED", "123456789", "******789")]           // Chile unformatted
        [InlineData("CO", "1234567890", "*******890")]                     // Colombia
        [InlineData("PE", "12345678", "*****678")]                         // Peru (DNI)
        [InlineData("UY", "1234567-8", "******7-8")]                       // Uruguay (CI) - keepLast=2 means last 2 digits (7,8)
        [InlineData("EC", "1234567890", "*******890")]                     // Ecuador
        [InlineData("BO", "123456", "***456")]                             // Bolivia (variable length)
        [InlineData("VE", "V-12345678", "*-*****678")]                     // Venezuela - 'V' is kept as keepFirst=0 doesn't apply to letter prefix
        public void Apply_Americas_MasksCorrectly(string countryCode, string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Asia-Pacific Tests

        [Theory]
        [InlineData("AU", "123456789", "******789")]                 // Australia (TFN)
        [InlineData("NZ", "12345678", "*****678")]                   // New Zealand (IRD)
        [InlineData("JP", "123456789012", "********9012")]           // Japan (My Number)
        [InlineData("CN", "11010119900307123X", "**************123X")] // China (Resident ID) - last 4 chars
        [InlineData("KR", "123456-1234567", "******-***4567")]       // South Korea (RRN)
        [InlineData("KR_UNFORMATTED", "1234561234567", "*********4567")] // Korea unformatted
        [InlineData("IN", "1234 5678 9012", "**** **** 9012")]       // India (Aadhaar)
        [InlineData("SG", "S1234567D", "S*******D")]                 // Singapore (NRIC) - keepFirst=1, keepLast=1
        [InlineData("HK", "A123456(7)", "A*****6(7)")]               // Hong Kong (HKID) - keepFirst=1, keepLast=2 = 'A' + '6' + '(7'
        [InlineData("TW", "A123456789", "A*******89")]               // Taiwan - keepFirst=1, keepLast=2 = 'A' + '89'
        [InlineData("MY", "123456-12-3456", "******-**-3456")]       // Malaysia (NRIC)
        [InlineData("MY_UNFORMATTED", "123456123456", "********3456")] // Malaysia unformatted
        [InlineData("TH", "1234567890123", "*********0123")]         // Thailand (CID)
        [InlineData("VN", "123456789012", "********9012")]           // Vietnam (12-digit)
        [InlineData("IDN", "1234567890123456", "************3456")]  // Indonesia (NIK)
        [InlineData("PH", "123-456-789-012", "***-***-**9-012")]     // Philippines (TIN) - keepLast=4 means last 4 digits (9, 0, 1, 2)
        public void Apply_AsiaPacific_MasksCorrectly(string countryCode, string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Middle East & North Africa Tests

        [Theory]
        [InlineData("IL", "123456789", "******789")]                 // Israel (Teudat Zehut)
        [InlineData("SA", "1234567890", "******7890")]               // Saudi Arabia (NIN) - keepLast=4
        [InlineData("AE", "784-1234-5678901-2", "784-****-*******-2")] // UAE (Emirates ID) - keepFirst=3 (784), keepLast=1 (2)
        [InlineData("EG", "12345678901234", "**********1234")]       // Egypt
        [InlineData("MA", "A12345", "A***45")]                       // Morocco (CIN) - keepFirst=1, keepLast=2
        [InlineData("PK", "12345-1234567-8", "*****-*****67-8")]     // Pakistan (CNIC) - keepLast=3 means last 3 alphanumeric (6,7,8)
        public void Apply_MiddleEastNorthAfrica_MasksCorrectly(string countryCode, string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Sub-Saharan Africa Tests

        [Theory]
        [InlineData("ZA", "1234567890123", "*********0123")]         // South Africa
        [InlineData("NG", "12345678901", "*******8901")]             // Nigeria (NIN)
        [InlineData("KE", "12345678", "*****678")]                   // Kenya
        [InlineData("GH", "GH123456789", "GH*******89")]             // Ghana (GhanaCard) - keepFirst=2, keepLast=2
        public void Apply_SubSaharanAfrica_MasksCorrectly(string countryCode, string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Auto-Detection Tests

        [Theory]
        [InlineData("123-45-6789", "***-**-6789")]          // Should detect US SSN
        [InlineData("AB123456C", "AB******C")]              // Should detect UK NINO
        [InlineData("123-456-789", "***-***-789")]          // Should detect CA SIN
        [InlineData("12345678901", "*******8901")]          // Could match multiple (DE, HR, etc.) - first match wins
        public void Apply_AutoDetect_MasksCorrectly(string input, string expected)
        {
            // Arrange - null country code triggers auto-detection
            var rule = new NationalIdMaskRule(countryCode: null);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_AutoDetect_NoMatch_ReturnsUnchanged()
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode: null);
            string input = "NOMATCH123XYZ";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        #endregion

        #region Custom Mask Character Tests

        [Theory]
        [InlineData("123-45-6789", "US", "#", "###-##-6789")]
        [InlineData("AB123456C", "UK", "X", "ABXXXXXXC")]      // UK NINO has keepFirst=2, keepLast=1, so 6 chars masked in middle with last 'C' visible
        [InlineData("123-456-789", "CA", "-", "--------789")]  // Canada SIN - dash as mask char makes it look weird but preserves sep
        public void Apply_CustomMaskChar_MasksCorrectly(string input, string countryCode, string maskChar, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode, maskChar: maskChar);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Separator Preservation Tests

        [Theory]
        [InlineData("123-45-6789", "***-**-6789")]          // Dashes preserved
        [InlineData("123456-7890", "******-7890")]          // Denmark format with dash
        [InlineData("756.1234.5678.90", "756.****.****.90")] // Switzerland with dots
        [InlineData("1234 5678 9012", "**** **** 9012")]    // India with spaces
        public void Apply_PreservesSeparators(string input, string expected)
        {
            // Arrange - Use appropriate country code or auto-detect
            var rule = new NationalIdMaskRule(countryCode: null);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Constructor Validation Tests

        [Fact]
        public void Constructor_NegativeKeepFirst_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new NationalIdMaskRule("US", keepFirst: -1));
            Assert.Equal("keepFirst", exception.ParamName);
        }

        [Fact]
        public void Constructor_NegativeKeepLast_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                new NationalIdMaskRule("US", keepLast: -1));
            Assert.Equal("keepLast", exception.ParamName);
        }

        [Fact]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            // Act
            var rule = new NationalIdMaskRule(
                countryCode: "US",
                keepFirst: 3,
                keepLast: 4,
                maskChar: "#");

            // Assert
            Assert.NotNull(rule);
        }

        #endregion

        #region Edge Cases

        [Theory]
        [InlineData("", "")]                     // Empty string
        [InlineData("   ", "   ")]               // Only spaces
        [InlineData("ABC", "ABC")]               // No digits in UK-like format but too short
        public void Apply_EdgeCases_HandlesCorrectly(string input, string expected)
        {
            // Arrange
            var rule = new NationalIdMaskRule("US");

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_KeepFirstAndLastExceedLength_ReturnsUnchanged()
        {
            // Arrange
            var rule = new NationalIdMaskRule("US", keepFirst: 10, keepLast: 10);
            string input = "123-45-6789";

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result); // Should return unchanged when keepFirst+keepLast >= alphanumeric count
        }

        [Theory]
        [InlineData("ES", "X1234567A", "******67A")]      // Spain NIE (foreigner) - keepFirst=0, keepLast=3
        [InlineData("ES", "12345678A", "******78A")]      // Spain DNI (citizen) - keepFirst=0, keepLast=3
        public void Apply_MultipleRegexPatterns_MasksCorrectly(string countryCode, string input, string expected)
        {
            // Arrange - Spain has two patterns (DNI and NIE)
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region IMaskRule Interface Tests

        [Fact]
        public void NationalIdMaskRule_ImplementsIStringMaskRule()
        {
            // Arrange
            var rule = new NationalIdMaskRule();

            // Act & Assert
            Assert.IsAssignableFrom<IStringMaskRule>(rule);
        }

        [Fact]
        public void NationalIdMaskRule_ImplementsIMaskRule()
        {
            // Arrange
            var rule = new NationalIdMaskRule();

            // Act & Assert
            Assert.IsAssignableFrom<IMaskRule>(rule);
        }

        [Fact]
        public void NationalIdMaskRule_ImplementsGenericIMaskRule()
        {
            // Arrange
            var rule = new NationalIdMaskRule();

            // Act & Assert
            Assert.IsAssignableFrom<IMaskRule<string, string>>(rule);
        }

        #endregion

        #region Real-World Examples

        [Theory]
        [InlineData("US", "123-45-6789", "***-**-6789", "US SSN")]
        [InlineData("UK", "AB123456C", "AB******C", "UK NINO")]
        [InlineData("CA", "123-456-789", "***-***-789", "Canada SIN")]
        [InlineData("DE", "12345678901", "*******8901", "Germany Tax ID")]
        [InlineData("FR", "1234567890123", "*********0123", "France NIR")]
        [InlineData("IT", "RSSMRA85T10A562S", "RSS**********62S", "Italy Codice Fiscale")]
        [InlineData("ES", "12345678A", "******78A", "Spain DNI")]
        [InlineData("BR", "123.456.789-01", "***.***.*89-01", "Brazil CPF")]
        [InlineData("CN", "11010119900307123X", "**************123X", "China Resident ID")]
        [InlineData("JP", "123456789012", "********9012", "Japan My Number")]
        [InlineData("AU", "123456789", "******789", "Australia TFN")]
        [InlineData("IN", "1234 5678 9012", "**** **** 9012", "India Aadhaar")]
        [InlineData("ZA", "1234567890123", "*********0123", "South Africa ID")]
        public void Apply_RealWorldExamples_MasksCorrectly(
            string countryCode, 
            string input, 
            string expected,
            string description)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Pattern-Specific Validation Tests

        [Theory]
        [InlineData("BE", "120101-123-45")]        // Belgium with valid format
        [InlineData("BE", "12010112345")]          // Belgium without separators
        public void Apply_Belgium_VariousFormats_MasksCorrectly(string countryCode, string input)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert - Should mask, preserving separators if present
            Assert.NotEqual(input, result); // Should be masked
            Assert.EndsWith("45", result);  // Last 4 chars (from last 2 groups) visible
        }

        [Theory]
        [InlineData("CL", "12.345.678-K")]         // Chile RUT with K check digit
        [InlineData("CL", "12.345.678-k")]         // lowercase k
        public void Apply_Chile_CheckDigitK_MasksCorrectly(string countryCode, string input)
        {
            // Arrange
            var rule = new NationalIdMaskRule(countryCode);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Contains("-K", result, StringComparison.OrdinalIgnoreCase);  // K should be visible (part of last 3 chars)
        }

        #endregion
    }
}
