# FluentMasker - Complete Mask Rules Reference

## Overview

FluentMasker provides **30+ mask rules** covering position-based masking, format-specific masking, cryptographic operations, and compliance requirements. All rules implement the `IMaskRule<TInput, TOutput>` interface and are optimized for performance using `ArrayPool<char>` and compiled expressions.

## Table of Contents

- [Position-Based Rules](#position-based-rules)
- [Format-Specific Rules](#format-specific-rules)
- [Cryptographic & Statistical Rules](#cryptographic--statistical-rules)
- [Date & Time Rules](#date--time-rules)
- [Pattern-Based Rules](#pattern-based-rules)
- [Character Class Rules](#character-class-rules)
- [Collection Rules](#collection-rules)
- [Quick Reference Table](#quick-reference-table)

---

## Position-Based Rules

### MaskStartRule
Masks the first N characters of a string.

**Parameters:**
- `count` (int): Number of characters to mask from the start
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
var rule = new MaskStartRule(3, "*");
rule.Apply("JohnDoe");
// Result: "***nDoe"
```

**Use Cases:**
- First name masking
- Prefix hiding
- Partial name redaction

**Status:** ✅ Implemented

---

### MaskEndRule
Masks the last N characters of a string.

**Parameters:**
- `count` (int): Number of characters to mask from the end
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
var rule = new MaskEndRule(3, "*");
rule.Apply("JohnDoe");
// Result: "John***"
```

**Use Cases:**
- Last name masking
- Suffix hiding
- Partial account numbers

**Status:** ✅ Implemented

---

### MaskFirstRule
**DEPRECATED** - Use `MaskStartRule` instead.

**Status:** ⚠️ Deprecated (maintained for backward compatibility)

---

### MaskLastRule
**DEPRECATED** - Use `MaskEndRule` instead.

**Status:** ⚠️ Deprecated (maintained for backward compatibility)

---

### MaskMiddleRule
Masks the middle portion of a string while keeping the first and last N characters visible.

**Parameters:**
- `keepFirst` (int): Number of characters to keep at the start
- `keepLast` (int): Number of characters to keep at the end
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
var rule = new MaskMiddleRule(keepFirst: 3, keepLast: 3, "*");
rule.Apply("john.doe@example.com");
// Result: "joh**********com"
```

**Use Cases:**
- Email local part masking
- Long identifier masking
- Preserving string structure while hiding content

**Status:** ✅ Implemented

---

### MaskRangeRule
Masks characters within a specific position range.

**Parameters:**
- `startIndex` (int): Starting position (0-based)
- `length` (int): Number of characters to mask
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
var rule = new MaskRangeRule(startIndex: 3, length: 5, "*");
rule.Apply("1234567890");
// Result: "123*****90"
```

**Use Cases:**
- Masking specific positions in IDs
- Partial SSN masking (middle section)
- Custom position-based redaction

**Status:** ✅ Implemented

---

### MaskPercentageRule
Masks a percentage of the string's characters.

**Parameters:**
- `percentage` (double): Percentage of characters to mask (0.0 - 1.0)
- `maskChar` (string): Character to use for masking (default: `"*"`)
- `fromStart` (bool): Whether to mask from start (true) or end (false) (default: true)

**Example:**
```csharp
var rule = new MaskPercentageRule(percentage: 0.5, maskChar: "*");
rule.Apply("JohnDoe1234");
// Result: "******1234" (50% masked)
```

**Use Cases:**
- Proportional data hiding
- Adaptive masking based on string length
- Progressive disclosure controls

**Status:** ✅ Implemented

---

### KeepFirstRule
Shows only the first N characters, masks the rest.

**Parameters:**
- `count` (int): Number of characters to keep visible
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
var rule = new KeepFirstRule(3, "*");
rule.Apply("JohnDoe");
// Result: "Joh****"
```

**Use Cases:**
- Name prefixes
- Account number prefixes
- BIN display (first 6 digits of card)

**Status:** ✅ Implemented

---

### KeepLastRule
Shows only the last N characters, masks the rest.

**Parameters:**
- `count` (int): Number of characters to keep visible
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
var rule = new KeepLastRule(4, "*");
rule.Apply("4532123456789012");
// Result: "************9012"
```

**Use Cases:**
- PCI-DSS compliant card number display
- Last 4 digits of SSN
- Account verification

**Status:** ✅ Implemented

---

### RedactRule
Completely replaces the input with a redaction placeholder.

**Parameters:**
- `redactionText` (string): The text to replace input with (default: `"[REDACTED]"`)

**Example:**
```csharp
var rule = new RedactRule("[CLASSIFIED]");
rule.Apply("TopSecretData");
// Result: "[CLASSIFIED]"
```

**Use Cases:**
- Complete data removal
- Standardized redaction markers
- HIPAA/GDPR erasure requests

**Status:** ✅ Implemented

---

### TruncateRule
Truncates strings to a maximum length with an optional suffix.

**Parameters:**
- `maxLength` (int): Maximum length of output string
- `suffix` (string): Suffix to append when truncated (default: `"..."`)

**Example:**
```csharp
var rule = new TruncateRule(maxLength: 10, suffix: "...");
rule.Apply("This is a very long string");
// Result: "This is..."
```

**Use Cases:**
- Log message length limiting
- UI display constraints
- Data export size reduction

**Status:** ✅ Implemented

---

### NullOutRule
Replaces any input with null.

**Parameters:** None

**Example:**
```csharp
var rule = new NullOutRule();
rule.Apply("SensitiveData");
// Result: null
```

**Use Cases:**
- GDPR right to erasure
- Complete data removal
- Null-safe field clearing

**Status:** ✅ Implemented

---

## Format-Specific Rules

### EmailMaskRule
Masks email addresses while preserving the domain structure.

**Parameters:**
- `localKeep` (int): Number of characters to keep in local part (default: 2)
- `domainStrategy` (EmailDomainStrategy): How to handle domain (default: `KeepFull`)
- `maskChar` (string): Character to use for masking (default: `"*"`)

**EmailDomainStrategy Options:**
- `KeepFull`: Keep full domain (`user@example.com` → `us**@example.com`)
- `KeepRoot`: Keep only root domain (`user@mail.example.com` → `us**@example.com`)
- `MaskDomain`: Mask domain too (`user@example.com` → `us**@*****.**`)

**Example:**
```csharp
var rule = new EmailMaskRule(localKeep: 2, domainStrategy: EmailDomainStrategy.KeepFull);
rule.Apply("john.doe@example.com");
// Result: "jo**@example.com"
```

**Use Cases:**
- GDPR-compliant email logging
- Email display in UIs
- Analytics with user privacy

**Status:** ✅ Implemented

---

### PhoneMaskRule
Masks phone numbers while optionally preserving separators and format.

**Parameters:**
- `keepLast` (int): Number of digits to keep visible at the end (default: 4)
- `preserveSeparators` (bool): Whether to maintain separators like `-`, `()`, spaces (default: true)
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
var rule = new PhoneMaskRule(keepLast: 4, preserveSeparators: true);
rule.Apply("+1 (555) 123-4567");
// Result: "+* (***) ***-4567"
```

**Use Cases:**
- Contact information masking
- Call logs
- Customer service displays

**Status:** ✅ Implemented

---

### CardMaskRule
Masks payment card numbers following PCI-DSS requirements.

**Parameters:**
- `keepFirst` (int): Number of digits to keep at start (default: 6 for BIN)
- `keepLast` (int): Number of digits to keep at end (default: 4)
- `preserveGrouping` (bool): Whether to maintain spacing/dashes (default: true)
- `maskChar` (string): Character to use for masking (default: `"*"`)
- `validateLuhn` (bool): Whether to validate Luhn checksum (default: false)

**Example:**
```csharp
// PCI-DSS compliant display (first 6 + last 4)
var rule = new CardMaskRule(keepFirst: 6, keepLast: 4);
rule.Apply("4532-1234-5678-9012");
// Result: "4532-12**-****-9012"

// Logs (last 4 only)
var logRule = new CardMaskRule(keepFirst: 0, keepLast: 4);
logRule.Apply("4532123456789012");
// Result: "************9012"
```

**Use Cases:**
- PCI-DSS compliance
- Payment transaction logs
- Receipt displays

**Status:** ✅ Implemented

---

### IBANMaskRule
Masks International Bank Account Numbers.

**Parameters:**
- `keepCountryCode` (bool): Whether to keep the 2-letter country code visible (default: true)
- `keepCheckDigits` (bool): Whether to keep check digits visible (default: true)
- `keepLast` (int): Number of characters to keep at end (default: 4)
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
var rule = new IBANMaskRule(keepCountryCode: true, keepLast: 4);
rule.Apply("GB82 WEST 1234 5698 7654 32");
// Result: "GB** **** **** **** **** 32"
```

**Use Cases:**
- Banking transaction logs
- Payment method display
- European financial compliance

**Status:** ✅ Implemented

---

### NationalIdMaskRule
Masks national identification numbers for 100+ countries with format detection.

**Parameters:**
- `countryCode` (string): ISO 3166-1 alpha-2 country code (e.g., "US", "UK", "DE") or empty for auto-detection
- `keepFirst` (int): Override number of characters to keep at start (default varies by country)
- `keepLast` (int): Override number of characters to keep at end (default varies by country)
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Supported Countries:** US, UK, CA, DE, FR, IT, ES, and 100+ more (see [NationalIdMaskRule.md](./NationalIdMaskRule.md))

**Example:**
```csharp
// US Social Security Number
var usRule = new NationalIdMaskRule("US");
usRule.Apply("123-45-6789");
// Result: "***-**-6789"

// UK National Insurance Number
var ukRule = new NationalIdMaskRule("UK");
ukRule.Apply("AB123456C");
// Result: "AB******C"

// Auto-detection
var autoRule = new NationalIdMaskRule();
autoRule.Apply("123-45-6789");
// Result: "***-**-6789" (detected as US SSN)
```

**Use Cases:**
- Multi-country applications
- GDPR/HIPAA compliance
- Government ID masking

**Status:** ✅ Implemented

**See Also:** [NationalIdMaskRule Documentation](./NationalIdMaskRule.md)

---

### URLMaskRule
Masks URLs while preserving the protocol and domain structure.

**Parameters:**
- `maskPath` (bool): Whether to mask the path (default: true)
- `maskQueryString` (bool): Whether to mask query parameters (default: true)
- `keepDomain` (bool): Whether to keep domain visible (default: true)
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
var rule = new URLMaskRule(maskPath: true, maskQueryString: true);
rule.Apply("https://example.com/users/12345?token=abc123");
// Result: "https://example.com/*****/****?*****=******"
```

**Use Cases:**
- API request logging
- Web analytics with privacy
- Referrer URL masking

**Status:** ✅ Implemented

---

## Cryptographic & Statistical Rules

### HashRule
Applies cryptographic hashing for pseudonymization.

**Parameters:**
- `algorithm` (HashAlgorithmType): Hash algorithm to use (default: SHA256)
  - `SHA256`: 256-bit SHA (recommended)
  - `SHA512`: 512-bit SHA (more secure)
  - `MD5`: Legacy (shows warning, not recommended)
- `saltMode` (SaltMode): Salt generation strategy (default: Static)
  - `Static`: Deterministic (same input → same output)
  - `PerRecord`: Non-deterministic (same input → different outputs)
  - `PerField`: Field-specific deterministic
- `outputFormat` (OutputFormat): Output encoding (default: Hex)
  - `Hex`: Lowercase hexadecimal
  - `Base64`: Standard Base64
  - `Base64Url`: URL-safe Base64
- `staticSalt` (byte[]): Custom static salt (optional)
- `fieldName` (string): Field name for PerField mode (required for PerField)

**Example:**
```csharp
// GDPR pseudonymization (deterministic)
var rule1 = new HashRule(HashAlgorithmType.SHA256, SaltMode.Static);
rule1.Apply("user@example.com");
// Result: "a1b2c3d4e5f6..." (64 hex chars, always same for this input)

// GDPR anonymization (non-deterministic)
var rule2 = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerRecord);
rule2.Apply("John Doe");
// Result: "x7y8z9a1b2c3..." (different each time)

// Per-field hashing
var rule3 = new HashRule(HashAlgorithmType.SHA256, SaltMode.PerField,
                         OutputFormat.Base64, fieldName: "Email");
rule3.Apply("test@example.com");
// Result: Base64-encoded hash unique to "Email" field
```

**Use Cases:**
- GDPR Article 32 pseudonymization
- User ID obfuscation for analytics
- Searchable encrypted identifiers

**Status:** ✅ Implemented

**Security Notes:**
- Uses `RandomNumberGenerator` for cryptographically secure salts
- MD5 displays warning (cryptographically broken)
- Static salts should be stored securely (Key Vault, env vars)

---

### NoiseAdditiveRule
Adds random noise to numeric values for statistical privacy.

**Parameters:**
- `minNoise` (double): Minimum noise value to add
- `maxNoise` (double): Maximum noise value to add
- `preserveSign` (bool): Whether to keep the original sign (default: true)

**Example:**
```csharp
var rule = new NoiseAdditiveRule(minNoise: -5, maxNoise: 5);
rule.Apply("120");
// Result: "123" (added +3 noise, varies per execution)
```

**Use Cases:**
- Medical data privacy (HIPAA)
- Statistical database privacy
- Differential privacy implementations

**Status:** ✅ Implemented

---

### RoundToRule<T>
Rounds numeric values to the nearest multiple.

**Parameters:**
- `roundTo` (T): Value to round to (e.g., 5, 10, 100)
- `roundingMode` (MidpointRounding): How to handle midpoints (default: AwayFromZero)

**Example:**
```csharp
var rule = new RoundToRule<decimal>(roundTo: 5m);
rule.Apply(123m);
// Result: 125

var ageRule = new RoundToRule<int>(roundTo: 10);
ageRule.Apply(47);
// Result: 50
```

**Use Cases:**
- Age bucketing (round to nearest 5 or 10 years)
- Salary ranges
- Geolocation obfuscation (round coordinates)

**Status:** ✅ Implemented

---

### BucketizeRule
Groups numeric values into predefined buckets/ranges.

**Parameters:**
- `bucketBreaks` (int[]): Array of bucket boundaries (must be sorted)
- `bucketLabels` (string[]): Optional labels for each bucket

**Example:**
```csharp
// Age groups
var rule = new BucketizeRule(new[] { 0, 18, 30, 45, 65, 100 });
rule.Apply(32);
// Result: "30-45"

// With custom labels
var labeledRule = new BucketizeRule(
    new[] { 0, 18, 65, 100 },
    new[] { "Minor", "Adult", "Senior" }
);
labeledRule.Apply(25);
// Result: "Adult"
```

**Use Cases:**
- HIPAA age aggregation (90+)
- Income brackets
- K-anonymity implementation

**Status:** ✅ Implemented

---

## Date & Time Rules

### DateAgeMaskRule
Masks dates and ages with multiple strategies for HIPAA/GDPR compliance.

**Parameters:**
- `mode` (MaskingMode): Masking strategy (default: YearOnly)
  - `YearOnly`: Show only year (`1982-11-23` → `1982-**-**`)
  - `DateShift`: Shift date by random offset
  - `Redact`: Complete redaction
- `daysRange` (int): Range for date shifting (±N days) (default: 365)
- `ageBucketing` (bool): Whether to bucket ages (default: false)
- `customAgeBreaks` (int[]): Custom age bucket boundaries
- `customAgeLabels` (string[]): Custom age bucket labels
- `maskChar` (string): Character for masking (default: `"*"`)
- `separator` (string): Date separator (default: `"-"`)

**Example:**
```csharp
// Year-only (HIPAA compliant)
var rule1 = new DateAgeMaskRule();
rule1.Apply("1982-11-23");
// Result: "1982-**-**"

// Age bucketing with automatic 90+ grouping
var rule2 = new DateAgeMaskRule(ageBucketing: true);
rule2.ApplyAge(94);
// Result: "90+" (HIPAA compliant)

// Date shifting (preserves temporal relationships)
var patientId = "patient-001";
var rule3 = new DateAgeMaskRule(MaskingMode.DateShift, daysRange: 180);
rule3.SeedProvider = dt => patientId.GetHashCode(); // Consistent per patient
rule3.Apply("2024-01-15");
// Result: "2024-06-10" (shifted +147 days, consistent for this patient)
```

**Use Cases:**
- HIPAA Safe Harbor de-identification
- GDPR data minimization
- Clinical research datasets

**Status:** ✅ Implemented

**See Also:** [DateAgeMaskRule Documentation](./DateAgeMaskRule.md)

---

### DateShiftRule
Shifts dates by a random offset while preserving temporal relationships.

**Parameters:**
- `daysRange` (int): Maximum days to shift (±N days)
- `preserveTime` (bool): Whether to keep time-of-day (default: true)
- `seedProvider` (Func<DateTime, int>): Function to generate consistent seeds

**Example:**
```csharp
var patientId = "patient-12345";
var rule = new DateShiftRule(daysRange: 180, preserveTime: true);
rule.SeedProvider = dt => patientId.GetHashCode();

var admission = new DateTime(2024, 1, 15, 08, 30, 0);
var discharge = new DateTime(2024, 1, 20, 10, 0, 0);

var maskedAdmission = rule.Apply(admission);
var maskedDischarge = rule.Apply(discharge);

// Both shifted by same amount (e.g., +73 days)
// maskedAdmission = 2024-03-29 08:30:00
// maskedDischarge = 2024-04-03 10:00:00
// Duration preserved: still 5 days apart
```

**Use Cases:**
- HIPAA clinical date de-identification
- Preserving patient journey timelines
- Research data anonymization

**Status:** ✅ Implemented

---

### TimeBucketRule
Rounds timestamps to time buckets (hour, day, week, month, year).

**Parameters:**
- `granularity` (Granularity): Bucket size
  - `Hour`: Round to hour
  - `Day`: Round to day
  - `Week`: Round to week (Monday start)
  - `Month`: Round to month
  - `Year`: Round to year

**Example:**
```csharp
var rule = new TimeBucketRule(Granularity.Hour);
rule.Apply(new DateTime(2024, 11, 15, 14, 23, 45));
// Result: 2024-11-15 14:00:00

var dayRule = new TimeBucketRule(Granularity.Day);
dayRule.Apply(new DateTime(2024, 11, 15, 14, 23, 45));
// Result: 2024-11-15 00:00:00
```

**Use Cases:**
- Analytics time aggregation
- Audit log privacy
- Time-based k-anonymity

**Status:** ✅ Implemented

---

### TimeBucketOffsetRule
Similar to TimeBucketRule but with configurable offset from bucket start.

**Parameters:**
- `granularity` (Granularity): Bucket size
- `offsetMinutes` (int): Offset from bucket start (default: 0)

**Example:**
```csharp
var rule = new TimeBucketOffsetRule(Granularity.Hour, offsetMinutes: 30);
rule.Apply(new DateTime(2024, 11, 15, 14, 23, 45));
// Result: 2024-11-15 14:30:00
```

**Use Cases:**
- Custom time rounding
- Timezone-aware bucketing
- Scheduled task alignment

**Status:** ✅ Implemented

---

## Pattern-Based Rules

### RegexMaskGroupRule
Masks only specific capture groups within regex pattern matches.

**Parameters:**
- `pattern` (string): Regular expression with capture groups
- `groupIndex` (int): Index of group to mask (0 = entire match, 1+ = capture groups)
- `maskChar` (string): Character to use for masking (default: `"*"`)
- `timeout` (TimeSpan): Regex timeout (default: 100ms, prevents ReDoS)
- `options` (RegexOptions): Regex options (default: None)

**Example:**
```csharp
// Mask area code in phone number (group 1)
var rule = new RegexMaskGroupRule(@"(\d{3})-(\d{3})-(\d{4})", groupIndex: 1);
rule.Apply("555-123-4567");
// Result: "***-123-4567"

// Mask middle section of SSN (group 2)
var ssnRule = new RegexMaskGroupRule(@"(\d{3})-(\d{2})-(\d{4})", groupIndex: 2, "X");
ssnRule.Apply("123-45-6789");
// Result: "123-XX-6789"
```

**Use Cases:**
- Custom format masking
- Selective pattern hiding
- Complex structured data

**Status:** ✅ Implemented

**Security:** 100ms timeout prevents ReDoS attacks

---

### RegexReplaceRule
Replaces regex matches with a replacement string or pattern.

**Parameters:**
- `pattern` (string): Regular expression to match
- `replacement` (string): Replacement string (supports $1, $2 backreferences)
- `timeout` (TimeSpan): Regex timeout (default: 100ms)
- `options` (RegexOptions): Regex options (default: None)

**Example:**
```csharp
// Replace all digits with asterisks
var rule = new RegexReplaceRule(@"\d", "*");
rule.Apply("Account: 123456");
// Result: "Account: ******"

// Using backreferences
var swapRule = new RegexReplaceRule(@"(\w+)\s+(\w+)", "$2, $1");
swapRule.Apply("John Doe");
// Result: "Doe, John"
```

**Use Cases:**
- Pattern-based redaction
- Format transformations
- Advanced text masking

**Status:** ✅ Implemented

---

### TemplateMaskRule
Declarative template-based masking with token syntax.

**Parameters:**
- `template` (string): Template with `{{token}}` placeholders

**Supported Tokens:**
- `{{F}}` or `{{F|n}}`: First n characters (default 1)
- `{{L}}` or `{{L|n}}`: Last n characters (default 1)
- `{{*xN}}`: N masked characters (e.g., `{{*x5}}` = `"*****"`)
- `{{digits}}` or `{{digits|start-end}}`: Extract digits with optional range
- `{{letters}}` or `{{letters|start-end}}`: Extract letters with optional range

**Example:**
```csharp
// Name masking
var rule1 = new TemplateMaskRule("{{F}}{{*x6}}{{L}}");
rule1.Apply("JohnDoe");
// Result: "J******e"

// Phone masking
var rule2 = new TemplateMaskRule("+{{digits|0-2}} ** ** {{digits|-2}}");
rule2.Apply("+45 12 34 56 78");
// Result: "+45 ** ** 78"

// Complex format
var rule3 = new TemplateMaskRule("{{F|3}}-{{*x4}}-{{L|4}}");
rule3.Apply("ABC-12345678-XYZ");
// Result: "ABC-****-5678"
```

**Use Cases:**
- Complex custom formats
- Declarative masking rules
- Configuration-driven masking

**Status:** ✅ Implemented

---

## Character Class Rules

### MaskCharClassRule
Masks characters based on character classes (digits, letters, etc.).

**Parameters:**
- `charClass` (CharacterClass): Type of characters to mask
  - `Digits`: Numeric characters (0-9)
  - `Letters`: Alphabetic characters (a-z, A-Z)
  - `Uppercase`: Uppercase letters only
  - `Lowercase`: Lowercase letters only
  - `Punctuation`: Punctuation marks
  - `Whitespace`: Spaces, tabs, newlines
- `maskChar` (string): Character to use for masking (default: `"*"`)

**Example:**
```csharp
// Mask all digits
var rule1 = new MaskCharClassRule(CharacterClass.Digits);
rule1.Apply("User ID: 12345");
// Result: "User ID: *****"

// Mask all letters
var rule2 = new MaskCharClassRule(CharacterClass.Letters, "#");
rule2.Apply("Account: ABC123");
// Result: "Account: ###123"
```

**Use Cases:**
- Selective character masking
- Format-preserving obfuscation
- Pattern-based redaction

**Status:** ✅ Implemented

---

### BlacklistCharsRule
Removes or masks specific blacklisted characters.

**Parameters:**
- `blacklistedChars` (string): Characters to remove/mask
- `maskChar` (string): Replacement character (default: empty string for removal)

**Example:**
```csharp
// Remove special characters
var rule = new BlacklistCharsRule("!@#$%^&*()");
rule.Apply("Email: user@example.com!");
// Result: "Email: userexample.com"

// Replace with asterisks
var maskRule = new BlacklistCharsRule("0123456789", "*");
maskRule.Apply("PIN: 1234");
// Result: "PIN: ****"
```

**Use Cases:**
- Input sanitization
- Special character filtering
- Format normalization

**Status:** ✅ Implemented

---

### WhitelistCharsRule
Keeps only whitelisted characters, removes all others.

**Parameters:**
- `whitelistedChars` (string): Characters to keep
- `replacementChar` (string): Replacement for non-whitelisted chars (default: empty string)

**Example:**
```csharp
// Keep only digits
var rule = new WhitelistCharsRule("0123456789");
rule.Apply("Phone: +1 (555) 123-4567");
// Result: "15551234567"

// Keep alphanumeric, replace others with space
var alnumRule = new WhitelistCharsRule(
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", " ");
alnumRule.Apply("Hello, World!");
// Result: "Hello  World "
```

**Use Cases:**
- Data normalization
- Input validation
- Format enforcement

**Status:** ✅ Implemented

---

## Collection Rules

### MaskForEachRule<TItem>
Applies a masker to each item in a collection.

**Parameters:**
- `masker` (AbstractMasker<TItem>): The masker to apply to each item

**Example:**
```csharp
public class PersonMasker : AbstractMasker<Person>
{
    public PersonMasker()
    {
        MaskFor(x => x.FirstName, m => m.MaskStart(2));
        MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule());
    }
}

public class OrderMasker : AbstractMasker<Order>
{
    public OrderMasker()
    {
        MaskFor(x => x.OrderId, m => m); // Keep OrderId

        // Mask collection of people
        MaskFor(x => x.Customers, new MaskForEachRule<Person>(new PersonMasker()));
    }
}

// Usage
var order = new Order
{
    OrderId = "ORD-001",
    Customers = new List<Person>
    {
        new Person { FirstName = "John", Email = "john@example.com" },
        new Person { FirstName = "Jane", Email = "jane@example.com" }
    }
};

var masker = new OrderMasker();
var result = masker.Mask(order);
// All customers in the collection are masked
```

**Use Cases:**
- Nested object masking
- Collection data protection
- Hierarchical data structures

**Status:** ✅ Implemented

---

## Quick Reference Table

| Rule | Category | Primary Use | GDPR | HIPAA | PCI-DSS | Status |
|------|----------|-------------|------|-------|---------|--------|
| MaskStartRule | Position | Prefix masking | ✅ | ✅ | ✅ | ✅ |
| MaskEndRule | Position | Suffix masking | ✅ | ✅ | ✅ | ✅ |
| MaskMiddleRule | Position | Keep first/last | ✅ | ✅ | ✅ | ✅ |
| MaskRangeRule | Position | Range masking | ✅ | ✅ | ✅ | ✅ |
| MaskPercentageRule | Position | Proportional masking | ✅ | ⚠️ | ⚠️ | ✅ |
| KeepFirstRule | Position | Prefix only | ✅ | ✅ | ✅ | ✅ |
| KeepLastRule | Position | Suffix only | ✅ | ✅ | ✅ | ✅ |
| RedactRule | Position | Complete redaction | ✅ | ✅ | ✅ | ✅ |
| TruncateRule | Position | Length limiting | ⚠️ | ⚠️ | ⚠️ | ✅ |
| NullOutRule | Position | Null replacement | ✅ | ✅ | ✅ | ✅ |
| EmailMaskRule | Format | Email masking | ✅ | ✅ | ❌ | ✅ |
| PhoneMaskRule | Format | Phone masking | ✅ | ✅ | ❌ | ✅ |
| CardMaskRule | Format | Credit card | ❌ | ❌ | ✅ | ✅ |
| IBANMaskRule | Format | Bank accounts | ✅ | ❌ | ⚠️ | ✅ |
| NationalIdMaskRule | Format | National IDs | ✅ | ✅ | ❌ | ✅ |
| URLMaskRule | Format | URL masking | ✅ | ⚠️ | ⚠️ | ✅ |
| HashRule | Crypto | Pseudonymization | ✅ | ⚠️ | ❌ | ✅ |
| NoiseAdditiveRule | Statistical | Random noise | ⚠️ | ✅ | ❌ | ✅ |
| RoundToRule | Statistical | Rounding | ⚠️ | ✅ | ❌ | ✅ |
| BucketizeRule | Statistical | Bucketing | ✅ | ✅ | ❌ | ✅ |
| DateAgeMaskRule | Date/Time | Date & age | ✅ | ✅ | ❌ | ✅ |
| DateShiftRule | Date/Time | Date shifting | ⚠️ | ✅ | ❌ | ✅ |
| TimeBucketRule | Date/Time | Time rounding | ✅ | ⚠️ | ❌ | ✅ |
| TimeBucketOffsetRule | Date/Time | Time rounding | ✅ | ⚠️ | ❌ | ✅ |
| RegexMaskGroupRule | Pattern | Regex groups | ✅ | ✅ | ✅ | ✅ |
| RegexReplaceRule | Pattern | Regex replace | ✅ | ✅ | ✅ | ✅ |
| TemplateMaskRule | Pattern | Template-based | ✅ | ✅ | ✅ | ✅ |
| MaskCharClassRule | Character | Class-based | ⚠️ | ⚠️ | ⚠️ | ✅ |
| BlacklistCharsRule | Character | Char removal | ⚠️ | ⚠️ | ⚠️ | ✅ |
| WhitelistCharsRule | Character | Char filtering | ⚠️ | ⚠️ | ⚠️ | ✅ |
| MaskForEachRule | Collection | Collection masking | ✅ | ✅ | ✅ | ✅ |

**Legend:**
- ✅ Recommended for compliance
- ⚠️ Can be used with caution
- ❌ Not applicable for this compliance requirement

---

## Deprecated Rules

The following rules are maintained for backward compatibility but should not be used in new code:

| Deprecated Rule | Replacement | Reason |
|----------------|-------------|--------|
| MaskFirstRule | MaskStartRule | Clearer naming convention |
| MaskLastRule | MaskEndRule | Clearer naming convention |

---

## Performance Characteristics

All mask rules are optimized for high performance:

- **ArrayPool Usage**: Zero-allocation string operations where possible
- **Compiled Regex**: Patterns are compiled for 10x+ faster matching
- **ReDoS Protection**: 100ms timeout on regex operations
- **Expression Trees**: Compiled property access for reflection-based operations

**Benchmark Results** (from BASELINE_PERFORMANCE.md):
- Position-based rules: **40-50M ops/sec**
- Format-specific rules: **1-5M ops/sec**
- Cryptographic rules: **100K-1M ops/sec**

All rules meet or exceed the target of **50,000 ops/sec** with **<50 KB memory** per 1,000 operations.

---

## Usage Patterns

### 1. Direct Instantiation

```csharp
var rule = new MaskStartRule(count: 3, maskChar: "*");
var result = rule.Apply("JohnDoe");
```

### 2. With AbstractMasker (Recommended)

```csharp
public class PersonMasker : AbstractMasker<Person>
{
    public PersonMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        MaskFor(x => x.FirstName, m => m.MaskStart(2));
        MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule());
        MaskFor(x => x.SSN, (IMaskRule)new RedactRule());
    }
}
```

### 3. With Fluent Builder (v2.0+)

```csharp
using ITW.FluentMasker.Extensions;

var result = StringMaskingBuilder.For(person)
    .MaskStart(x => x.FirstName, count: 2)
    .MaskMiddle(x => x.Email, keepFirst: 3, keepLast: 3)
    .Build();
```

---

## Creating Custom Rules

To create a custom mask rule, implement the `IMaskRule<TInput, TOutput>` interface:

```csharp
public class CustomMaskRule : IMaskRule<string, string>
{
    public string Apply(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Your masking logic here

        return maskedValue;
    }
}
```

**Best Practices:**
1. Handle null and empty inputs gracefully
2. Use `ArrayPool<char>` for string operations
3. Validate constructor parameters
4. Add comprehensive XML documentation
5. Include usage examples in comments

---

## See Also

- [Getting Started Guide](./GettingStarted.md)
- [Compliance Guide (GDPR/HIPAA/PCI-DSS)](./ComplianceGuide.md)
- [DateAgeMaskRule Documentation](./DateAgeMaskRule.md)
- [NationalIdMaskRule Documentation](./NationalIdMaskRule.md)
- [Serilog Integration](./SerilogIntegration.md)
- [ILogger Integration](./ILoggerIntegration.md)
- [Main README](../ITW.FluentMasker/README.md)

---

## Contributing

If you'd like to contribute a new mask rule:

1. Implement `IMaskRule<TInput, TOutput>`
2. Add comprehensive unit tests
3. Include XML documentation with examples
4. Add performance benchmarks
5. Update this reference document
6. Submit a pull request

For questions or feature requests, please open an issue on [GitHub](https://github.com/UlrikAtItWrk/FluentMasker).
