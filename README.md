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

See the [FluentMasker - Complete Mask Rules Reference](./Docs/MaskRulesReference.md) for a detailed description of the Masking rules.

---

## Advanced Usage

### Deterministic Masking with Seed Providers

Ensure consistent masking across multiple runs using seed providers:

```csharp
var result = NumericMaskingBuilder.For(transaction)
    .WithRandomSeed(x => x.TransactionId.GetHashCode())  // Seed from ID
    .NoiseAdditive(maxAbs: 1000)
    .RoundTo(increment: 100)
    .Build();

// Same transaction ID always produces same masked value
```

### Chaining Multiple Rules

Apply multiple transformations in sequence:

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

Mask complex hierarchical data structures:

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

Control how unmapped properties are handled:

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

Handle masking errors gracefully:

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
