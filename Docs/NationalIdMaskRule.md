# National ID Masking for Global Compliance

## Overview

The `NationalIdMaskRule` provides comprehensive masking for national identification numbers (SSN, Tax ID, etc.) across 100+ countries with format validation and automatic detection. It supports country-specific patterns with configurable masking while preserving separators and formatting.

## Features

- **100+ Country Support**: Covers EU member states, Americas, Asia-Pacific, Middle East, and Africa
- **Format Validation**: Built-in regex patterns for each country with checksum hints
- **Automatic Detection**: Can identify format without specifying country code
- **Separator Preservation**: Maintains dashes, dots, and spaces in original format
- **High Performance**: Uses `ArrayPool<char>` for efficient memory management
- **Graceful Degradation**: Returns unchanged input for invalid or unknown formats

## Supported Regions

### European Union (27 countries)
Austria (AT), Belgium (BE), Bulgaria (BG), Croatia (HR), Cyprus (CY), Czechia (CZ), Denmark (DK), Estonia (EE), Finland (FI), France (FR), Germany (DE), Greece (GR), Hungary (HU), Ireland (IE), Italy (IT), Latvia (LV), Lithuania (LT), Luxembourg (LU), Malta (MT), Netherlands (NL), Poland (PL), Portugal (PT), Romania (RO), Slovakia (SK), Slovenia (SI), Spain (ES), Sweden (SE)

### Europe (Non-EU)
Switzerland (CH), Norway (NO), Iceland (IS), Russia (RU), Türkiye (TR), Ukraine (UA), United Kingdom (UK)

### Americas
United States (US), Canada (CA), Mexico (MX), Brazil (BR), Argentina (AR), Chile (CL), Colombia (CO), Peru (PE), Uruguay (UY), Ecuador (EC), Bolivia (BO), Venezuela (VE)

### Asia-Pacific
Australia (AU), New Zealand (NZ), Japan (JP), China (CN), South Korea (KR), India (IN), Singapore (SG), Hong Kong (HK), Taiwan (TW), Malaysia (MY), Thailand (TH), Vietnam (VN), Indonesia (IDN), Philippines (PH)

### Middle East & North Africa
Israel (IL), Saudi Arabia (SA), United Arab Emirates (AE), Egypt (EG), Morocco (MA)

### Sub-Saharan Africa
South Africa (ZA), Nigeria (NG), Kenya (KE), Ghana (GH)

## Basic Usage

### US Social Security Number (Default)

```csharp
var rule = new NationalIdMaskRule("US");
var result = rule.Apply("123-45-6789");
// Result: "***-**-6789"
```

### UK National Insurance Number

```csharp
var rule = new NationalIdMaskRule("UK");
var result = rule.Apply("AB123456C");
// Result: "AB******C"
```

### Germany Tax ID

```csharp
var rule = new NationalIdMaskRule("DE");
var result = rule.Apply("12345678901");
// Result: "*******8901"
```

### Auto-Detection (No Country Code)

```csharp
var rule = new NationalIdMaskRule();
var result = rule.Apply("123-45-6789");
// Result: "***-**-6789" (auto-detected as US SSN)
```

## Custom Masking Options

### Override Visibility

```csharp
// Keep first 3 and last 3 characters visible
var rule = new NationalIdMaskRule("DE", keepFirst: 3, keepLast: 3);
var result = rule.Apply("12345678901");
// Result: "123*****901"
```

### Custom Mask Character

```csharp
var rule = new NationalIdMaskRule("US", maskChar: "X");
var result = rule.Apply("123-45-6789");
// Result: "XXX-XX-6789"
```

## Country-Specific Examples

### European Union

```csharp
// Italy - Codice Fiscale (complex alphanumeric)
var italyRule = new NationalIdMaskRule("IT");
italyRule.Apply("RSSMRA85T10A562S");
// Result: "RSS***********62S"

// France - NIR (Numéro d'Inscription au Répertoire)
var franceRule = new NationalIdMaskRule("FR");
franceRule.Apply("2850512345678 90");
// Result: "**********5678 90"

// Netherlands - BSN (Burgerservicenummer)
var netherlandsRule = new NationalIdMaskRule("NL");
netherlandsRule.Apply("123456782");
// Result: "******782"

// Sweden - Personnummer
var swedenRule = new NationalIdMaskRule("SE");
swedenRule.Apply("811218-9876");
// Result: "******-9876"
```

### Americas

