# Date & Age Masking for GDPR and HIPAA Compliance

## Overview

The `DateAgeMaskRule` provides comprehensive date and age masking capabilities designed for GDPR and HIPAA Safe Harbor compliance. It supports multiple masking strategies and age generalization techniques.

## GDPR & HIPAA Compliance

### GDPR
Dates of birth and other personal dates are considered personal data under GDPR. This rule provides multiple strategies to minimize data while maintaining analytical utility.

### HIPAA Safe Harbor (45 CFR §164.514(b)(2))
- All elements of dates (except year) must be removed or generalized
- Ages over 89 must be aggregated to "90+" category
- Dates can be shifted by a consistent random offset (±365 days) per individual

## Masking Modes

### 1. Year-Only Mode (Default)
Keeps the year and masks month/day with asterisks.

```csharp
var rule = new DateAgeMaskRule();
var result = rule.Apply("1982-11-23");
// Result: "1982-**-**"

// Also handles timestamps
rule.Apply("2023-09-18T14:23:00Z");
// Result: "2023-**-**"
```

**HIPAA Compliant:** Yes - Removes all date elements except year

### 2. Date Shift Mode
Shifts dates by a consistent random offset. Requires seed provider for HIPAA compliance.

```csharp
var patientId = "patient-12345";

var rule = new DateAgeMaskRule(
    mode: DateAgeMaskRule.MaskingMode.DateShift,
    daysRange: 180); // ±180 days
    
rule.SeedProvider = value => patientId.GetHashCode();

// All dates for this patient shifted by same amount
var admission = rule.Apply("2023-01-15");    // e.g., "2023-06-12"
var discharge = rule.Apply("2023-01-20");    // e.g., "2023-06-17"
// Duration preserved: still 5 days apart
```

**HIPAA Compliant:** Yes - When used with consistent seed per individual  
**Key Feature:** Preserves temporal relationships (durations, ordering)

### 3. Redact Mode
Completely removes the date for maximum privacy.

```csharp
var rule = new DateAgeMaskRule(DateAgeMaskRule.MaskingMode.Redact);
var result = rule.Apply("1982-11-23");
// Result: "[REDACTED]"
```

**HIPAA Compliant:** Yes - Maximum privacy, minimum utility

## Age Masking

### Basic Age Masking (HIPAA 90+ Rule)
Ages ?90 always become "90+" regardless of settings.

```csharp
var rule = new DateAgeMaskRule(ageBucketing: false);

rule.ApplyAge(42);   // "42"
rule.ApplyAge(89);   // "89"
rule.ApplyAge(94);   // "90+"  ? HIPAA compliant
```

### Age Bucketing
Groups ages into ranges for additional privacy.

```csharp
var rule = new DateAgeMaskRule(ageBucketing: true);

rule.ApplyAge(7);    // "6-10"
rule.ApplyAge(42);   // "41-50"
rule.ApplyAge(85);   // "81-89"
rule.ApplyAge(94);   // "90+"
```

**Standard Buckets:**
- 0-5, 6-10, 11-20, 21-30, 31-40, 41-50, 51-60, 61-70, 71-80, 81-89, 90+

### Custom Age Buckets
Define your own age ranges (e.g., for pediatric data).

```csharp
var rule = new DateAgeMaskRule(
    ageBucketing: true,
    customAgeBreaks: new[] { 0, 1, 3, 6, 13, 18, 150 },
    customAgeLabels: new[] { "Infant", "Toddler", "Preschool", 
                              "School-age", "Teen", "Adult" });

rule.ApplyAge(0);    // "Infant"
rule.ApplyAge(2);    // "Toddler"
rule.ApplyAge(15);   // "Teen"
rule.ApplyAge(94);   // "Adult" (custom buckets can override 90+ rule)
```

### Calculate Age from Date of Birth

```csharp
var rule = new DateAgeMaskRule(ageBucketing: true);

var dob = new DateTime(1982, 11, 23);
var maskedAge = rule.CalculateAndMaskAge(dob);
// Result: "41-50" (as of 2025)

// With specific reference date
var referenceDate = new DateTime(2024, 11, 10);
maskedAge = rule.CalculateAndMaskAge(dob, referenceDate);
// Result: "41-50"
```

## Fluent API Usage

### With AbstractMasker

