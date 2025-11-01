# FluentMasker Compliance Guide

## GDPR, HIPAA, and PCI-DSS Implementation

This comprehensive guide demonstrates how to use FluentMasker to achieve compliance with major data protection regulations: **GDPR** (General Data Protection Regulation), **HIPAA** (Health Insurance Portability and Accountability Act), and **PCI-DSS** (Payment Card Industry Data Security Standard).

---

## Table of Contents

1. [GDPR Compliance](#gdpr-compliance)
2. [HIPAA Compliance](#hipaa-compliance)
3. [PCI-DSS Compliance](#pci-dss-compliance)
4. [Cross-Regulation Scenarios](#cross-regulation-scenarios)
5. [Compliance Checklists](#compliance-checklists)

---

## GDPR Compliance

### Overview

The **General Data Protection Regulation (GDPR)** is a European Union regulation that governs how personal data of EU residents must be processed, stored, and protected. FluentMasker helps organizations comply with several key GDPR principles:

- **Data Minimization** (Article 5(1)(c)): Only process the minimum necessary personal data
- **Pseudonymization** (Article 32): Transform data so it cannot be attributed to a specific individual without additional information
- **Right to Erasure** (Article 17): Enable safe deletion while maintaining analytical capabilities
- **Logging Personal Data Safely**: Ensure logs don't expose sensitive personal information

### Key GDPR Concepts

#### Pseudonymization vs. Anonymization

| Concept | Definition | Reversible? | GDPR Protection | FluentMasker Implementation |
|---------|-----------|-------------|-----------------|----------------------------|
| **Pseudonymization** | Replacing identifiable data with pseudonyms | Yes (with key) | Still considered personal data, but offers reduced risk | `HashRule` with static salt |
| **Anonymization** | Irreversibly removing personal identifiers | No | No longer personal data | `HashRule` with per-record salt, `RedactRule` |

### 1. Data Minimization Patterns

**Principle**: Only log or store the minimum data required for the business purpose.

```csharp
using ITW.FluentMasker;
using ITW.FluentMasker.Extensions;

public class GDPRUserMasker : AbstractMasker<User>
{
    public GDPRUserMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Remove properties without explicit masking rules
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // Keep only necessary identification (first letter of name)
        MaskFor(x => x.FirstName, m => m.KeepFirst(1));
        MaskFor(x => x.LastName, m => m.KeepFirst(1));
        
        // Pseudonymize email for analytics
        MaskFor(x => x.Email, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.Static,
            OutputFormat.Hex));
        
        // Mask phone number showing only country code
        MaskFor(x => x.PhoneNumber, new PhoneMaskRule(keepLast: 0, preserveSeparators: true));
        
        // Age groups instead of exact age (k-anonymity)
        MaskFor(x => x.Age, new BucketizeRule(new[] { 0, 18, 25, 35, 45, 55, 65, 100 }));
        
        // Remove precise location, keep only city
        MaskFor(x => x.Address, m => m.KeepFirst(0).Truncate(0));
        MaskFor(x => x.City, m => m); // Keep full city name
    }
}

// Usage
var user = new User
{
    FirstName = "Sophie",
    LastName = "Anderson",
    Email = "sophie.anderson@email.com",
    PhoneNumber = "+44-20-1234-5678",
    Age = 32,
    Address = "123 High Street, Apartment 4B",
    City = "London"
};

var masker = new GDPRUserMasker();
var result = masker.Mask(user);
// Result contains only: {"FirstName":"S","LastName":"A","Email":"<hash>","PhoneNumber":"+44-**-****-****","Age":"35-45","City":"London"}
```

### 2. Pseudonymization for Analytics

**Use Case**: Maintain ability to track user behavior across sessions without exposing real identity.

```csharp
public class AnalyticsPseudonymizer : AbstractMasker<UserSession>
{
    private static readonly byte[] _analyticsSecret = 
        Convert.FromBase64String("YourStaticSaltHere=="); // Store securely!

    public AnalyticsPseudonymizer()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // Pseudonymize user ID - same user always gets same pseudonym
        MaskFor(x => x.UserId, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.Static,
            OutputFormat.Base64Url,
            staticSalt: _analyticsSecret));
        
        // Pseudonymize email but keep it searchable
        MaskFor(x => x.Email, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.Static,
            OutputFormat.Hex,
            staticSalt: _analyticsSecret));
        
        // Round timestamps to hour buckets
        MaskFor(x => x.SessionStart, new TimeBucketRule(TimeBucketRule.Granularity.Hour));
        
        // Keep behavioral data (non-personal)
        MaskFor(x => x.PageViews, m => m); // No masking needed
        MaskFor(x => x.ClickCount, m => m); // No masking needed
    }
}

// Search example: When you need to find sessions for a specific email
var searchEmail = "sophie.anderson@email.com";
var hashedEmail = new HashRule(
    HashAlgorithmType.SHA256,
    SaltMode.Static,
    OutputFormat.Hex,
    staticSalt: _analyticsSecret).Apply(searchEmail);

// Query logs: SELECT * FROM sessions WHERE email_hash = @hashedEmail
```

### 3. Right to Erasure Implementation

**Use Case**: Comply with GDPR Article 17 - users can request deletion of their data.

```csharp
public class RightToErasureMasker : AbstractMasker<UserRecord>
{
    public RightToErasureMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Exclude);

        // Completely anonymize personal identifiers
        MaskFor(x => x.UserId, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.PerRecord)); // Different hash each time = true anonymization
        
        MaskFor(x => x.Name, new RedactRule("[ERASED]"));
        MaskFor(x => x.Email, new RedactRule("[ERASED]"));
        MaskFor(x => x.PhoneNumber, new RedactRule("[ERASED]"));
        MaskFor(x => x.DateOfBirth, new NullOutRule());
        MaskFor(x => x.Address, new NullOutRule());
        
        // Keep aggregated/non-personal data for business analytics
        MaskFor(x => x.TotalOrders, m => m); // Count is not personal
        MaskFor(x => x.AccountCreatedDate, new TimeBucketRule(TimeBucketRule.Granularity.Month));
        MaskFor(x => x.LastLoginDate, new TimeBucketRule(TimeBucketRule.Granularity.Month));
    }
}

// Usage: When user requests erasure
var userRecord = database.GetUser(userId);
var erasureMasker = new RightToErasureMasker();
var anonymizedRecord = erasureMasker.Mask(userRecord);

// Update the database with anonymized data
database.UpdateUser(anonymizedRecord.MaskedData);

// Result: All personal data is gone, but business metrics remain
```

### 4. Logging Personal Data Safely (Serilog Integration)

**Use Case**: Ensure application logs don't expose personal data in violation of GDPR.

```csharp
using Serilog;
using ITW.FluentMasker.Serilog.Destructure;

// Configure Serilog with FluentMasker
Log.Logger = new LoggerConfiguration()
    .Destructure.With(new FluentMaskerPolicy())
    .WriteTo.File("logs/application.log")
    .WriteTo.Console()
    .CreateLogger();

// Define GDPR-compliant masker for logging
public class UserLogMasker : AbstractMasker<User>
{
    public UserLogMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // Mask all personal identifiers
        MaskFor(x => x.FirstName, m => m.KeepFirst(1));
        MaskFor(x => x.LastName, m => m.KeepFirst(1));
        MaskFor(x => x.Email, new EmailMaskRule(localKeep: 1, domainStrategy: EmailDomainStrategy.KeepRoot));
        MaskFor(x => x.PhoneNumber, new PhoneMaskRule(keepLast: 4));
        MaskFor(x => x.SSN, new RedactRule());
        MaskFor(x => x.DateOfBirth, new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.YearOnly));
        
        // Keep non-sensitive operational data
        MaskFor(x => x.UserId, new HashRule(HashAlgorithmType.SHA256, SaltMode.Static));
        MaskFor(x => x.AccountType, m => m); // Not personal
        MaskFor(x => x.IsActive, m => m); // Not personal
    }
}

// Log user events safely - use {@Object} syntax for destructuring
var user = GetCurrentUser();

// ✅ CORRECT - Automatically masked
Log.Information("User login successful: {@User}", user);
// Output: User login successful: {"FirstName":"S","LastName":"A","Email":"s***@example.com",...}

// ❌ WRONG - No masking, violates GDPR!
Log.Information("User login: {User}", user); // Just calls ToString()
```

### 5. Data Subject Access Requests (DSAR)

**Use Case**: Provide users with a copy of their data (Article 15) in a privacy-respecting format.

```csharp
public class DSARExportMasker : AbstractMasker<UserProfile>
{
    public DSARExportMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Include all properties (user has right to see their own data)
        SetPropertyRuleBehavior(PropertyRuleBehavior.Include);

        // Mask only internal system identifiers and sensitive third-party data
        MaskFor(x => x.InternalUserId, new HashRule(HashAlgorithmType.SHA256));
        MaskFor(x => x.PasswordHash, new RedactRule("[PROTECTED]"));
        MaskFor(x => x.SecurityAnswers, new RedactRule("[PROTECTED]"));
        
        // Show all personal data as-is (it's their data!)
        // MaskFor(x => x.Name, m => m); - No masking for DSAR
        // MaskFor(x => x.Email, m => m); - No masking for DSAR
    }
}
```

---

## HIPAA Compliance

### Overview

The **Health Insurance Portability and Accountability Act (HIPAA)** protects sensitive patient health information in the United States. The **HIPAA Privacy Rule** (45 CFR Part 164) establishes national standards for protecting health records.

FluentMasker supports the **Safe Harbor Method** for de-identification (§164.514(b)(2)), which requires removal or transformation of 18 specific identifiers.

### HIPAA Safe Harbor Method - 18 Identifiers

| Identifier | Description | FluentMasker Rule |
|-----------|-------------|-------------------|
| 1. Names | All names (patient, relatives, employers) | `MaskStartRule`, `KeepFirstRule`, `RedactRule` |
| 2. Geographic subdivisions | Addresses, cities (smaller than state) | `TruncateRule`, `RedactRule` |
| 3. Dates | Birth dates, admission dates, death dates | `DateShiftRule`, `DateAgeMaskRule` |
| 4. Telephone numbers | All phone numbers | `PhoneMaskRule` with `keepLast: 0` |
| 5. Fax numbers | All fax numbers | `PhoneMaskRule` with `keepLast: 0` |
| 6. Email addresses | All email addresses | `EmailMaskRule`, `RedactRule` |
| 7. Social Security numbers | SSN | `RedactRule`, `HashRule` |
| 8. Medical record numbers | MRN | `HashRule` |
| 9. Health plan numbers | Insurance ID | `HashRule` |
| 10. Account numbers | Billing accounts | `HashRule` |
| 11. Certificate/license numbers | Professional licenses | `HashRule`, `RedactRule` |
| 12. Vehicle identifiers | VIN, license plates | `RedactRule` |
| 13. Device identifiers | Device serial numbers | `RedactRule` |
| 14. URLs | Web addresses | `URLMaskRule`, `RedactRule` |
| 15. IP addresses | Network addresses | `RedactRule` |
| 16. Biometric identifiers | Fingerprints, retinal scans | `RedactRule` |
| 17. Photos | Full face photos | `RedactRule` (filepath) |
| 18. Other unique codes | Any other unique identifier | `HashRule`, `RedactRule` |

### 1. Safe Harbor De-Identification

```csharp
using ITW.FluentMasker;
using ITW.FluentMasker.Extensions;

public class HIPAASafeHarborMasker : AbstractMasker<PatientRecord>
{
    private readonly string _patientIdSeed; // For consistent date shifting

    public HIPAASafeHarborMasker(string patientIdSeed)
    {
        _patientIdSeed = patientIdSeed;
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // 1. Names - Pseudonymize or remove
        MaskFor(x => x.PatientFirstName, new HashRule(HashAlgorithmType.SHA256));
        MaskFor(x => x.PatientLastName, new HashRule(HashAlgorithmType.SHA256));
        
        // 2. Geographic - Keep only state, remove smaller subdivisions
        MaskFor(x => x.StreetAddress, new RedactRule());
        MaskFor(x => x.City, new RedactRule());
        MaskFor(x => x.ZipCode, m => m.KeepFirst(3)); // Keep first 3 digits only
        MaskFor(x => x.State, m => m); // State is allowed
        
        // 3. Dates - CRITICAL: Use DateShiftRule with consistent seed
        var dateShiftRule = new DateShiftRule(daysRange: 365, preserveTime: true);
        dateShiftRule.SeedProvider = dt => _patientIdSeed.GetHashCode();
        
        MaskFor(x => x.DateOfBirth, dateShiftRule);
        MaskFor(x => x.AdmissionDate, dateShiftRule);
        MaskFor(x => x.DischargeDate, dateShiftRule);
        MaskFor(x => x.DateOfService, dateShiftRule);
        // All dates for same patient shift by same amount!
        
        // Ages over 89 must be aggregated
        var ageRule = new DateAgeMaskRule(ageBucketing: true);
        MaskFor(x => x.DateOfBirth, ageRule); // Converts to age bucket
        
        // 4-6. Contact Information - Remove or hash
        MaskFor(x => x.PhoneNumber, new PhoneMaskRule(keepLast: 0));
        MaskFor(x => x.FaxNumber, new PhoneMaskRule(keepLast: 0));
        MaskFor(x => x.Email, new RedactRule());
        
        // 7. SSN - Remove completely
        MaskFor(x => x.SSN, new RedactRule());
        
        // 8-10. Medical/Financial Numbers - Pseudonymize for tracking
        MaskFor(x => x.MedicalRecordNumber, new HashRule(HashAlgorithmType.SHA256));
        MaskFor(x => x.HealthPlanNumber, new HashRule(HashAlgorithmType.SHA256));
        MaskFor(x => x.AccountNumber, new HashRule(HashAlgorithmType.SHA256));
        
        // 11-13. Identifiers - Remove
        MaskFor(x => x.LicenseNumber, new RedactRule());
        MaskFor(x => x.VehicleId, new RedactRule());
        MaskFor(x => x.DeviceSerialNumber, new RedactRule());
        
        // 14-15. Network Identifiers - Remove
        MaskFor(x => x.WebUrl, new RedactRule());
        MaskFor(x => x.IpAddress, new RedactRule());
        
        // 16-17. Biometric/Photos - Remove
        MaskFor(x => x.BiometricData, new RedactRule());
        MaskFor(x => x.PhotoFilePath, new RedactRule());
        
        // 18. Other unique identifiers
        MaskFor(x => x.PatientId, new HashRule(HashAlgorithmType.SHA256));
    }
}

// Usage
var patientRecord = GetPatientRecord("patient-12345");
var masker = new HIPAASafeHarborMasker("patient-12345"); // Use patient ID as seed
var deidentified = masker.Mask(patientRecord);

// Result is HIPAA Safe Harbor compliant
```

### 2. Preserving Temporal Relationships

**CRITICAL for HIPAA**: All dates for the same patient must be shifted by the **same amount** to preserve temporal relationships (e.g., length of hospital stay).

```csharp
public class TemporalPreservationExample
{
    public static void DemonstrateConsistentDateShifting()
    {
        var patientId = "patient-67890";
        
        // Create date shift rule with patient-specific seed
        var dateShiftRule = new DateShiftRule(daysRange: 180, preserveTime: true);
        dateShiftRule.SeedProvider = dt => patientId.GetHashCode(); // Same seed = same shift!

        // Original dates
        var admission = new DateTime(2024, 1, 15, 08, 30, 0);
        var surgery = new DateTime(2024, 1, 16, 14, 0, 0);
        var discharge = new DateTime(2024, 1, 20, 10, 0, 0);

        // Shift all dates
        var maskedAdmission = dateShiftRule.Apply(admission);
        var maskedSurgery = dateShiftRule.Apply(surgery);
        var maskedDischarge = dateShiftRule.Apply(discharge);

        // Verify temporal relationships preserved
        var originalStay = (discharge - admission).Days; // 5 days
        var maskedStay = (maskedDischarge - maskedAdmission).Days; // Still 5 days!
        
        Console.WriteLine($"Original stay: {originalStay} days");
        Console.WriteLine($"Masked stay: {maskedStay} days");
        Console.WriteLine($"Temporal relationship preserved: {originalStay == maskedStay}");
        
        // Time-of-day preserved (08:30, 14:00, 10:00)
        Console.WriteLine($"Admission time: {maskedAdmission:HH:mm}"); // 08:30
        Console.WriteLine($"Surgery time: {maskedSurgery:HH:mm}"); // 14:00
        Console.WriteLine($"Discharge time: {maskedDischarge:HH:mm}"); // 10:00
    }
}
```

### 3. Age Aggregation for Ages > 89

**HIPAA Requirement**: Ages over 89 must be aggregated into a single category (e.g., "90+").

```csharp
public class AgeComplianceMasker : AbstractMasker<Patient>
{
    public AgeComplianceMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // Ages over 89 aggregated to "90+" bucket
        var ageRule = new DateAgeMaskRule(
            mode: DateAgeMaskRule.MaskingMode.AgeBucket,
            ageBucketing: true);
        
        MaskFor(x => x.DateOfBirth, ageRule);
        
        // Alternative: Use BucketizeRule if you already have age calculated
        MaskFor(x => x.Age, new BucketizeRule(new[] { 0, 18, 30, 40, 50, 60, 70, 80, 90, 150 }));
        // Ages 90-150 will show as "90-150" or "90+"
    }
}

// Example outputs:
// Age 25 → "18-30"
// Age 45 → "40-50"
// Age 92 → "90+" (compliant with HIPAA!)
```

### 4. Audit Logging with PHI Masking

**Use Case**: Log access to patient records without exposing PHI in audit logs.

```csharp
public class HIPAAAuditLogMasker : AbstractMasker<AuditLogEntry>
{
    public HIPAAAuditLogMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Include);

        // Hash patient/user identifiers for audit trail
        MaskFor(x => x.PatientId, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.Static,
            OutputFormat.Hex));
        
        MaskFor(x => x.UserId, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.Static,
            OutputFormat.Hex));
        
        // Round timestamps to hour for privacy
        MaskFor(x => x.AccessTimestamp, new TimeBucketRule(TimeBucketRule.Granularity.Hour));
        
        // Keep action type (non-PHI)
        MaskFor(x => x.ActionType, m => m); // "View", "Edit", "Delete"
        
        // Hash IP address
        MaskFor(x => x.IpAddress, new HashRule(HashAlgorithmType.SHA256));
        
        // Redact any PHI captured in notes
        MaskFor(x => x.Notes, new TruncateRule(maxLength: 50, suffix: "[TRUNCATED]"));
    }
}

// Usage with Serilog
Log.Information("Patient record accessed: {@AuditLog}", auditLogEntry);
// No PHI exposed in logs, but audit trail maintained
```

### 5. Research Dataset De-Identification

**Use Case**: Create de-identified datasets for medical research while maintaining statistical validity.

```csharp
public class ResearchDatasetMasker : AbstractMasker<ClinicalData>
{
    private readonly string _studyIdSeed;

    public ResearchDatasetMasker(string studyIdSeed)
    {
        _studyIdSeed = studyIdSeed;
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // Consistent date shifting per patient
        var dateShiftRule = new DateShiftRule(daysRange: 180, preserveTime: false);
        dateShiftRule.SeedProvider = dt => _studyIdSeed.GetHashCode();
        
        MaskFor(x => x.EnrollmentDate, dateShiftRule);
        MaskFor(x => x.FollowUpDate, dateShiftRule);
        
        // Age bucketing
        MaskFor(x => x.Age, new BucketizeRule(new[] { 0, 30, 40, 50, 60, 70, 80, 90, 150 }));
        
        // Pseudonymize identifiers
        MaskFor(x => x.SubjectId, new HashRule(HashAlgorithmType.SHA256));
        
        // Add noise to clinical measurements for privacy
        MaskFor(x => x.BloodPressureSystolic, new NoiseAdditiveRule(-5, 5));
        MaskFor(x => x.Weight, new RoundToRule<decimal>(5m)); // Round to nearest 5 kg
        MaskFor(x => x.Height, new RoundToRule<decimal>(5m)); // Round to nearest 5 cm
        
        // Truncate diagnosis codes (keep only main category)
        MaskFor(x => x.DiagnosisCode, m => m.KeepFirst(3)); // ICD-10: E11.9 → E11
        
        // Keep clinical outcomes (aggregate data)
        MaskFor(x => x.TreatmentOutcome, m => m); // "Improved", "Stable", "Declined"
        MaskFor(x => x.AdverseEvents, m => m); // Count only
    }
}
```

---

## PCI-DSS Compliance

### Overview

The **Payment Card Industry Data Security Standard (PCI-DSS)** is a set of security standards designed to ensure that all companies that accept, process, store, or transmit credit card information maintain a secure environment.

FluentMasker helps with **Requirement 3: Protect Stored Cardholder Data** by ensuring sensitive payment information is properly masked in logs, databases, and reports.

### Key PCI-DSS Requirements

| Requirement | Description | FluentMasker Implementation |
|------------|-------------|----------------------------|
| 3.3 | Mask PAN when displayed (max first 6 + last 4) | `CardMaskRule(keepFirst: 6, keepLast: 4)` |
| 3.4 | Render PAN unreadable (encryption, truncation, masking) | `CardMaskRule`, `HashRule` |
| 4.2 | Never send unprotected PANs | Use maskers before logging/transmission |
| 12.3.10 | Security awareness training on cardholder data protection | Documentation + examples |

**Primary Account Number (PAN)**: The credit card number (typically 13-19 digits).

**Cardholder Data**: PAN, cardholder name, expiration date, service code.

**Sensitive Authentication Data**: CVV/CVC, PIN, magnetic stripe data - **MUST NEVER BE STORED**.

### 1. Credit Card Masking (PAN Protection)

```csharp
using ITW.FluentMasker;
using ITW.FluentMasker.Extensions;

public class PCIDSSCardMasker : AbstractMasker<PaymentCard>
{
    public PCIDSSCardMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // PCI-DSS 3.3: Display max first 6 + last 4 digits
        MaskFor(x => x.CardNumber, new CardMaskRule(
            keepFirst: 6,  // BIN (Bank Identification Number)
            keepLast: 4,   // Last 4 for verification
            preserveGrouping: true,
            validateLuhn: false)); // Validation happens elsewhere
        
        // CVV/CVC MUST NEVER BE LOGGED OR STORED
        MaskFor(x => x.CVV, new RedactRule("***"));
        
        // Cardholder name - mask partially
        MaskFor(x => x.CardholderName, m => m
            .KeepFirst(3)
            .KeepLast(3));
        
        // Expiration date - round to month (day precision not needed)
        MaskFor(x => x.ExpirationDate, new TimeBucketRule(TimeBucketRule.Granularity.Month));
        
        // Billing address - keep only ZIP code for fraud detection
        MaskFor(x => x.BillingAddress, new RedactRule());
        MaskFor(x => x.BillingZip, m => m); // Keep for AVS
    }
}

// Usage
var card = new PaymentCard
{
    CardNumber = "4532-1234-5678-9010",
    CVV = "123",
    CardholderName = "John Smith",
    ExpirationDate = new DateTime(2027, 12, 15),
    BillingAddress = "123 Main St",
    BillingZip = "12345"
};

var masker = new PCIDSSCardMasker();
var result = masker.Mask(card);
// Output: {"CardNumber":"4532-12**-****-9010","CVV":"***","CardholderName":"Joh***ith",...}
```

### 2. Transaction Logging (Secure Audit Trail)

**Use Case**: Log payment transactions for fraud detection without exposing full card details.

```csharp
public class TransactionLogMasker : AbstractMasker<Transaction>
{
    public TransactionLogMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Include);

        // PAN - show only last 4 digits for logs
        MaskFor(x => x.CardNumber, new CardMaskRule(
            keepFirst: 0,
            keepLast: 4,
            preserveGrouping: false));
        
        // NEVER log CVV - even masked!
        MaskFor(x => x.CVV, new NullOutRule()); // Remove from log entirely
        
        // Customer email - pseudonymize for fraud tracking
        MaskFor(x => x.CustomerEmail, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.Static));
        
        // Customer phone - show last 4 for verification
        MaskFor(x => x.CustomerPhone, new PhoneMaskRule(keepLast: 4));
        
        // Keep transaction metadata (non-sensitive)
        MaskFor(x => x.TransactionId, m => m);
        MaskFor(x => x.Amount, m => m);
        MaskFor(x => x.Currency, m => m);
        MaskFor(x => x.MerchantId, m => m);
        MaskFor(x => x.Timestamp, m => m);
        MaskFor(x => x.Status, m => m); // "Approved", "Declined"
        
        // IP address - hash for fraud detection
        MaskFor(x => x.IpAddress, new HashRule(HashAlgorithmType.SHA256));
    }
}

// Serilog integration
Log.Information("Transaction processed: {@Transaction}", transaction);
// Output: Transaction processed: {"CardNumber":"****9010","Amount":125.50,"Status":"Approved",...}
// CVV is completely absent from logs
```

### 3. CVV Handling - CRITICAL REQUIREMENT

**PCI-DSS Requirement 3.2.2**: CVV/CVC/CAV2/CID data **MUST NOT** be stored after authorization.

```csharp
public class CVVSecurityExample
{
    public class PaymentProcessor
    {
        public ProcessPaymentResult ProcessPayment(PaymentRequest request)
        {
            // ✅ CORRECT: Use CVV for authorization
            var authResult = AuthorizePayment(
                request.CardNumber,
                request.CVV,
                request.Amount);

            // ✅ CORRECT: Create log entry WITHOUT CVV
            var logEntry = new TransactionLog
            {
                CardNumber = request.CardNumber,
                Amount = request.Amount,
                Timestamp = DateTime.UtcNow,
                Status = authResult.Status
                // CVV is NOT included!
            };

            // ✅ CORRECT: Mask before logging
            var masker = new TransactionLogMasker();
            var maskedLog = masker.Mask(logEntry);
            
            Log.Information("Payment processed: {@Transaction}", maskedLog.MaskedData);

            // ❌ WRONG: Never store or log CVV!
            // database.SaveTransaction(request); // Contains CVV - VIOLATION!
            // Log.Information("Payment: {Request}", request); // Contains CVV - VIOLATION!

            return authResult;
        }
    }

    // Define request object without CVV storage capability
    public class TransactionLog
    {
        public string CardNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        // No CVV property - by design!
    }
}
```

### 4. Cardholder Data Environment (CDE) Protection

**Use Case**: Protect cardholder data in reports, exports, and data transfers.

```csharp
public class CDEReportMasker : AbstractMasker<PaymentReport>
{
    public CDEReportMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Include);

        // PAN - truncate to last 4 only
        MaskFor(x => x.CardNumber, new CardMaskRule(keepFirst: 0, keepLast: 4));
        
        // Cardholder name - first initial + last initial
        MaskFor(x => x.CardholderName, m => m
            .KeepFirst(1)
            .KeepLast(1));
        
        // Transaction amounts - round for privacy
        MaskFor(x => x.TransactionAmount, new RoundToRule<decimal>(10m));
        
        // Timestamps - bucket to hour
        MaskFor(x => x.TransactionTime, new TimeBucketRule(TimeBucketRule.Granularity.Hour));
        
        // Merchant data - keep for business analysis
        MaskFor(x => x.MerchantId, m => m);
        MaskFor(x => x.MerchantCategory, m => m);
        
        // Geographic data - aggregate to state level
        MaskFor(x => x.City, new RedactRule());
        MaskFor(x => x.State, m => m); // State OK
        MaskFor(x => x.ZipCode, m => m.KeepFirst(3)); // First 3 digits only
    }
}

// Export to CSV for business intelligence
var transactions = GetTransactions();
var masker = new CDEReportMasker();
var maskedTransactions = transactions.Select(t => masker.Mask(t).MaskedData);
ExportToCsv(maskedTransactions, "report.csv");
```

### 5. Fraud Detection Dataset

**Use Case**: Create datasets for machine learning fraud detection while maintaining PCI-DSS compliance.

```csharp
public class FraudDetectionMasker : AbstractMasker<FraudAnalysisRecord>
{
    public FraudDetectionMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // Pseudonymize PAN for fraud pattern tracking
        MaskFor(x => x.CardNumber, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.Static,
            OutputFormat.Hex));
        
        // Keep BIN (first 6 digits) separate for issuer analysis
        MaskFor(x => x.BIN, m => m); // First 6 digits - allowed by PCI-DSS
        
        // Keep last 4 for fraud investigation
        MaskFor(x => x.Last4Digits, m => m);
        
        // Pseudonymize customer identifiers
        MaskFor(x => x.CustomerId, new HashRule(HashAlgorithmType.SHA256));
        MaskFor(x => x.Email, new HashRule(HashAlgorithmType.SHA256));
        
        // Hash IP address for location-based fraud detection
        MaskFor(x => x.IpAddress, new HashRule(HashAlgorithmType.SHA256));
        
        // Keep fraud indicators (non-sensitive)
        MaskFor(x => x.TransactionAmount, m => m);
        MaskFor(x => x.MerchantCategory, m => m);
        MaskFor(x => x.Country, m => m);
        MaskFor(x => x.DeviceFingerprint, new HashRule(HashAlgorithmType.SHA256));
        MaskFor(x => x.IsFraudulent, m => m); // Label for ML
        
        // Velocity features (counts, not PAN)
        MaskFor(x => x.TransactionCount24h, m => m);
        MaskFor(x => x.DeclinedCount7d, m => m);
    }
}
```

### 6. PCI-DSS Testing Data Generation

**Use Case**: Create realistic test data for development/QA without using real card numbers.

```csharp
public class TestDataGenerator
{
    // Test card numbers (from payment processor test docs)
    private static readonly string[] TestCards = new[]
    {
        "4532015112830366", // Visa
        "5425233430109903", // Mastercard
        "374245455400126",  // Amex
        "6011000991001201"  // Discover
    };

    public static PaymentCard GenerateTestCard()
    {
        var random = new Random();
        return new PaymentCard
        {
            CardNumber = TestCards[random.Next(TestCards.Length)],
            CVV = "***", // Use placeholder, not real CVV
            CardholderName = "TEST CARDHOLDER",
            ExpirationDate = DateTime.Now.AddYears(2),
            BillingZip = "12345"
        };
    }

    // Even test data should be masked in logs!
    public static void LogTestTransaction(PaymentCard testCard)
    {
        var masker = new PCIDSSCardMasker();
        var masked = masker.Mask(testCard);
        Log.Debug("Test transaction: {@Card}", masked.MaskedData);
    }
}
```

---

## Cross-Regulation Scenarios

### Scenario 1: Healthcare Payment Processing (HIPAA + PCI-DSS)

**Challenge**: A medical billing system must comply with both HIPAA (patient data) and PCI-DSS (payment data).

```csharp
public class MedicalBillingMasker : AbstractMasker<MedicalBillingRecord>
{
    private readonly string _patientIdSeed;

    public MedicalBillingMasker(string patientIdSeed)
    {
        _patientIdSeed = patientIdSeed;
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // ===== HIPAA Compliance =====
        
        // Patient identifiers - pseudonymize
        MaskFor(x => x.PatientId, new HashRule(HashAlgorithmType.SHA256));
        MaskFor(x => x.PatientName, new HashRule(HashAlgorithmType.SHA256));
        
        // Date shifting with consistency
        var dateShiftRule = new DateShiftRule(daysRange: 180, preserveTime: true);
        dateShiftRule.SeedProvider = dt => _patientIdSeed.GetHashCode();
        MaskFor(x => x.ServiceDate, dateShiftRule);
        
        // Diagnosis codes - keep for billing, but truncate
        MaskFor(x => x.DiagnosisCodes, m => m.KeepFirst(3));
        
        // ===== PCI-DSS Compliance =====
        
        // Payment card - show last 4 only
        MaskFor(x => x.CardNumber, new CardMaskRule(keepFirst: 0, keepLast: 4));
        
        // CVV - never log
        MaskFor(x => x.CVV, new NullOutRule());
        
        // ===== Business Data (Keep) =====
        
        MaskFor(x => x.BillingAmount, m => m);
        MaskFor(x => x.InsuranceClaim, m => m);
        MaskFor(x => x.PaymentStatus, m => m);
    }
}

// Usage
var billing = new MedicalBillingRecord
{
    PatientId = "P-12345",
    PatientName = "Jane Doe",
    ServiceDate = new DateTime(2024, 3, 15),
    DiagnosisCodes = "E11.9",
    CardNumber = "4532123456789010",
    CVV = "123",
    BillingAmount = 250.00m,
    PaymentStatus = "Paid"
};

var masker = new MedicalBillingMasker("P-12345");
var result = masker.Mask(billing);
// Both HIPAA and PCI-DSS compliant!
```

### Scenario 2: European E-Commerce (GDPR + PCI-DSS)

**Challenge**: An online store must protect EU customer data (GDPR) and payment information (PCI-DSS).

```csharp
public class EcommerceOrderMasker : AbstractMasker<OrderRecord>
{
    public EcommerceOrderMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // ===== GDPR Compliance =====
        
        // Personal data minimization
        MaskFor(x => x.CustomerName, m => m.KeepFirst(1));
        MaskFor(x => x.Email, new EmailMaskRule(localKeep: 1));
        MaskFor(x => x.PhoneNumber, new PhoneMaskRule(keepLast: 4));
        
        // Pseudonymize for analytics
        MaskFor(x => x.CustomerId, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.Static));
        
        // Shipping address - keep only country
        MaskFor(x => x.ShippingAddress, new RedactRule());
        MaskFor(x => x.Country, m => m);
        
        // ===== PCI-DSS Compliance =====
        
        // Payment card
        MaskFor(x => x.CardNumber, new CardMaskRule(keepFirst: 0, keepLast: 4));
        MaskFor(x => x.CVV, new NullOutRule());
        
        // ===== Business Analytics (Keep) =====
        
        MaskFor(x => x.OrderId, m => m);
        MaskFor(x => x.OrderTotal, m => m);
        MaskFor(x => x.ProductCategories, m => m);
        MaskFor(x => x.OrderDate, new TimeBucketRule(TimeBucketRule.Granularity.Day));
    }
}
```

### Scenario 3: Global Health Research (GDPR + HIPAA)

**Challenge**: Multi-national clinical trial must comply with both EU (GDPR) and US (HIPAA) regulations.

```csharp
public class GlobalClinicalTrialMasker : AbstractMasker<TrialParticipant>
{
    private readonly string _participantIdSeed;

    public GlobalClinicalTrialMasker(string participantIdSeed)
    {
        _participantIdSeed = participantIdSeed;
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // ===== GDPR + HIPAA Common Requirements =====
        
        // Pseudonymize all identifiers
        MaskFor(x => x.ParticipantId, new HashRule(HashAlgorithmType.SHA256));
        MaskFor(x => x.Name, new HashRule(HashAlgorithmType.SHA256));
        MaskFor(x => x.Email, new HashRule(HashAlgorithmType.SHA256));
        
        // Date shifting (HIPAA Safe Harbor)
        var dateShiftRule = new DateShiftRule(daysRange: 180, preserveTime: false);
        dateShiftRule.SeedProvider = dt => _participantIdSeed.GetHashCode();
        
        MaskFor(x => x.EnrollmentDate, dateShiftRule);
        MaskFor(x => x.FollowUpVisits, new MaskForEachRule<DateTime>(dateShiftRule));
        
        // Age bucketing (HIPAA ages >89 + GDPR minimization)
        MaskFor(x => x.Age, new BucketizeRule(new[] { 0, 18, 30, 40, 50, 60, 70, 80, 90, 150 }));
        
        // Geographic - keep only country
        MaskFor(x => x.Country, m => m);
        MaskFor(x => x.City, new RedactRule());
        
        // ===== Clinical Data (Keep for Research) =====
        
        // Add noise to measurements for privacy
        MaskFor(x => x.BloodPressure, new NoiseAdditiveRule(-5, 5));
        MaskFor(x => x.LabResults, new RoundToRule<decimal>(5m));
        
        // Keep outcomes (aggregate data)
        MaskFor(x => x.AdverseEvents, m => m);
        MaskFor(x => x.TreatmentResponse, m => m);
    }
}
```

---

## Compliance Checklists

### GDPR Compliance Checklist

- [ ] **Data Minimization**: Only necessary fields are logged/stored
- [ ] **Pseudonymization**: Identifiers use `HashRule` with static salt
- [ ] **Anonymization**: Right-to-erasure uses `HashRule` with per-record salt or `RedactRule`
- [ ] **Logging**: Serilog configured with `FluentMaskerPolicy`
- [ ] **Email Masking**: Use `EmailMaskRule` with appropriate strategy
- [ ] **Phone Masking**: Use `PhoneMaskRule` to limit exposed digits
- [ ] **Age Bucketing**: Use `BucketizeRule` for k-anonymity
- [ ] **Property Behavior**: Set to `Remove` or `Exclude` for sensitive data
- [ ] **Audit Trail**: Log data access with pseudonymized identifiers
- [ ] **Documentation**: Maintain records of masking strategies used

### HIPAA Safe Harbor Checklist

All 18 identifiers must be removed or de-identified:

- [ ] **Names**: Masked with `HashRule` or `RedactRule`
- [ ] **Geographic**: Only state kept, city/address redacted
- [ ] **Dates**: All dates shifted using `DateShiftRule` with consistent seed per patient
- [ ] **Phone Numbers**: Completely masked with `PhoneMaskRule(keepLast: 0)`
- [ ] **Fax Numbers**: Completely masked
- [ ] **Email**: Redacted with `RedactRule`
- [ ] **SSN**: Redacted with `RedactRule`
- [ ] **Medical Record Number**: Pseudonymized with `HashRule`
- [ ] **Health Plan Number**: Pseudonymized with `HashRule`
- [ ] **Account Numbers**: Pseudonymized with `HashRule`
- [ ] **License Numbers**: Redacted
- [ ] **Vehicle IDs**: Redacted
- [ ] **Device IDs**: Redacted
- [ ] **URLs**: Redacted
- [ ] **IP Addresses**: Redacted
- [ ] **Biometric Data**: Redacted
- [ ] **Photos**: File paths redacted
- [ ] **Other Unique IDs**: Hashed or redacted
- [ ] **Ages >89**: Aggregated to "90+" with `DateAgeMaskRule` or `BucketizeRule`
- [ ] **ZIP Codes**: Only first 3 digits kept
- [ ] **Temporal Relationships**: Date shift consistent per patient (same seed)

### PCI-DSS Requirement 3 Checklist

- [ ] **PAN Display**: Max first 6 + last 4 visible (`CardMaskRule(keepFirst: 6, keepLast: 4)`)
- [ ] **PAN Storage**: Full PAN never stored unencrypted
- [ ] **CVV/CVC**: **NEVER** stored, logged, or displayed after authorization (`NullOutRule`)
- [ ] **Cardholder Name**: Masked with `KeepFirst`/`KeepLast`
- [ ] **Expiration Date**: Rounded to month with `TimeBucketRule`
- [ ] **Transaction Logs**: PAN shows last 4 only
- [ ] **Audit Logs**: CVV completely absent
- [ ] **Reports/Exports**: PAN truncated to last 4
- [ ] **Email/Transmission**: No unprotected PAN sent
- [ ] **Test Data**: Use designated test cards, still mask in logs
- [ ] **Development/QA**: Test cards used, production data never accessed
- [ ] **Logging**: Serilog with `FluentMaskerPolicy` for automatic masking

### General Security Best Practices

- [ ] **Static Salts**: Stored securely (Key Vault, environment variables)
- [ ] **Deterministic Hashing**: Document which fields use static vs. per-record salts
- [ ] **Performance**: Use compiled maskers in production (`AbstractMasker` auto-compiles)
- [ ] **Validation**: Test maskers with real data samples before production
- [ ] **Code Reviews**: Security team reviews all new maskers
- [ ] **Documentation**: Document business justification for each unmasked field
- [ ] **Monitoring**: Log masker usage and failures
- [ ] **Incident Response**: Plan for handling exposed data if masking fails
- [ ] **Training**: Developers trained on when to use `{@Object}` vs. `{Object}` in logs
- [ ] **Testing**: Unit tests verify masking rules produce expected outputs

---

## Additional Resources

### FluentMasker Documentation
- [Main README](../ITW.FluentMasker/README.md)
- [Serilog Integration Sample](../ITW.FluentMasker.Serilog.Destructure.Sample/README.md)

### Official Compliance Resources
- **GDPR**: [Official EUR-Lex Text](https://eur-lex.europa.eu/eli/reg/2016/679/oj)
- **HIPAA Safe Harbor**: [45 CFR §164.514(b)(2)](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html)
- **PCI-DSS**: [Official PCI Security Standards](https://www.pcisecuritystandards.org/document_library)

### Tools & Validators
- GDPR Compliance Checker: Review data processing activities
- HIPAA Identifier Validator: Ensure all 18 identifiers are masked
- PCI-DSS Self-Assessment Questionnaire (SAQ): For merchants/service providers
- Luhn Algorithm Validator: Test credit card number validity

---

## Disclaimer

This guide provides technical implementation patterns but does not constitute legal advice. Organizations should consult with qualified legal counsel to ensure full compliance with applicable regulations. Compliance requirements vary by jurisdiction, industry, and specific use cases.

**Note**: Regulations evolve over time. Always refer to the latest official guidance from regulatory bodies:
- GDPR: European Data Protection Board (EDPB)
- HIPAA: U.S. Department of Health & Human Services (HHS)
- PCI-DSS: PCI Security Standards Council

