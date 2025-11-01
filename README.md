# FluentMasker

A high-performance, fluent API library for .NET that provides comprehensive data masking and privacy-preserving transformations. Built for compliance with GDPR, HIPAA, CCPA, and PCI-DSS requirements.

[![.NET Version](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/UlrikAtItWrk/FluentMasker)
[![NuGet](https://img.shields.io/badge/nuget-v2.0.4-blue)](https://www.nuget.org/packages/ITW.FluentMasker)

---

## Quick Start

**New to FluentMasker?** Start with our comprehensive **[Getting Started Guide](./Docs/GettingStarted.md)** - Get up and running in 5 minutes!

The guide includes:
- Installation and setup
- Your first masker in 5 minutes
- Core concepts explained
- Common use cases and patterns
- Troubleshooting and testing

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Documentation](#documentation)
- [Masking Rules](#masking-rules)
- [Advanced Usage](#advanced-usage)
- [Performance](#performance)
- [Compliance & Security](#compliance--security)
- [Contributing](#contributing)
- [License](#license)

---

## Overview

FluentMasker is a powerful data masking library designed to help you protect sensitive information while maintaining data utility for testing, analytics, and compliance purposes. It supports over 30 masking strategies across multiple categories:

- **String Masking**: Pattern-based character masking
- **Format-Preserving Masking**: Domain-aware masking (email, phone, credit cards, IBAN)
- **Numeric & Statistical Masking**: Privacy-preserving transformations
- **Cryptographic Masking**: Hash-based pseudonymization
- **Temporal Masking**: Date shifting and bucketing for HIPAA compliance

### Why FluentMasker?

* **Type-Safe**: Generic interfaces ensure compile-time type safety
* **High Performance**: 40,000+ operations/second with zero-allocation optimizations
* **Fluent API**: Intuitive, chainable method syntax
* **Compliance-Ready**: GDPR, HIPAA, CCPA, and PCI-DSS compliant transformations
* **Extensible**: Easy to add custom masking rules
* **Well-Tested**: Comprehensive unit test coverage

---

## Features

### Core Capabilities

- **30+ Built-in Mask Rules** across 5 categories
- **Fluent Builder API** for intuitive masking configuration
- **Chain Multiple Rules** on a single property
- **Nested Object Support** for complex data structures
- **Deterministic Masking** with seed providers for consistent output
- **Format Validation** ensures masked data remains valid (IBAN, credit cards, etc.)
- **Graceful Degradation** for invalid inputs
- **Zero External Dependencies** (except Newtonsoft.Json)

### Performance Optimizations

- **ArrayPool\<char\>** for zero-allocation string operations
- **Compiled Expression Trees** for fast property access
- **Cached Regex Patterns** for format-preserving rules
- **Span\<T\>** support for memory-efficient operations

---

## Documentation

### Essential Guides
- **[Getting Started Guide](./Docs/GettingStarted.md)** - 5-minute tutorial and core concepts ⭐ **START HERE**

### Integration Guides
- **[Serilog Integration](./Docs/SerilogIntegration.md)** - Automatic masking with IDestructuringPolicy
- **[ILogger Integration](./Docs/ILoggerIntegration.md)** - Using with Microsoft.Extensions.Logging

### Specialized Topics
- **[Compliance Guide](./Docs/ComplianceGuide.md)** - GDPR, HIPAA, and PCI-DSS implementation patterns ⚖️ **COMPLIANCE**
- **[DateShiftRule Documentation](./Docs/DateAgeMaskRule.md)** - HIPAA-compliant date masking
- **[NationalIdMaskRule Documentation](./Docs/NationalIdMaskRule.md)** - Country-specific ID masking

### 💡 Sample Projects
- **[Serilog Destructuring Sample](./ITW.FluentMasker.Serilog.Destructure.Sample/)** - Complete working examples
  - Person masking (PII)
  - Credit card masking (PCI-DSS)
  - Health record masking (HIPAA)
  - Multiple object types

---

## Masking Rules

FluentMasker includes **30+ built-in masking rules** organized into 7 categories: position-based masking, format-specific rules (email, phone, cards), cryptographic operations, date/time transformations, pattern-based rules, character filtering, and collection handling. Each rule is optimized for performance and includes comprehensive examples.

**Rule Categories:**
- **Position-Based** (10 rules): MaskStart, MaskEnd, MaskMiddle, MaskRange, KeepFirst, KeepLast, etc.
- **Format-Specific** (6 rules): EmailMask, PhoneMask, CardMask, IBANMask, NationalIdMask, URLMask
- **Cryptographic** (4 rules): HashRule (SHA256/SHA512), NoiseAdditive, RoundTo, Bucketize
- **Date & Time** (4 rules): DateAgeMask, DateShift, TimeBucket, TimeBucketOffset
- **Pattern-Based** (3 rules): RegexMaskGroup, RegexReplace, TemplateMask
- **Character Class** (3 rules): MaskCharClass, BlacklistChars, WhitelistChars
- **Collection** (1 rule): MaskForEach

📖 **See the [Complete Mask Rules Reference](./Docs/MaskRulesReference.md)** for detailed documentation including:
- Full parameter descriptions and examples for each rule
- Compliance applicability (GDPR/HIPAA/PCI-DSS)
- Performance characteristics and benchmarks
- Code examples and real-world use cases
- Quick reference comparison table

---

## Advanced Usage

### Deterministic Masking with Seed Providers

By default, rules like `NoiseAdditive` and `DateShift` use random values, producing **different outputs each time**. For many use cases—analytics, testing, or HIPAA compliance—you need **consistent, reproducible masking** where the same input always produces the same masked output. Seed providers solve this by deriving random seeds from record identifiers.

**Why Deterministic Masking?**
- **Analytics**: Aggregate masked data across multiple exports without duplicates
- **HIPAA Compliance**: Shift all dates for a patient by the same offset (preserves temporal relationships)
- **Testing**: Reproducible test data that remains consistent across test runs
- **Searchability**: Hash the same email twice to find matching records in masked datasets
- **Data Joins**: Join masked tables by pseudonymized keys

**How It Works:**
- Provide a seed function that extracts a unique identifier (e.g., `TransactionId`, `PatientId`)
- FluentMasker uses the identifier's hash code to seed the random number generator
- Same identifier → same seed → same "random" output (deterministic randomness)
- Different identifiers still get different masked values

**Example - Consistent Transaction Masking:**

```csharp
var result = NumericMaskingBuilder.For(transaction)
    .WithRandomSeed(x => x.TransactionId.GetHashCode())  // Seed from ID
    .NoiseAdditive(maxAbs: 1000)
    .RoundTo(increment: 100)
    .Build();

// Same transaction ID always produces same masked value
```

### Chaining Multiple Rules

FluentMasker's true power emerges when you **compose multiple masking rules into a pipeline**. Each rule receives the output of the previous rule, allowing you to build sophisticated anonymization strategies from simple, focused operations. Order matters—rules execute left-to-right in the chain.

**Why Chain Rules?**
- **Layered Privacy**: Combine noise addition + rounding for statistical privacy
- **Format Then Mask**: Normalize data before applying masking rules
- **Privacy + Utility**: Balance data protection with analytical usefulness
- **Complex Transformations**: Break down complex requirements into simple, testable steps
- **Compliance**: Meet multiple regulatory requirements simultaneously (e.g., HIPAA + statistical anonymization)

**Common Patterns:**
- **Noise → Round**: Add randomness, then reduce precision for k-anonymity
- **Mask → Truncate**: Partially hide data, then limit length for display
- **Hash → Prefix**: Pseudonymize, then extract prefix for bucketing
- **Shift → Bucket**: Move dates, then group into time ranges

**Design Principle:**
Each rule does **one thing well**. Chaining lets you combine them declaratively without writing complex custom logic.

**Example - Multi-Step Transformations:**

```csharp
masker.MaskFor(x => x.Salary, m => m
    .NoiseAdditive(maxAbs: 5000)     // Add noise first
    .RoundTo(increment: 1000)         // Then round
);

masker.MaskFor(x => x.Name, m => m
    .MaskMiddle(keepFirst: 1, keepLast: 1)  // Mask middle
    .KeepFirst(5)                            // Then keep only first 5
);
```

### Nested Object Masking

Real-world data models are rarely flat—they contain **collections, nested objects, and complex hierarchies**. FluentMasker handles this elegantly with `MaskForEachRule<T>`, which applies type-specific maskers to child objects while maintaining full type safety and composability.

**How It Works:**
- Create individual maskers for each type in your object graph
- Use `MaskForEachRule<T>` to apply maskers to nested properties
- Works with collections (`List<T>`, `IEnumerable<T>`, arrays) and single nested objects
- Maskers are composable—build complex masking strategies from simple, reusable components

**Benefits:**
- **Type-Safe**: Compile-time verification of property access and masking rules
- **Reusable**: Define `EmployeeMasker` once, use it across multiple parent objects
- **Maintainable**: Changes to child masking logic automatically propagate
- **Testable**: Unit test each masker independently

**Common Scenarios:**
- **Order → OrderItems → Product**: E-commerce transaction masking
- **Company → Employees → Address**: Organizational data hierarchies
- **Patient → Visits → Medications**: Healthcare record de-identification
- **API DTOs**: Complex nested response objects for logging

**Example - Complex Hierarchical Masking:**

```csharp
public class CompanyMasker : AbstractMasker<Company>
{
    public CompanyMasker()
    {
        MaskFor(x => x.Name, new MaskEndRule(3, "*"));

        // Mask each employee in the collection
        MaskFor(x => x.Employees, new MaskForEachRule<Employee>(
            new EmployeeMasker()
        ));

        // Mask nested address object
        MaskFor(x => x.Address, new MaskForEachRule<Address>(
            new AddressMasker()
        ));
    }
}
```

### Property Rule Behavior

FluentMasker provides fine-grained control over **properties without explicit masking rules** through the `PropertyRuleBehavior` setting. This is critical for security and compliance, allowing you to implement **data minimization** (GDPR Article 5) and prevent accidental exposure of sensitive fields.

**Available Behaviors:**
- **`Remove`** (Recommended): Excludes unmapped properties from output entirely - best for zero-trust security
- **`Exclude`**: Sets unmapped properties to `null` - useful when schema must be preserved
- **`Include`**: Passes unmapped properties through unchanged - ⚠️ use with caution, only for non-sensitive data

**Use Cases:**
- **Compliance**: Ensure only explicitly approved fields are logged (SOC 2, ISO 27001)
- **Security**: Prevent data leaks from newly added properties that haven't been reviewed
- **Testing**: Quickly redact all PII while keeping operational data
- **API Responses**: Control exactly which fields external systems receive

**Example - Selective Property Masking:**

```csharp
public class SelectiveMasker : AbstractMasker<Person>
{
    public SelectiveMasker()
    {
        // Only include explicitly masked properties
        PropertyRuleBehavior = PropertyRuleBehavior.Exclude;

        MaskFor(x => x.SSN, new RedactRule());
        MaskFor(x => x.Email, new EmailMaskRule());

        // All other properties excluded from output
    }
}
```

### Error Handling

FluentMasker uses a **non-failing by default** strategy to ensure data processing continues even when individual property masking fails. All masking operations return a `MaskingResult` object that includes success status and detailed error information, allowing you to handle failures gracefully without losing the entire dataset.

**Key Benefits:**
- Partial masking succeeds even if some properties fail
- Detailed error messages for debugging
- Production-safe: one bad field doesn't break the entire pipeline
- Audit trail: all failures are logged in the result

**Example - Graceful Error Handling:**

```csharp
var masker = new PersonMasker();
var result = masker.Mask(person);

if (!result.IsSuccess)
{
    Console.WriteLine("Masking encountered errors:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"- {error}");
    }
}
else
{
    // Process masked data
    var json = result.MaskedData;
    SaveToDatabase(json);
}
```

---

## Performance

FluentMasker is built for high-throughput scenarios with enterprise-grade performance:

### Benchmark Results

| Operation | Throughput | Latency (p99) | Memory |
|-----------|------------|---------------|---------|
| **MaskStart** (simple) | 42.5M ops/sec | 23.5 ns | 39 B/op |
| **EmailMask** | 500K ops/sec | 2 μs | 512 B/op |
| **CardMask** (Luhn validation) | 250K ops/sec | 4 μs | 768 B/op |
| **IBANMask** (ISO validation) | 40K ops/sec | 25 μs | 1.2 KB/op |
| **Hash** (SHA256) | 100K ops/sec | 10 μs | 384 B/op |
| **NoiseAdditive** | 1M ops/sec | 1 μs | 64 B/op |

### Performance Features

- **Zero-Allocation String Operations**: Uses `ArrayPool<char>` to minimize GC pressure
- **Compiled Expression Trees**: Property access compiled once, reused for all calls
- **Cached Regex Patterns**: Pre-compiled patterns for format-preserving rules
- **SIMD Optimizations**: Where applicable in .NET 8.0
- **Minimal Boxing**: Generic interfaces eliminate boxing for value types

### Running Benchmarks

```bash
cd src/ITW.FluentMasker.Benchmarks
dotnet run -c Release
```

---

## Compliance & Security

FluentMasker is designed to help organizations meet regulatory compliance requirements:

> **📖 For comprehensive compliance implementation patterns, see the [Compliance Guide](src/Docs/ComplianceGuide.md)** which covers:
> - **GDPR**: Data minimization, pseudonymization, right to erasure, and safe logging
> - **HIPAA**: Safe Harbor Method (all 18 identifiers), temporal preservation, PHI masking
> - **PCI-DSS**: Credit card masking, CVV handling, transaction logging, and CDE protection
> 
> Includes 20+ real-world code examples, compliance checklists, and cross-regulation scenarios.

### GDPR (General Data Protection Regulation)

* **Pseudonymization**: Hash rule provides GDPR Article 32 compliant pseudonymization
* **Right to be Forgotten**: NullOut and Redact rules for data deletion
* **Data Minimization**: Selective masking reduces PII exposure
* **Privacy by Design**: Default behaviors protect sensitive data

**Example:**

```csharp
// GDPR-compliant user data export
public class GDPRUserMasker : AbstractMasker<User>
{
    public GDPRUserMasker()
    {
        // Pseudonymization (Article 32)
        MaskFor(x => x.Email, new HashRule(
            HashAlgorithmType.SHA256,
            SaltMode.Static,
            OutputFormat.Hex
        ));

        // Data minimization
        MaskFor(x => x.IPAddress, new IPMaskRule(cidr: 24));

        // Deletion compliance
        MaskFor(x => x.SSN, new NullOutRule());
    }
}
```

### HIPAA (Health Insurance Portability and Accountability Act)

* **Safe Harbor Method**: DateShiftRule implements 45 CFR §164.514(b)(2)
* **De-identification**: 18 HIPAA identifiers can be masked
* **Consistent Shifting**: Deterministic date shifting per patient

**Example:**

```csharp
// HIPAA Safe Harbor compliant date masking
public class HIPAAPatientMasker : AbstractMasker<Patient>
{
    public HIPAAPatientMasker()
    {
        // Dates shifted consistently per patient (±180 days)
        MaskFor(x => x.AdmissionDate, m => m
            .WithRandomSeed(p => p.PatientId.GetHashCode())
            .DateShift(daysRange: 180, preserveTime: true)
        );

        MaskFor(x => x.DischargeDate, m => m
            .WithRandomSeed(p => p.PatientId.GetHashCode())
            .DateShift(daysRange: 180, preserveTime: true)
        );

        // Geographic precision (3-digit ZIP only)
        MaskFor(x => x.ZipCode, m => m
            .KeepFirst(3)
            .MaskEnd(count: 2, mask: "0")
        );

        // IP address masking
        MaskFor(x => x.LastAccessIP, new IPMaskRule(cidr: 24));
    }
}
```

### PCI-DSS (Payment Card Industry Data Security Standard)

* **Requirement 3.3**: CardMask shows last 4 digits only
* **Requirement 3.4**: Renders PAN unreadable via masking
* **Luhn Validation**: Ensures card numbers are valid before masking

**Example:**

```csharp
// PCI-DSS compliant card masking
masker.MaskFor(x => x.CardNumber, m => m.CardMask(
    keepFirst: 0,           // Hide BIN
    keepLast: 4,            // Show last 4 only (PCI-DSS Requirement 3.3)
    preserveGrouping: true,
    validateLuhn: true      // Validate before masking
));
// "4532 0151 1283 0366" → "**** **** **** 0366"
```

### CCPA (California Consumer Privacy Act)

* **Consumer Rights**: Support for data deletion and anonymization
* **De-identification**: Statistical masking for analytics while protecting privacy

### Security Best Practices

- **Cryptographically Secure RNG**: Uses `RandomNumberGenerator` for all random operations
- **Constant-Time Comparisons**: Prevents timing attacks in hash operations
- **Input Validation**: All format-preserving rules validate inputs
- **No Plaintext Logs**: Sensitive data never logged in plaintext
- **Memory Clearing**: Sensitive buffers cleared after use

---

## Documentation

### Usage Examples

#### Example 1: API Response Logging

```csharp
public class APILogMasker : AbstractMasker<APILog>
{
    public APILogMasker()
    {
        // Mask sensitive query parameters
        MaskFor(x => x.RequestURL, new URLMaskRule(
            maskQueryKeys: new[] { "token", "apiKey", "secret", "password" }
        ));

        // Mask client IP (keep subnet for debugging)
        MaskFor(x => x.ClientIP, new IPMaskRule(cidr: 24));

        // Round response times for privacy
        MaskFor(x => x.ResponseTimeMs, new RoundToRule<int>(increment: 10));
    }
}
```

#### Example 2: Analytics Data Export

```csharp
public class AnalyticsMasker : AbstractMasker<Transaction>
{
    public AnalyticsMasker()
    {
        // Deterministic noise for consistent aggregations
        MaskFor(x => x.Amount, m => m
            .WithRandomSeed(t => t.TransactionId.GetHashCode())
            .NoiseAdditive(maxAbs: 10.0)
            .RoundTo(increment: 5.0m)
        );

        // Age bucketing for k-anonymity
        MaskFor(x => x.CustomerAge, m => m.Bucketize(
            breaks: new[] { 0, 18, 25, 35, 45, 55, 65, 100 },
            labels: new[] { "<18", "18-24", "25-34", "35-44", "45-54", "55-64", "65+" }
        ));

        // Time bucketing to hour granularity
        MaskFor(x => x.Timestamp, m => m.TimeBucket(Granularity.Hour));
    }
}
```

#### Example 3: Database Export for Testing

```csharp
public class TestDataMasker : AbstractMasker<Customer>
{
    public TestDataMasker()
    {
        // Hash PII for pseudonymization
        MaskFor(x => x.Email, m => m.Hash(
            algorithm: "SHA256",
            saltMode: "static",
            outputFormat: "hex"
        ));

        // Mask phone numbers
        MaskFor(x => x.Phone, m => m.PhoneMask(keepLast: 4));

        // Mask credit cards (PCI-DSS)
        MaskFor(x => x.PaymentCard, m => m.CardMask(keepLast: 4));

        // Generalize addresses
        MaskFor(x => x.Address, m => m.MaskMiddle(keepFirst: 10, keepLast: 0));

        // Preserve age demographics with bucketing
        MaskFor(x => x.Age, m => m.Bucketize(
            breaks: new[] { 0, 18, 30, 50, 70, 100 },
            labels: new[] { "minor", "young-adult", "adult", "senior", "elderly" }
        ));
    }
}
```

---

## Contributing

Contributions are welcome! Please follow these guidelines:

### Development Setup

```bash
# Clone the repository
git clone https://github.com/UlrikAtItWrk/FluentMasker.git
cd FluentMasker

# Build the solution
cd src
dotnet build ITW.FluentMasker.sln

# Run unit tests
dotnet test ITW.FluentMasker.UnitTests/ITW.FluentMasker.UnitTests.csproj

# Run benchmarks
dotnet run -c Release --project ITW.FluentMasker.Benchmarks
```

### Adding New Mask Rules

1. Create a new class implementing `IMaskRule<TInput, TOutput>` in `src/ITW.FluentMasker/MaskRules/`
2. Add comprehensive XML documentation
3. Create unit tests in `src/ITW.FluentMasker.UnitTests/`
4. Add builder extension method in `src/ITW.FluentMasker/Extensions/`
5. Update documentation

Example:

```csharp
/// <summary>
/// Your custom mask rule description
/// </summary>
public class CustomMaskRule : IStringMaskRule
{
    public string Apply(string input)
    {
        // Implementation
    }
}
```

### Code Quality Standards

- **Unit Tests**: Minimum 90% code coverage
- **Performance**: Meet or exceed benchmark targets
- **Documentation**: Comprehensive XML docs with examples
- **Naming**: Follow C# naming conventions
- **SOLID Principles**: Maintain clean architecture

### Pull Request Process

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
