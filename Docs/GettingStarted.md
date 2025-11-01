# Getting Started with FluentMasker

Welcome to FluentMasker! This guide will help you get up and running with data masking in just a few minutes.

## What is FluentMasker?

FluentMasker is a powerful .NET library for masking sensitive data with a fluent, type-safe API. It helps you comply with GDPR, HIPAA, and PCI-DSS requirements while keeping your data usable for development, testing, and analytics.

### Key Features

* **Fluent API** - Chain multiple masking rules in readable, declarative style  
* **Security-First** - Built-in ReDoS protection for regex operations  
* **High Performance** - Compiled expression trees for 10x+ faster property access  
* **Type-Safe** - Generic masking rules with compile-time validation  
* **Extensible** - Easy to create custom masking rules  
* **Rich Rule Set** - 25+ built-in masking rules for common scenarios

## Installation

### Using .NET CLI

```bash
dotnet add package ITW.FluentMasker
```

### Using Package Manager Console

```powershell
Install-Package ITW.FluentMasker
```

### Using Visual Studio

1. Right-click on your project in Solution Explorer
2. Select "Manage NuGet Packages..."
3. Search for "ITW.FluentMasker"
4. Click "Install"

### Requirements

- .NET 8.0 or higher
- Newtonsoft.Json 13.0.3+ (automatically installed as dependency)

## Your First Masker (5-Minute Tutorial)

Let's create a simple example that masks personal information.

### Step 1: Create Your Model

```csharp
public class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string SSN { get; set; }
}
```

### Step 2: Create a Masker Class

```csharp
using ITW.FluentMasker;
using ITW.FluentMasker.MaskRules;

public class PersonMasker : AbstractMasker<Person>
{
    public PersonMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Remove properties without explicit masking rules from output
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // Mask first name - keep first 2 characters
        MaskFor(x => x.FirstName, m => m.MaskStart(2));

        // Email - mask local part, keep domain
        MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule(
            localKeep: 2, 
            domainStrategy: EmailDomainStrategy.KeepFull
        ));

        // Phone - show last 4 digits, preserve format
        MaskFor(x => x.Phone, (IMaskRule)new PhoneMaskRule(
            keepLast: 4, 
            preserveSeparators: true
        ));

        // SSN - completely redact
        MaskFor(x => x.SSN, (IMaskRule)new RedactRule("[REDACTED]"));
    }
}
```

### Step 3: Use the Masker

```csharp
// Create your data
var person = new Person
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john.doe@example.com",
    Phone = "+1 (555) 123-4567",
    SSN = "123-45-6789"
};

// Apply masking
var masker = new PersonMasker();
var result = masker.Mask(person);

// Get masked JSON output
Console.WriteLine(result.MaskedData);
// Output: {"FirstName":"**hn","Email":"jo**@example.com","Phone":"+* (***) ***-4567","SSN":"[REDACTED]"}

// Check if masking was successful
if (result.IsSuccess)
{
    Console.WriteLine("✅ Masking successful!");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"❌ Error: {error}");
    }
}
```

## Core Concepts

### 1. AbstractMasker<T>

The base class for all maskers. Inherit from it to create type-specific masking logic.

```csharp
public class PersonMasker : AbstractMasker<Person>
{
    // Your masking logic here
}
```

### 2. MaskFor Method

Defines masking rules for specific properties. Three ways to use it:

#### A) Using Fluent Builder (Extension Methods)

```csharp
MaskFor(x => x.FirstName, m => m.MaskStart(2).MaskEnd(2));
```

#### B) Using Rule Classes

```csharp
MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule(localKeep: 2));
```

#### C) Using Func for Complex Rules

```csharp
MaskFor(x => x.Address, m => m
    .KeepFirst(4)
    .MaskMiddle(2, 2)
    .KeepLast(3));
```

### 3. PropertyRuleBehavior

Controls how properties **without** explicit masking rules are handled:

```csharp
// Remove unmasked properties from output (most secure)
SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

// Set unmasked properties to null
SetPropertyRuleBehavior(PropertyRuleBehavior.Exclude);

// Include unmasked properties as-is (use with caution!)
SetPropertyRuleBehavior(PropertyRuleBehavior.Include);
```

**Recommendation:** Use `PropertyRuleBehavior.Remove` to prevent accidentally leaking unmapped properties.

### 4. MaskingResult

The result object returned by the `Mask()` method:

```csharp
public class MaskingResult
{
    public bool IsSuccess { get; }
    public string MaskedData { get; }  // JSON string
    public List<string> Errors { get; }
}
```

## Common Use Cases

### Use Case 1: Logging User Data

```csharp
using Microsoft.Extensions.Logging;
using ITW.FluentMasker;

public class UserService
{
    private readonly ILogger<UserService> _logger;
    private readonly PersonMasker _personMasker;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
        _personMasker = new PersonMasker();
    }

    public void RegisterUser(Person person)
    {
        // Mask before logging
        var masked = _personMasker.Mask(person);
        _logger.LogInformation("User registered: {MaskedUser}", masked.MaskedData);

        // Your business logic...
    }
}
```

### Use Case 2: API Response Masking

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly PersonMasker _masker = new();

    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var user = GetUserFromDatabase(id);
        var masked = _masker.Mask(user);
        
        return Ok(masked.MaskedData); // Returns masked JSON
    }
}
```

### Use Case 3: Test Data Generation

```csharp
public class TestDataGenerator
{
    private readonly PersonMasker _masker = new();

    public string GenerateSafeTestData()
    {
        var realPerson = GetRealPersonFromProduction();
        var masked = _masker.Mask(realPerson);
        
        return masked.MaskedData; // Safe for test environments
    }
}
```

### Use Case 4: Credit Card Processing

```csharp
public class CreditCardMasker : AbstractMasker<CreditCard>
{
    public CreditCardMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // PCI-DSS compliant: show first 4 and last 4 digits
        MaskFor(x => x.CardNumber, (IMaskRule)new CardMaskRule());

        // NEVER log CVV!
        MaskFor(x => x.CVV, (IMaskRule)new RedactRule("***"));

        // Mask cardholder name
        MaskFor(x => x.CardHolderName, m => m.KeepFirst(3).KeepLast(3));
    }
}

// Usage
var cardMasker = new CreditCardMasker();
var result = cardMasker.Mask(creditCard);
_logger.LogInformation("Payment processed: {Card}", result.MaskedData);
// Output: {"CardNumber":"4532********9012","CVV":"***","CardHolderName":"Joh******Doe"}
```

## Most Commonly Used Rules

Here are the rules you'll use most often:

### 1. EmailMaskRule - Email Masking

```csharp
MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule(
    localKeep: 2,  // Keep first 2 chars of local part
    domainStrategy: EmailDomainStrategy.KeepFull  // Keep full domain
));

// "john.doe@example.com" → "jo**@example.com"
```

### 2. PhoneMaskRule - Phone Number Masking

```csharp
MaskFor(x => x.Phone, (IMaskRule)new PhoneMaskRule(
    keepLast: 4,           // Show last 4 digits
    preserveSeparators: true  // Keep format
));

// "+1 (555) 123-4567" → "+* (***) ***-4567"
```

### 3. CardMaskRule - Credit Card Masking

```csharp
MaskFor(x => x.CardNumber, (IMaskRule)new CardMaskRule());

// "4532123456789012" → "4532********9012"
```

### 4. RedactRule - Complete Redaction

```csharp
MaskFor(x => x.SSN, (IMaskRule)new RedactRule("[REDACTED]"));

// "123-45-6789" → "[REDACTED]"
```

### 5. Position-Based Rules (via Builder)

```csharp
// Keep first N characters
MaskFor(x => x.Name, m => m.KeepFirst(3));
// "Jonathan" → "Jon*****"

// Keep last N characters
MaskFor(x => x.Name, m => m.KeepLast(3));
// "Jonathan" → "*****han"

// Mask from start
MaskFor(x => x.Name, m => m.MaskStart(3));
// "Jonathan" → "***athan"

