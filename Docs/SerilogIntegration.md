# Serilog Integration with IDestructuringPolicy

This guide demonstrates how to integrate FluentMasker with Serilog using `IDestructuringPolicy` to automatically mask sensitive data in your logs.

## Overview

Serilog's `IDestructuringPolicy` interface allows you to intercept objects during the destructuring phase and transform them before they are logged. By combining this with FluentMasker, you can ensure sensitive data is automatically masked before reaching any log sinks (files, databases, cloud services, etc.).

## Table of Contents

- [Why Use IDestructuringPolicy?](#why-use-idestructuringpolicy)
- [Basic Implementation](#basic-implementation)
- [Step-by-Step Guide](#step-by-step-guide)
- [Configuration](#configuration)
- [Advanced Patterns](#advanced-patterns)
- [Best Practices](#best-practices)
- [Complete Example](#complete-example)
- [Troubleshooting](#troubleshooting)

## Why Use IDestructuringPolicy?

### Benefits

1. **Centralized Masking**: Define masking rules once, apply everywhere
2. **Automatic**: No need to manually mask data at each log statement
3. **Type-Safe**: Works with Serilog's structured logging
4. **Transparent**: Developers don't need to remember to mask data
5. **Compliance**: Ensures PII/PHI never reaches log sinks (GDPR, HIPAA, PCI-DSS)
6. **Performance**: Masking only occurs when objects are actually logged

### When to Use

- Logging user data (names, emails, phone numbers)
- Payment processing systems (credit cards, bank accounts)
- Healthcare applications (patient data, medical records)
- Multi-tenant systems (cross-tenant data protection)
- Audit logging (sensitive operations)
- Production debugging (without exposing real data)

## Basic Implementation

### 1. Create the IDestructuringPolicy

```csharp
using Serilog.Core;
using Serilog.Events;
using ITW.FluentMasker;

public sealed class FluentMaskerPolicy : IDestructuringPolicy
{
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        // Check if the object is a type we want to mask
        if (value is Person person)
        {
            // Apply masking using FluentMasker
            var masker = new PersonMasker();
            var maskingResult = masker.Mask(person);
            
            // Return masked JSON string as a scalar value
            result = propertyValueFactory.CreatePropertyValue(
                maskingResult.MaskedData, 
                destructureObjects: true
            );
            return true;
        }

        // Not handled by this policy
        result = null!;
        return false;
    }
}
```

### 2. Create a Masker Class

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
        // Remove properties without explicit masking rules
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        // Define masking rules
        MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule(localKeep: 2));
        MaskFor(x => x.Phone, (IMaskRule)new PhoneMaskRule(keepLast: 4, preserveSeparators: true));
        MaskFor(x => x.SSN, (IMaskRule)new RedactRule("[REDACTED]"));
    }
}
```

### 3. Configure Serilog

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Destructure.With(new FluentMaskerPolicy())
    .WriteTo.Console()
    .WriteTo.File("logs/app.log")
    .CreateLogger();
```

### 4. Use in Your Application

```csharp
var person = new Person
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john.doe@example.com",
    Phone = "+1 (555) 123-4567",
    SSN = "123-45-6789"
};

// ? Use {@Object} syntax to trigger destructuring
Log.Information("User registered: {@Person}", person);

// Output: User registered: {"Email":"jo**@example.com","Phone":"+* (***) ***-4567","SSN":"[REDACTED]"}
```

## Step-by-Step Guide

### Step 1: Add Required Packages

```bash
dotnet add package ITW.FluentMasker
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
```

### Step 2: Define Your Models

```csharp
public class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string SSN { get; set; }
    public DateTime BirthDate { get; set; }
}
```

### Step 3: Create Masker Classes

Define how each property should be masked:

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

        // Email: Mask local part, keep domain
        MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule(
            localKeep: 2, 
            domainStrategy: EmailDomainStrategy.KeepFull
        ));

        // Phone: Show last 4 digits, preserve format
        MaskFor(x => x.Phone, (IMaskRule)new PhoneMaskRule(
            keepLast: 4, 
            preserveSeparators: true
        ));

        // SSN: Complete redaction
        MaskFor(x => x.SSN, (IMaskRule)new RedactRule("[REDACTED]"));

        // Names: Partial masking
        MaskFor(x => x.FirstName, m => m.MaskStart(2));
        MaskFor(x => x.LastName, m => m.KeepFirst(1).KeepLast(1));

        // BirthDate: Random shift for privacy
        MaskFor(x => x.BirthDate, new DateShiftRule(daysRange: 30));
    }
}
```

### Step 4: Implement IDestructuringPolicy

```csharp
using Serilog.Core;
using Serilog.Events;
using ITW.FluentMasker;

public sealed class FluentMaskerPolicy : IDestructuringPolicy
{
    // Optional: Cache masker instances for performance
    private readonly Dictionary<Type, object> _maskerCache = new();

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        // Get the type of the object
        var type = value.GetType();

        // Try to get or create a masker for this type
        if (TryGetMasker(type, out var masker))
        {
            // Apply masking
            var maskMethod = masker.GetType().GetMethod("Mask");
            var maskingResult = (MaskingResult)maskMethod!.Invoke(masker, new[] { value });

            // Return masked data
            result = propertyValueFactory.CreatePropertyValue(
                maskingResult.MaskedData,
                destructureObjects: true
            );
            return true;
        }

        // Not handled
        result = null!;
        return false;
    }

    private bool TryGetMasker(Type type, out object masker)
    {
        // Check cache first
        if (_maskerCache.TryGetValue(type, out masker!))
            return true;

        // Create masker based on type
        masker = type.Name switch
        {
            nameof(Person) => new PersonMasker(),
            nameof(CreditCard) => new CreditCardMasker(),
            nameof(HealthRecord) => new HealthRecordMasker(),
            _ => null
        };

        if (masker != null)
        {
            _maskerCache[type] = masker;
            return true;
        }

        return false;
    }
}
```

### Step 5: Register with Serilog

```csharp
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .Destructure.With(new FluentMaskerPolicy())
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Application starting");
            
            // Your application code here
            
            Log.Information("Application stopping");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
```

## Configuration

### Multiple Policies

You can chain multiple destructuring policies:

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.With(new FluentMaskerPolicy())
    .Destructure.With(new CustomPolicy())
    .WriteTo.Console()
    .CreateLogger();
```

### Property Rule Behaviors

Control how properties without explicit masking rules are handled:

```csharp
// Remove unmasked properties from output
SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

// Set unmasked properties to null
SetPropertyRuleBehavior(PropertyRuleBehavior.Exclude);

// Include unmasked properties as-is
SetPropertyRuleBehavior(PropertyRuleBehavior.Include);
```

### Conditional Masking

Apply different masking based on context:

```csharp
public class ContextualMasker : AbstractMasker<Person>
{
    private readonly bool _isProduction;

    public ContextualMasker(bool isProduction)
    {
        _isProduction = isProduction;
        Initialize();
    }

    private void Initialize()
    {
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        if (_isProduction)
        {
            // Strict masking in production
            MaskFor(x => x.Email, (IMaskRule)new RedactRule("[EMAIL]"));
            MaskFor(x => x.Phone, (IMaskRule)new RedactRule("[PHONE]"));
        }
        else
        {
            // Partial masking in development
            MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule(localKeep: 2));
            MaskFor(x => x.Phone, (IMaskRule)new PhoneMaskRule(keepLast: 4));
        }
    }
}
```

## Advanced Patterns

### Generic Policy with Convention

Create a policy that automatically discovers maskers:

```csharp
public sealed class ConventionBasedMaskerPolicy : IDestructuringPolicy
{
    private readonly Dictionary<Type, object> _maskers = new();
    private readonly Assembly _assembly;

    public ConventionBasedMaskerPolicy(Assembly assembly)
    {
        _assembly = assembly;
    }

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        var type = value.GetType();

        if (!_maskers.TryGetValue(type, out var masker))
        {
            // Look for a masker class named "{Type}Masker"
            var maskerType = _assembly.GetTypes()
                .FirstOrDefault(t => t.Name == $"{type.Name}Masker");

            if (maskerType != null)
            {
                masker = Activator.CreateInstance(maskerType);
                _maskers[type] = masker;
            }
            else
            {
                result = null!;
                return false;
            }
        }

        // Apply masking
        var maskMethod = masker.GetType().GetMethod("Mask");
        var maskingResult = (MaskingResult)maskMethod!.Invoke(masker, new[] { value });

        result = propertyValueFactory.CreatePropertyValue(
            maskingResult.MaskedData,
            destructureObjects: true
        );
        return true;
    }
}

// Usage
Log.Logger = new LoggerConfiguration()
    .Destructure.With(new ConventionBasedMaskerPolicy(typeof(Program).Assembly))
    .WriteTo.Console()
    .CreateLogger();
```

### Attribute-Based Masking

Use attributes to define masking rules:

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class MaskAttribute : Attribute
{
    public string RuleType { get; set; }
    public object[] Parameters { get; set; }

    public MaskAttribute(string ruleType, params object[] parameters)
    {
        RuleType = ruleType;
        Parameters = parameters;
    }
}

public class Person
{
    [Mask("MaskStart", 2)]
    public string FirstName { get; set; }

    [Mask("EmailMask", 2)]
    public string Email { get; set; }

    [Mask("Redact", "[REDACTED]")]
    public string SSN { get; set; }
}
```

### Async Masking (for external lookups)

If you need to perform async operations during masking:

```csharp
public sealed class AsyncFluentMaskerPolicy : IDestructuringPolicy
{
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        if (value is SensitiveData data)
        {
            // Note: IDestructuringPolicy is synchronous, so we need to
            // block on async operations (not ideal, but sometimes necessary)
            var masker = new SensitiveDataMasker();
            var maskingTask = masker.MaskAsync(data);
            var maskingResult = maskingTask.GetAwaiter().GetResult();

            result = propertyValueFactory.CreatePropertyValue(
                maskingResult.MaskedData,
                destructureObjects: true
            );
            return true;
        }

        result = null!;
        return false;
    }
}
```

## Best Practices

### 1. Cache Masker Instances

Maskers are typically stateless and can be reused:

```csharp
public sealed class FluentMaskerPolicy : IDestructuringPolicy
{
    private readonly PersonMasker _personMasker = new();
    private readonly CreditCardMasker _creditCardMasker = new();

