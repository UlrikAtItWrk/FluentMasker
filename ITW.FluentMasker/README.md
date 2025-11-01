# FluentMasker

A powerful and flexible .NET library for masking sensitive data with a fluent API. Perfect for data privacy, GDPR compliance, logging, and secure data handling.

## Features

? **Fluent API** - Chain multiple masking rules in a readable, declarative style  
?? **Security-First** - Built-in ReDoS (Regular Expression Denial of Service) protection  
?? **Type-Safe** - Generic masking rules with automatic type conversion  
? **High Performance** - Compiled expression trees for 10x+ faster property access  
?? **Extensible** - Easy to create custom masking rules  
?? **Rich Rule Set** - 25+ built-in masking rules for common scenarios  

## Installation

```bash
dotnet add package ITW.FluentMasker
```

## Quick Start

### Basic Usage

```csharp
using ITW.FluentMasker;
using ITW.FluentMasker.Extensions;

// Create a masker for your model
public class PersonMasker : AbstractMasker<Person>
{
    public void Initialize()
    {
        // Simple masking
        MaskFor(x => x.FirstName, new MaskFirstRule(3));
        
        // Fluent API with chaining
        MaskFor(x => x.Email, m => m
            .KeepFirst(3)
            .MaskMiddle(2, 2)
            .KeepLast(4));
        
        // Built-in rules for common scenarios
        MaskFor(x => x.Phone, new PhoneMaskRule(keepLast: 4));
        MaskFor(x => x.CreditCard, new CardMaskRule());
    }
}

// Use the masker
var person = new Person 
{ 
    FirstName = "John",
    Email = "john.doe@example.com",
    Phone = "+1-555-123-4567",
    CreditCard = "4532123456789012"
};

var masker = new PersonMasker();
masker.Initialize();
masker.SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

var result = masker.Mask(person);
Console.WriteLine(result.MaskedData); 
// Output: {"FirstName":"***","Email":"joh******.com","Phone":"+*-***-***-4567","CreditCard":"4532********9012"}
```

## Core Concepts

### Property Rule Behaviors

Control how properties without explicit rules are handled:

- **`PropertyRuleBehavior.Remove`** - Exclude unmasked properties from output
- **`PropertyRuleBehavior.Exclude`** - Set unmasked properties to `null`
- **`PropertyRuleBehavior.Include`** - Include unmasked properties as-is

```csharp
masker.SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);
```

### Fluent API

Chain multiple rules for complex masking scenarios:

```csharp
MaskFor(x => x.Address, m => m
    .KeepFirst(4)
    .MaskMiddle(1, 1)
    .KeepLast(3));
```

## Built-in Masking Rules

### Position-Based Rules

```csharp
// Mask from start
MaskFor(x => x.Name, new MaskStartRule(3)); // "Hello" ? "***lo"

// Mask from end
MaskFor(x => x.Name, new MaskEndRule(3)); // "Hello" ? "He***"

// Mask first N characters
MaskFor(x => x.Name, new MaskFirstRule(2)); // "Hello" ? "**llo"

// Mask last N characters
MaskFor(x => x.Name, new MaskLastRule(2)); // "Hello" ? "Hel**"

// Keep first N characters
MaskFor(x => x.Name, new KeepFirstRule(2)); // "Hello" ? "He***"

// Keep last N characters
MaskFor(x => x.Name, new KeepLastRule(2)); // "Hello" ? "***lo"

// Mask middle section
MaskFor(x => x.Name, new MaskMiddleRule(2, 2)); // "Hello" ? "He*lo"

// Mask specific range
MaskFor(x => x.Name, new MaskRangeRule(1, 3)); // "Hello" ? "H***o"

// Mask by percentage
MaskFor(x => x.Name, new MaskPercentageRule(0.5, MaskFrom.End)); // "HelloWorld" ? "Hello*****"
```

### Format-Specific Rules

```csharp
// Email masking
MaskFor(x => x.Email, new EmailMaskRule()); // "user@example.com" ? "us**@example.com"

// Phone masking with separator preservation
MaskFor(x => x.Phone, new PhoneMaskRule(keepLast: 4, preserveSeparators: true));
// "+1 (555) 123-4567" ? "+* (***) ***-4567"

// Credit card masking
MaskFor(x => x.Card, new CardMaskRule()); // "4532123456789012" ? "4532********9012"

// IBAN masking
MaskFor(x => x.IBAN, new IBANMaskRule()); // "GB82WEST12345698765432" ? "GB82************5432"

// Template-based masking
MaskFor(x => x.SSN, new TemplateMaskRule("XXX-XX-{4}")); // "123-45-6789" ? "XXX-XX-6789"
```