```csharp
// Canada - Social Insurance Number
var canadaRule = new NationalIdMaskRule("CA");
canadaRule.Apply("123-456-789");
// Result: "***-***-789"

// Brazil - CPF (formatted)
var brazilRule = new NationalIdMaskRule("BR");
brazilRule.Apply("123.456.789-01");
// Result: "***.***.***-01"

// Mexico - CURP
var mexicoRule = new NationalIdMaskRule("MX");
mexicoRule.Apply("ABCD123456HDFRRL09");
// Result: "ABCD**************09"

// Chile - RUT
var chileRule = new NationalIdMaskRule("CL");
chileRule.Apply("12.345.678-K");
// Result: "**.***.**8-K"
```

### Asia-Pacific

```csharp
// Japan - My Number
var japanRule = new NationalIdMaskRule("JP");
japanRule.Apply("123456789012");
// Result: "********9012"

// China - Resident Identity Card
var chinaRule = new NationalIdMaskRule("CN");
chinaRule.Apply("110101199003074593");
// Result: "**************4593"

// Singapore - NRIC
var singaporeRule = new NationalIdMaskRule("SG");
singaporeRule.Apply("S1234567D");
// Result: "S*******D"

// India - Aadhaar
var indiaRule = new NationalIdMaskRule("IN");
indiaRule.Apply("1234 5678 9012");
// Result: "**** **** 9012"

// South Korea - RRN
var koreaRule = new NationalIdMaskRule("KR");
koreaRule.Apply("123456-1234567");
// Result: "******-***4567"
```

### Middle East & Africa

```csharp
// Israel - Teudat Zehut
var israelRule = new NationalIdMaskRule("IL");
israelRule.Apply("123456789");
// Result: "******789"

// Saudi Arabia - National ID
var saudiRule = new NationalIdMaskRule("SA");
saudiRule.Apply("1234567890");
// Result: "******7890"

// South Africa - ID Number
var southAfricaRule = new NationalIdMaskRule("ZA");
southAfricaRule.Apply("8001015009087");
// Result: "*********9087"

// UAE - Emirates ID
var uaeRule = new NationalIdMaskRule("AE");
uaeRule.Apply("784-1234-1234567-8");
// Result: "784-****-*******-8"
```

## Fluent API Usage

### With AbstractMasker

```csharp
using ITW.FluentMasker;
using ITW.FluentMasker.Extensions;

public class PersonData
{
    public string SSN { get; set; }
    public string TaxId { get; set; }
    public string PassportNumber { get; set; }
}

public class PersonMasker : AbstractMasker<PersonData>
{
    protected override void Initialize()
    {
        // US Social Security Number
        MaskFor(x => x.SSN, m => m.NationalIdMask("US"));

        // UK National Insurance Number
        MaskFor(x => x.TaxId, m => m.NationalIdMask("UK"));

        // Auto-detect format
        MaskFor(x => x.PassportNumber, m => m.NationalIdMask());
    }
}

// Usage
var masker = new PersonMasker();
var person = new PersonData
{
    SSN = "123-45-6789",
    TaxId = "AB123456C",
    PassportNumber = "12345678901"
};

var masked = masker.Mask(person);
// SSN: "***-**-6789"
// TaxId: "AB******C"
// PassportNumber: masked based on detected format
```

### Custom Masking Configuration

```csharp
public class ComplianceMasker : AbstractMasker<EmployeeData>
{
    protected override void Initialize()
    {
        // Show more characters for internal use
        MaskFor(x => x.SSN, m => m.NationalIdMask("US", keepFirst: 3, keepLast: 4));

        // Custom mask character
        MaskFor(x => x.TaxId, m => m.NationalIdMask("DE", maskChar: "#"));

        // Minimal visibility for external reports
        MaskFor(x => x.NationalId, m => m.NationalIdMask("FR", keepFirst: 0, keepLast: 2));
    }
}
```

## Format Details by Country

### United States (US)
- **Format**: `123-45-6789` or `123456789` (unformatted)
- **Pattern**: 9 digits with optional dashes
- **Validation**: Format only (no checksum)
- **Default Masking**: Keep last 4 digits
- **Example**: `123-45-6789` ? `***-**-6789`

### United Kingdom (UK)
- **Format**: `AB123456C` (NINO)
- **Pattern**: 2 letters + 6 digits + 1 letter
- **Validation**: Excludes certain letter combinations
- **Default Masking**: Keep first 2, last 1
- **Example**: `AB123456C` ? `AB******C`

### Germany (DE)
- **Format**: `12345678901` (Steuer-IdNr)
- **Pattern**: 11 digits
- **Validation**: ISO 7064 mod 11,10 checksum
- **Default Masking**: Keep last 4 digits
- **Example**: `12345678901` ? `*******8901`

### France (FR)
- **Format**: `2850512345678 90` (NIR)
- **Pattern**: 13-15 digits with optional space
- **Validation**: Mod 97 checksum
- **Default Masking**: Keep last 4 digits
- **Example**: `2850512345678 90` ? `**********5678 90`