```csharp
using ITW.FluentMasker;
using ITW.FluentMasker.Extensions;

public class PatientData
{
    public string DateOfBirth { get; set; }
    public string AdmissionDate { get; set; }
    public string DischargeDate { get; set; }
}

public class PatientMasker : AbstractMasker<PatientData>
{
    private readonly string _patientId;

    public PatientMasker(string patientId)
    {
        _patientId = patientId;
    }

    protected override void Initialize()
    {
        // Year-only for date of birth (HIPAA compliant)
        MaskFor(x => x.DateOfBirth, m => m.DateAgeMask());

        // Date shifting with consistent offset for admission/discharge
        // Preserves the length of stay
        MaskFor(x => x.AdmissionDate, m => m
            .WithRandomSeed(str => _patientId.GetHashCode())
            .DateAgeMask("date-shift", daysRange: 180));

        MaskFor(x => x.DischargeDate, m => m
            .WithRandomSeed(str => _patientId.GetHashCode())
            .DateAgeMask("date-shift", daysRange: 180));
    }
}

// Usage
var masker = new PatientMasker("patient-12345");
var patient = new PatientData
{
    DateOfBirth = "1945-06-15",
    AdmissionDate = "2024-11-10",
    DischargeDate = "2024-11-15"
};

var masked = masker.Mask(patient);
// DateOfBirth: "1945-**-**"
// AdmissionDate & DischargeDate: shifted by same amount, 5-day stay preserved
```

## Real-World Healthcare Scenario

```csharp
public class PatientRecord
{
    public string PatientId { get; set; }
    public string DateOfBirth { get; set; }
    public string InitialConsultation { get; set; }
    public string DiagnosisDate { get; set; }
    public string TreatmentStart { get; set; }
    public string FollowUp1 { get; set; }
    public string FollowUp2 { get; set; }
}

public class HealthcareMasker : AbstractMasker<PatientRecord>
{
    protected override void Initialize()
    {
        // Year-only for DOB (HIPAA requirement)
        MaskFor(x => x.DateOfBirth, m => m.DateAgeMask());

        // Consistent date shifting for all clinical dates per patient
        // This preserves the patient journey timeline
        MaskFor(x => x.InitialConsultation, m => m
            .WithRandomSeed(str => GetPatientIdSeed())
            .DateAgeMask("date-shift", daysRange: 180));

        MaskFor(x => x.DiagnosisDate, m => m
            .WithRandomSeed(str => GetPatientIdSeed())
            .DateAgeMask("date-shift", daysRange: 180));

        MaskFor(x => x.TreatmentStart, m => m
            .WithRandomSeed(str => GetPatientIdSeed())
            .DateAgeMask("date-shift", daysRange: 180));

        MaskFor(x => x.FollowUp1, m => m
            .WithRandomSeed(str => GetPatientIdSeed())
            .DateAgeMask("date-shift", daysRange: 180));

        MaskFor(x => x.FollowUp2, m => m
            .WithRandomSeed(str => GetPatientIdSeed())
            .DateAgeMask("date-shift", daysRange: 180));
    }

    private int GetPatientIdSeed()
    {
        // Access the current patient ID during masking
        // This ensures all dates for the same patient use the same seed
        return CurrentData?.PatientId?.GetHashCode() ?? 0;
    }
}

// Usage
var masker = new HealthcareMasker();
var record = new PatientRecord
{
    PatientId = "patient-001",
    DateOfBirth = "1945-06-15",
    InitialConsultation = "2024-11-10",
    DiagnosisDate = "2024-11-20",
    TreatmentStart = "2024-12-01",
    FollowUp1 = "2024-12-15",
    FollowUp2 = "2025-01-10"
};

var masked = masker.Mask(record);

// Benefits:
// 1. DOB year only: "1945-**-**" (HIPAA compliant)
// 2. All clinical dates shifted by SAME offset (e.g., +73 days)
// 3. Chronological order preserved
// 4. Durations preserved (e.g., 10 days between consult and diagnosis)
// 5. Shift within ±180 days (HIPAA compliant)
```

## Supported Date Formats

The rule automatically handles various common date formats:

- **ISO 8601**: `1982-11-23`, `2023-09-18T14:23:00Z`
- **US Format**: `11/23/1982`, `11-23-1982`
- **European Format**: `23.11.1982`, `23/11/1982`, `23-11-1982`
- **Timestamps**: Full ISO 8601 with timezone

Invalid dates are returned unchanged (graceful degradation).

## Customization Options