### Regex-Based Rules

```csharp
// Regex replace with ReDoS protection
MaskFor(x => x.Text, new RegexReplaceRule(@"\d", "X"));
// "Order123" ? "OrderXXX"

// Mask specific regex groups
MaskFor(x => x.Phone, new RegexMaskGroupRule(@"(\d{3})-(\d{3})-(\d{4})", 2));
// "555-123-4567" ? "555-***-4567"
```

### Character-Based Rules

```csharp
// Whitelist characters (remove others)
MaskFor(x => x.Input, new WhitelistCharsRule("0123456789"));
// "Card: 1234-5678" ? "12345678"

// Blacklist characters (remove specific chars)
MaskFor(x => x.Input, new BlacklistCharsRule("-_ "));
// "123-456-789" ? "123456789"

// Mask specific character classes
MaskFor(x => x.Input, new MaskCharClassRule(CharClass.Digits));
// "Code: ABC123" ? "Code: ABC***"
```

### Data Modification Rules

```csharp
// Redact completely
MaskFor(x => x.Secret, new RedactRule("[REDACTED]"));

// Null out
MaskFor(x => x.Sensitive, new NullOutRule());

// Truncate
MaskFor(x => x.Description, new TruncateRule(50, "..."));

// Hash (one-way)
MaskFor(x => x.UserId, new HashRule(HashAlgorithm.SHA256));
```

### Numeric Rules

```csharp
// Round to nearest value
MaskFor(x => x.Salary, new RoundToRule(1000)); // 52750 ? 53000

// Add noise
MaskFor(x => x.Age, new NoiseAdditiveRule(-2, 2)); // 42 ? random between 40-44

// Bucketize values
MaskFor(x => x.Age, new BucketizeRule(new[] { 0, 18, 30, 50, 100 }));
// 25 ? "18-30"
```

### Date/Time Rules

```csharp
// Shift dates randomly
MaskFor(x => x.BirthDate, new DateShiftRule(-30, 30)); // Random ±30 days

// Round to time buckets
MaskFor(x => x.Timestamp, new TimeBucketRule(TimeSpan.FromHours(1)));
// 2024-01-15 14:37:22 ? 2024-01-15 14:00:00

// With offset for anonymity
MaskFor(x => x.Timestamp, new TimeBucketOffsetRule(TimeSpan.FromHours(1), TimeSpan.FromMinutes(17)));
```

## Extension Methods

Convenient extension methods for common scenarios:

```csharp
// Position-based
builder.MaskStart(3)
       .MaskEnd(2)
       .MaskMiddle(2, 2)
       .KeepFirst(4)
       .KeepLast(4)
       .MaskRange(2, 5);

// Character filtering
builder.WhitelistChars("0123456789")
       .WhitelistAlphanumeric()
       .WhitelistDigits()
       .BlacklistChars("-_ ");

// Numeric operations
builder.RoundTo(100)
       .AddNoise(-5, 5)
       .Bucketize(new[] { 0, 10, 20, 30 });

// Date/Time operations
builder.ShiftDate(-7, 7)
       .BucketTime(TimeSpan.FromHours(1));
```

## Advanced Features

### Type Conversion

Automatic conversion between types when masking non-string properties:

```csharp
// Mask numeric properties
MaskFor(x => x.Salary, new RoundToRule(1000));

// Mask DateTime properties
MaskFor(x => x.BirthDate, new DateShiftRule(-365, 365));
```

### Collection Masking

Mask entire collections with nested maskers:

```csharp
public class PersonMasker : AbstractMasker<Person>
{
    public void Initialize()
    {
        MaskFor(x => x.Pets, new MaskForEachRule<Pet>(new PetMasker()));
    }
}
```

### Custom Rules

Create your own masking rules by implementing `IMaskRule<TInput, TOutput>`:

```csharp
public class CustomMaskRule : IStringMaskRule
{
    public string Apply(string input)
    {
        // Your custom logic here
        return input?.ToUpper();
    }
}
```

## Security Features

### ReDoS Protection

Built-in protection against Regular Expression Denial of Service attacks:

```csharp
// Automatic 100ms timeout on regex operations
var rule = new RegexReplaceRule(@"\d+", "X");

// Custom timeout for complex patterns
var rule = new RegexReplaceRule(@"complex.*pattern", "X", TimeSpan.FromMilliseconds(200));
```

### Safe Defaults

- Null and empty string handling
- Input validation and argument checking
- Thread-safe singleton registries

## Requirements

- .NET 8.0 or higher
- Newtonsoft.Json 13.0.3+

## License

MIT License. See `https://github.com/UlrikAtItWrk/FluentMasker/LICENSE` file for details.