    public bool TryDestructure(/* ... */)
    {
        if (value is Person person)
        {
            var result = _personMasker.Mask(person);
            // ...
        }
    }
}
```

### 2. Use PropertyRuleBehavior.Remove

Prevent accidentally logging unmapped properties:

```csharp
SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);
```

### 3. Always Use {@Object} Syntax

Train developers to use structured logging with destructuring:

```csharp
// ? CORRECT - Triggers masking
Log.Information("User: {@User}", user);

// ? WRONG - No masking, calls ToString()
Log.Information("User: {User}", user);

// ? WRONG - No masking, string interpolation
Log.Information($"User: {user}");
```

### 4. Test Masking Rules

Write unit tests for your maskers:

```csharp
[Fact]
public void PersonMasker_MasksEmail_Correctly()
{
    // Arrange
    var person = new Person { Email = "john.doe@example.com" };
    var masker = new PersonMasker();

    // Act
    var result = masker.Mask(person);
    var json = JObject.Parse(result.MaskedData);

    // Assert
    Assert.Contains("**", json["Email"].ToString());
    Assert.Contains("@example.com", json["Email"].ToString());
}
```

### 5. Document Masking Rules

Keep a reference of what gets masked:

```csharp
/// <summary>
/// Masker for Person objects.
/// 
/// Masking Rules:
/// - FirstName: First 2 characters masked (MaskStart)
/// - LastName: Only first and last character shown (KeepFirst/KeepLast)
/// - Email: Local part masked, domain preserved (EmailMaskRule)
/// - Phone: Last 4 digits shown, format preserved (PhoneMaskRule)
/// - SSN: Completely redacted (RedactRule)
/// </summary>
public class PersonMasker : AbstractMasker<Person>
{
    // ...
}
```

### 6. Handle Null and Empty Values

FluentMasker handles nulls gracefully, but be aware:

```csharp
// These are safe - FluentMasker returns null/empty unchanged
person.Email = null;    // Remains null
person.Phone = "";      // Remains empty
```

### 7. Be Careful with Object Graphs

When logging complex object graphs, mask at appropriate levels:

```csharp
public class Order
{
    public int OrderId { get; set; }
    public Person Customer { get; set; }
    public CreditCard PaymentMethod { get; set; }
}