```csharp
var rule = new DateAgeMaskRule(
    mode: DateAgeMaskRule.MaskingMode.YearOnly,  // YearOnly, DateShift, or Redact
    daysRange: 180,                               // For DateShift mode
    ageBucketing: true,                           // Enable age ranges
    customAgeBreaks: new[] { 0, 18, 65, 150 },   // Custom age boundaries
    customAgeLabels: new[] { "Minor", "Adult", "Senior" },
    maskChar: "X",                                // Custom mask character
    separator: "/"                                // Custom date separator
);

rule.Apply("1982-11-23");
// Result: "1982/XX/XX" (with custom separator and mask char)
```

## Performance

The `DateAgeMaskRule` is highly optimized:

- **Date masking**: >50,000 operations/second
- **Age bucketing**: >100,000 operations/second
- Uses binary search for O(log n) bucket lookups
- Cryptographically secure randomness for non-deterministic mode

## Best Practices

### 1. HIPAA Safe Harbor Compliance Checklist

? **For DOB**: Use year-only mode
```csharp
MaskFor(x => x.DateOfBirth, m => m.DateAgeMask());
```

? **For Ages ?90**: Automatically masked as "90+"
```csharp
rule.ApplyAge(94); // Always returns "90+"
```

? **For Clinical Dates**: Use date-shift with consistent seed
```csharp
MaskFor(x => x.AdmissionDate, m => m
    .WithRandomSeed(str => patientId.GetHashCode())
    .DateAgeMask("date-shift", daysRange: 180));
```

? **Preserve Temporal Relationships**: Use same seed for all dates of one patient

### 2. GDPR Compliance Strategies

**Minimization**: Use year-only mode
```csharp
MaskFor(x => x.DateOfBirth, m => m.DateAgeMask());
```

**Pseudonymization**: Use date-shift with deterministic seed
```csharp
MaskFor(x => x.Date, m => m
    .WithRandomSeed(str => subjectId.GetHashCode())
    .DateAgeMask("date-shift", daysRange: 90));
```

**Anonymization**: Use redact mode or age bucketing
```csharp
MaskFor(x => x.SensitiveDate, m => m.DateAgeMask("redact"));
// OR
MaskFor(x => x.Age, m => m.DateAgeMask(ageBucketing: true));
```

### 3. Analytics Preservation

When you need to maintain analytical utility:

- **Use date-shift mode** to preserve temporal patterns
- **Use age bucketing** for demographic analysis
- **Use consistent seeds per individual** to maintain relationships

### 4. Batch Processing

For consistent age calculation across a dataset:

```csharp
var referenceDate = DateTime.Today; // Fix reference date
var rule = new DateAgeMaskRule(ageBucketing: true);

foreach (var record in records)
{
    record.Age = rule.CalculateAndMaskAge(record.DateOfBirth, referenceDate);
}
```

## Error Handling

The rule is designed for graceful degradation:

```csharp
// Invalid dates return unchanged
rule.Apply("INVALID");     // Returns "INVALID"
rule.Apply("");            // Returns ""
rule.Apply(null);          // Returns null

// Unusual ages handled gracefully
rule.ApplyAge(-1);         // Returns "0-5" (with bucketing)
rule.ApplyAge(200);        // Returns "90+" (with HIPAA rule)
```

## Extension Method

The fluent API extension makes it easy to use with the builder pattern:

```csharp
using ITW.FluentMasker.Extensions;

// In your masker class
MaskFor(x => x.DateOfBirth, m => m.DateAgeMask());

// With date shifting
MaskFor(x => x.AdmissionDate, m => m
    .WithRandomSeed(str => patientId.GetHashCode())
    .DateAgeMask("date-shift", daysRange: 180));

// With custom options
MaskFor(x => x.Date, m => m.DateAgeMask(
    mode: "year-only",
    ageBucketing: true,
    maskChar: "X",
    separator: "/"));
```

## See Also

- [HIPAA Safe Harbor De-identification](https://www.hhs.gov/hipaa/for-professionals/privacy/special-topics/de-identification/index.html)
- [GDPR Article 5 - Data Minimization](https://gdpr-info.eu/art-5-gdpr/)
- `DateShiftRule` - For DateTime-based date shifting
- `BucketizeRule` - For general bucketing of numeric values
- [FluentMasker GitHub Repository](https://github.com/UlrikAtItWrk/FluentMasker)