// Mask from end
MaskFor(x => x.Name, m => m.MaskEnd(3));
// "Jonathan" → "Jonat***"

// Keep first and last
MaskFor(x => x.Name, m => m.KeepFirst(2).KeepLast(2));
// "Jonathan" → "Jo****an"
```

### 6. HashRule - One-Way Hashing

```csharp
MaskFor(x => x.UserId, (IMaskRule)new HashRule(HashAlgorithmType.SHA256));

// "user@example.com" → "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8"
```

### 7. DateShiftRule - Date Anonymization

```csharp
MaskFor(x => x.BirthDate, new DateShiftRule(daysRange: 30));

// "1990-05-15" → "1990-06-10" (randomly shifted ±30 days)
```

## Common Pitfalls and Solutions

### ❌ Problem 1: Properties Not Appearing in Output

```csharp
var result = masker.Mask(person);
// Output: {"Email":"jo**@example.com"}  // Where's FirstName?
```

**Solution:** You forgot to add a masking rule for `FirstName`, and `PropertyRuleBehavior` is set to `Remove`.

```csharp
// Add the missing rule
MaskFor(x => x.FirstName, m => m.MaskStart(2));

// Or change behavior to include unmapped properties
SetPropertyRuleBehavior(PropertyRuleBehavior.Include);
```

### ❌ Problem 2: Compilation Error "Ambiguous Call"

```csharp
// ❌ This causes compilation error
MaskFor(x => x.Email, new EmailMaskRule());
```

**Solution:** Cast to `IMaskRule` interface:

```csharp
// ✅ This works
MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule());
```

### ❌ Problem 3: Nothing Gets Masked

```csharp
var masker = new PersonMasker();  // Forgot to call Initialize()!
var result = masker.Mask(person);
```

**Solution:** Make sure `Initialize()` is called (do this in the constructor):

```csharp
public PersonMasker()
{
    Initialize();  // ✅ Call this!
}
```

### ❌ Problem 4: Poor Performance

```csharp
// ❌ Creating new masker every time
public void ProcessUser(Person person)
{
    var masker = new PersonMasker();  // Wasteful!
    var result = masker.Mask(person);
}
```

**Solution:** Reuse masker instances:

```csharp
// ✅ Create once, reuse many times
private readonly PersonMasker _masker = new();

public void ProcessUser(Person person)
{
    var result = _masker.Mask(person);
}
```

### ❌ Problem 5: Null Reference Exception

```csharp
Person person = null;
var result = masker.Mask(person);  // 💥 NullReferenceException
```

**Solution:** Check for null before masking:

```csharp
if (person != null)
{
    var result = masker.Mask(person);
    // ...
}
```

## Dependency Injection Setup

### ASP.NET Core

```csharp
// Program.cs
using ITW.FluentMasker;

var builder = WebApplication.CreateBuilder(args);

// Register maskers as singletons (they're thread-safe and stateless)
builder.Services.AddSingleton<PersonMasker>();
builder.Services.AddSingleton<CreditCardMasker>();

var app = builder.Build();
app.Run();
```

```csharp
// In your controller or service
public class UserService
{
    private readonly PersonMasker _personMasker;

    public UserService(PersonMasker personMasker)
    {
        _personMasker = personMasker;
    }

    public void ProcessUser(Person person)
    {
        var masked = _personMasker.Mask(person);
        // Use masked data...
    }
}
```

### Console Application with DI

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<PersonMasker>();
        services.AddSingleton<MyApplication>();
    })
    .Build();

var app = host.Services.GetRequiredService<MyApplication>();
app.Run();
```

## Testing Your Maskers

### Unit Test Example