### Canada (CA)
- **Format**: `123-456-789` (SIN)
- **Pattern**: 9 digits with dashes
- **Validation**: Luhn mod 10 checksum
- **Default Masking**: Keep last 3 digits
- **Example**: `123-456-789` ? `***-***-789`

### Brazil (BR)
- **Format**: `123.456.789-01` (CPF)
- **Pattern**: 11 digits with dots and dash
- **Validation**: Two mod 11 check digits
- **Default Masking**: Keep last 4 digits
- **Example**: `123.456.789-01` ? `***.***.***-01`

### China (CN)
- **Format**: `110101199003074593` (18 digits)
- **Pattern**: 17 digits + 1 check character (X allowed)
- **Validation**: Weighted sum mod 11
- **Default Masking**: Keep last 4 digits
- **Example**: `110101199003074593` ? `**************4593`

### Japan (JP)
- **Format**: `123456789012` (My Number)
- **Pattern**: 12 digits
- **Validation**: Mod 11 checksum
- **Default Masking**: Keep last 4 digits
- **Example**: `123456789012` ? `********9012`

## Checksum Information

Many national ID formats include checksums for validation. The `NationalIdMaskRule` includes hints about checksum algorithms in the code comments, though it primarily focuses on format validation and masking:

### Countries with Checksums
- **Luhn Algorithm**: CA, SA, ZA, IL (mod 10)
- **Mod 11**: DE, HR, NO, IS, PT, NL, LT, BR, CL, JP, CN, TR, KR, TH
- **Mod 97**: BE, FR
- **Verhoeff**: IN (Aadhaar)
- **Custom**: SE (Luhn on 10 digits), FI (mod 31), ES (mod 23), IT (mod 26)

### Date of Birth Encoding
Several formats encode date of birth:
- **YYMMDD**: BE, CZ, DK, SK, SE, KR, MY
- **DDMMYY**: FI, IS
- **YYYYMMDD**: LU, CN, EG, IDN, ZA

## Performance

The `NationalIdMaskRule` is optimized for high performance:

- **ArrayPool Usage**: Efficient memory allocation for string manipulation
- **Regex Timeout**: 100ms timeout per pattern to prevent ReDoS attacks
- **Early Exit**: Returns immediately for null/empty input
- **Separator Preservation**: Maintains original formatting without rebuilding

## Error Handling

The rule is designed for graceful degradation:

```csharp
var rule = new NationalIdMaskRule("US");

// Invalid format returns unchanged
rule.Apply("INVALID");        // Returns "INVALID"

// Empty/null returns unchanged
rule.Apply("");              // Returns ""
rule.Apply(null);            // Returns null

// Unknown country code returns unchanged
var unknownRule = new NationalIdMaskRule("XX");
unknownRule.Apply("123456");  // Returns "123456"

// Format doesn't match country pattern
rule.Apply("12-345-6789");   // Returns "12-345-6789" (wrong format)
```

## Unformatted Variants

Several countries have unformatted variants for flexibility:

```csharp
// US unformatted
var usRule = new NationalIdMaskRule("US_UNFORMATTED");
usRule.Apply("123456789");
// Result: "*****6789"

// Brazil unformatted
var brRule = new NationalIdMaskRule("BR_UNFORMATTED");
brRule.Apply("12345678901");
// Result: "*******8901"

// Chile unformatted
var clRule = new NationalIdMaskRule("CL_UNFORMATTED");
clRule.Apply("12345678K");
// Result: "******78K"

// Switzerland unformatted
var chRule = new NationalIdMaskRule("CH_UNFORMATTED");
chRule.Apply("75612345678901");
// Result: "756**********01"

// Korea unformatted
var krRule = new NationalIdMaskRule("KR_UNFORMATTED");
krRule.Apply("1234561234567");
// Result: "*********4567"
```

## Best Practices

### 1. Use Country-Specific Rules

Always specify the country code when known:

```csharp
// Good - Explicit country
MaskFor(x => x.SSN, m => m.NationalIdMask("US"));

// Avoid - Auto-detection when country is known
MaskFor(x => x.SSN, m => m.NationalIdMask());
```

### 2. Validate Before Masking

Consider pre-validation for critical applications:

```csharp
public class SecureMasker : AbstractMasker<SecureData>
{
    protected override void Initialize()
    {
        MaskFor(x => x.SSN, m => m.NationalIdMask("US"));
    }

    public override MaskingResult<SecureData> Mask(SecureData data)
    {
        // Pre-validation
        if (!IsValidSSNFormat(data.SSN))
        {
            throw new ArgumentException("Invalid SSN format");
        }

        return base.Mask(data);
    }
}
```

### 3. Balance Visibility and Privacy

Adjust `keepFirst` and `keepLast` based on use case:

```csharp
// Maximum privacy (external reports)
MaskFor(x => x.SSN, m => m.NationalIdMask("US", keepFirst: 0, keepLast: 0));
// Result: "***-**-****"

// Moderate privacy (internal use)
MaskFor(x => x.SSN, m => m.NationalIdMask("US", keepFirst: 0, keepLast: 4));
// Result: "***-**-6789" (default)

// More visibility (debugging/support)
MaskFor(x => x.SSN, m => m.NationalIdMask("US", keepFirst: 3, keepLast: 4));
// Result: "123-**-6789"
```

### 4. Handle Multiple Formats

For international applications, consider supporting multiple formats:

```csharp
public class MultiCountryMasker : AbstractMasker<GlobalEmployee>
{
    private readonly string _country;

    public MultiCountryMasker(string countryCode)
    {
        _country = countryCode;
    }

    protected override void Initialize()
    {
        MaskFor(x => x.NationalId, m => m.NationalIdMask(_country));
    }
}

// Usage for different countries
var usMasker = new MultiCountryMasker("US");
var ukMasker = new MultiCountryMasker("UK");
var deMasker = new MultiCountryMasker("DE");
```

### 5. Test Edge Cases

Ensure proper handling of variations:

```csharp
var rule = new NationalIdMaskRule("US");

// Test formatted
Assert.Equal("***-**-6789", rule.Apply("123-45-6789"));

// Test unformatted (might not match US pattern)
var unformattedRule = new NationalIdMaskRule("US_UNFORMATTED");
Assert.Equal("*****6789", unformattedRule.Apply("123456789"));

// Test invalid
Assert.Equal("INVALID", rule.Apply("INVALID"));
```

## Real-World Healthcare Scenario

```csharp
public class PatientRecord
{
    public string PatientId { get; set; }
    public string Name { get; set; }
    public string SSN { get; set; }
    public string TaxId { get; set; }
    public string MedicareNumber { get; set; }
}

public class HealthcareMasker : AbstractMasker<PatientRecord>
{
    protected override void Initialize()
    {
        // Mask name
        MaskFor(x => x.Name, m => m.Redact());

        // Mask SSN (HIPAA compliance)
        MaskFor(x => x.SSN, m => m.NationalIdMask("US"));

        // Mask Tax ID
        MaskFor(x => x.TaxId, m => m.NationalIdMask("US"));

        // Mask Medicare number (if follows SSN pattern)
        MaskFor(x => x.MedicareNumber, m => m.NationalIdMask());
    }
}

// Usage
var masker = new HealthcareMasker();
var patient = new PatientRecord
{
    PatientId = "patient-12345",
    Name = "John Doe",
    SSN = "123-45-6789",
    TaxId = "987-65-4321",
    MedicareNumber = "111-22-3333A"
};

var masked = masker.Mask(patient);
// Name: "[REDACTED]"
// SSN: "***-**-6789"
// TaxId: "***-**-4321"
// MedicareNumber: masked based on detected format
```

## Extension Method

The fluent API extension makes it easy to use with the builder pattern:

```csharp
using ITW.FluentMasker.Extensions;

// In your masker class
MaskFor(x => x.SSN, m => m.NationalIdMask("US"));

// With custom options
MaskFor(x => x.TaxId, m => m.NationalIdMask("UK", keepFirst: 2, keepLast: 2));

// With custom mask character
MaskFor(x => x.NationalId, m => m.NationalIdMask("DE", maskChar: "#"));

// Auto-detect format
MaskFor(x => x.UnknownId, m => m.NationalIdMask());
```

## Security Considerations

### 1. ReDoS Protection
All regex patterns have 100ms timeout to prevent Regular Expression Denial of Service attacks.

### 2. No Validation Bypass
Invalid formats are returned unchanged rather than attempting to "fix" them.

### 3. Separator Preservation
Original formatting is maintained to avoid information leakage through format changes.

### 4. Consistent Masking
Same input always produces same output (deterministic masking).

## Compliance

### GDPR
- Minimizes personal data exposure
- Supports right to erasure through redaction
- Maintains data utility for analytics
- Format-aware to prevent re-identification

### HIPAA
- Masks PHI (Protected Health Information)
- Suitable for Safe Harbor de-identification
- Preserves format for operational needs
- Supports audit logging (input format preserved)

### PCI-DSS
- While not for payment cards, follows similar principles
- Minimum necessary exposure
- Consistent masking approach

## See Also

- [FluentMasker GitHub Repository](https://github.com/UlrikAtItWrk/FluentMasker)
- [DateAgeMaskRule Documentation](DateAgeMaskRule.md)
- [GDPR Personal Data Guidelines](https://gdpr-info.eu/)
- [ISO 3166-1 Country Codes](https://en.wikipedia.org/wiki/ISO_3166-1_alpha-2)