// Both Customer and PaymentMethod will be masked if policies exist
Log.Information("Order placed: {@Order}", order);
```

## Complete Example

Here's a complete, production-ready example:

```csharp
// Models/Person.cs
public class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string SSN { get; set; }
    public DateTime BirthDate { get; set; }
}

// Maskers/PersonMasker.cs
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
        SetPropertyRuleBehavior(PropertyRuleBehavior.Remove);

        MaskFor(x => x.FirstName, m => m.MaskStart(2));
        MaskFor(x => x.LastName, m => m.KeepFirst(1).KeepLast(1));
        MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule(
            localKeep: 2, 
            domainStrategy: EmailDomainStrategy.KeepFull
        ));
        MaskFor(x => x.Phone, (IMaskRule)new PhoneMaskRule(
            keepLast: 4, 
            preserveSeparators: true
        ));
        MaskFor(x => x.SSN, (IMaskRule)new RedactRule("[REDACTED]"));
        MaskFor(x => x.BirthDate, new DateShiftRule(daysRange: 30));
    }
}

// Logging/FluentMaskerPolicy.cs
using Serilog.Core;
using Serilog.Events;
using ITW.FluentMasker;

public sealed class FluentMaskerPolicy : IDestructuringPolicy
{
    private readonly PersonMasker _personMasker = new();

    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        if (value is Person person)
        {
            var maskingResult = _personMasker.Mask(person);
            result = propertyValueFactory.CreatePropertyValue(
                maskingResult.MaskedData,
                destructureObjects: true
            );
            return true;
        }