```csharp
using Xunit;
using ITW.FluentMasker;
using Newtonsoft.Json.Linq;

public class PersonMaskerTests
{
    private readonly PersonMasker _masker = new();

    [Fact]
    public void Mask_ShouldMaskEmail_Correctly()
    {
        // Arrange
        var person = new Person
        {
            Email = "john.doe@example.com"
        };

        // Act
        var result = _masker.Mask(person);
        var json = JObject.Parse(result.MaskedData);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("**", json["Email"].ToString());
        Assert.Contains("@example.com", json["Email"].ToString());
    }

    [Fact]
    public void Mask_ShouldRedactSSN_Completely()
    {
        // Arrange
        var person = new Person
        {
            SSN = "123-45-6789"
        };

        // Act
        var result = _masker.Mask(person);
        var json = JObject.Parse(result.MaskedData);

        // Assert
        Assert.Equal("[REDACTED]", json["SSN"].ToString());
    }

    [Fact]
    public void Mask_WithNullEmail_ShouldNotThrow()
    {
        // Arrange
        var person = new Person
        {
            Email = null
        };

        // Act & Assert
        var exception = Record.Exception(() => _masker.Mask(person));
        Assert.Null(exception);
    }
}
```

## Quick Reference Cheat Sheet

### Creating a Masker

```csharp
public class MyMasker : AbstractMasker<MyClass>
{
    public MyMasker()
    {
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);
        
        // Add your rules here
        MaskFor(x => x.Property, /* rule */);
    }
}
```

### Rule Syntax Options

```csharp
// Option 1: Builder (extension methods)
MaskFor(x => x.Name, m => m.MaskStart(2));

// Option 2: Rule class
MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule());

// Option 3: Multiple chained rules
MaskFor(x => x.Address, m => m
    .KeepFirst(4)
    .KeepLast(3));
```

### Using the Masker

```csharp
var masker = new MyMasker();
var result = masker.Mask(myObject);

if (result.IsSuccess)
{
    string maskedJson = result.MaskedData;
    // Use maskedJson...
}
else
{
    // Handle errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error);
    }
}
```

### Common PropertyRuleBehavior Values

```csharp
PropertyRuleBehavior.Remove   // ✅ Most secure - removes unmapped properties
PropertyRuleBehavior.Exclude  // Set unmapped properties to null
PropertyRuleBehavior.Include  // ⚠️ Include unmapped properties as-is
```

## Next Steps

Now that you understand the basics, explore these topics:

### **Core Documentation**
- **[Masking Rules Reference](./MaskingRulesReference.md)** - Complete catalog of all 25+ built-in rules
- **[Advanced Patterns](./AdvancedPatterns.md)** - Enterprise patterns, DI, performance optimization
- **[Compliance Guide](./ComplianceGuide.md)** - GDPR, HIPAA, PCI-DSS implementation patterns

### **Integration Guides**
- **[Serilog Integration](./SerilogIntegration.md)** - Automatic masking with IDestructuringPolicy
- **[ILogger Integration](./ILoggerIntegration.md)** - Using with Microsoft.Extensions.Logging

### **Specialized Topics**
- **[DateShiftRule Documentation](./DateAgeMaskRule.md)** - HIPAA-compliant date masking
- **[NationalIdMaskRule Documentation](./NationalIdMaskRule.md)** - Country-specific ID masking

### **Sample Projects**
- **ITW.FluentMasker.Serilog.Destructure.Sample** - Working examples in your solution

### **External Resources**
- [GitHub Repository](https://github.com/UlrikAtItWrk/FluentMasker)
- [NuGet Package](https://www.nuget.org/packages/ITW.FluentMasker)
- [Main README](../ITW.FluentMasker/README.md)

## Getting Help

### Common Questions

**Q: Can I mask nested objects?**  
A: Yes! Create separate maskers for nested types and use `MaskForEachRule<T>` for collections.

**Q: Is FluentMasker thread-safe?**  
A: Yes! Maskers are stateless and can be safely reused across threads.

**Q: Can I create custom rules?**  
A: Absolutely! Implement `IMaskRule<TInput, TOutput>` interface. See the Advanced Patterns guide.

**Q: Does it work with Entity Framework?**  
A: Yes, but mask data **after** retrieving from database, not in LINQ queries.

**Q: How do I mask DateTime properties?**  
A: Use `DateShiftRule` or `TimeBucketRule` for date/time anonymization.