        result = null!;
        return false;
    }
}

// Program.cs
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .Destructure.With(new FluentMaskerPolicy())
            .WriteTo.Console()
            .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var person = new Person
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "+1 (555) 123-4567",
                SSN = "123-45-6789",
                BirthDate = new DateTime(1990, 5, 15)
            };

            // This will automatically mask sensitive data
            Log.Information("User registered: {@Person}", person);

            // Output: User registered: 
            // {"FirstName":"**hn","LastName":"D*e","Email":"jo**@example.com",
            //  "Phone":"+* (***) ***-4567","SSN":"[REDACTED]","BirthDate":"1990-06-10T00:00:00"}
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
```

## Troubleshooting

### Masking Not Applied

**Problem**: Objects are logged without masking.

**Solution**: Ensure you're using the `{@Object}` syntax, not `{Object}`:

```csharp
// ? Correct
Log.Information("User: {@Person}", person);

// ? Wrong - no destructuring
Log.Information("User: {Person}", person);
```

### Policy Not Called

**Problem**: `TryDestructure` method never executes.

**Solution**: Verify the policy is registered:

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.With(new FluentMaskerPolicy())  // Must be here
    .WriteTo.Console()
    .CreateLogger();
```

### Compilation Errors with IMaskRule

**Problem**: Ambiguous method calls when using `MaskFor`.

**Solution**: Cast to `IMaskRule` interface:

```csharp
// ? Correct
MaskFor(x => x.Email, (IMaskRule)new EmailMaskRule());

// ? Might cause ambiguity
MaskFor(x => x.Email, new EmailMaskRule());
```

### Performance Issues

**Problem**: Logging is slow due to masking overhead.

**Solutions**:
1. Cache masker instances
2. Use `PropertyRuleBehavior.Remove` to reduce output size
3. Consider masking only in production
4. Use async sinks to offload masking

```csharp
// Cache maskers
private readonly PersonMasker _personMasker = new();

// Or use lazy initialization
private readonly Lazy<PersonMasker> _personMasker = 
    new Lazy<PersonMasker>(() => new PersonMasker());
```

### JSON Parsing Errors

**Problem**: Masked output is not valid JSON.

**Solution**: Ensure `MaskingResult.MaskedData` is valid JSON:

```csharp
var maskingResult = masker.Mask(person);

// This is already JSON
result = propertyValueFactory.CreatePropertyValue(
    maskingResult.MaskedData,
    destructureObjects: true  // Parse as JSON
);
```

## Related Documentation

- [FluentMasker README](../ITW.FluentMasker/README.md)
- [Available Masking Rules](../ITW.FluentMasker/README.md#built-in-masking-rules)
- [DateShiftRule Documentation](./DateAgeMaskRule.md)
- [NationalIdMaskRule Documentation](./NationalIdMaskRule.md)
- [Serilog IDestructuringPolicy API](https://github.com/serilog/serilog/blob/dev/src/Serilog/Core/IDestructuringPolicy.cs)

## Sample Project

A complete working example is available in the solution:

```
ITW.FluentMasker.Serilog.Destructure.Sample/
??? FluentMaskerPolicy.cs       # IDestructuringPolicy implementation
??? Models.cs                   # Sample model classes
??? Maskers.cs                  # Masker implementations
??? Program.cs                  # Usage examples
??? README.md                   # Sample-specific documentation
```

## Conclusion

Integrating FluentMasker with Serilog via `IDestructuringPolicy` provides a powerful, automatic way to ensure sensitive data is masked before it reaches your logs. This pattern is especially valuable for:

- **Compliance**: Meet GDPR, HIPAA, PCI-DSS requirements
- **Security**: Prevent data leaks through logs
- **Developer Experience**: No manual masking needed
- **Flexibility**: Easy to customize per property or type
- **Performance**: Efficient masking with compiled expressions

For more information and examples, see the sample project included in this repository.
