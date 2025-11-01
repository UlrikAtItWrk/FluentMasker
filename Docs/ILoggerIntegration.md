# ILogger Integration with FluentMasker

This guide demonstrates how to integrate FluentMasker with the standard `Microsoft.Extensions.Logging.ILogger` interface. Unlike Serilog's `IDestructuringPolicy`, `ILogger` doesn't have a built-in destructuring mechanism, so we'll explore several patterns to safely mask sensitive data before logging.

## Overview

The standard `ILogger` interface is used throughout the .NET ecosystem, including ASP.NET Core, Worker Services, and console applications. Since it doesn't support automatic destructuring like Serilog, we need different approaches to ensure sensitive data is masked before logging.

## Table of Contents

- [Key Differences from Serilog](#key-differences-from-serilog)
- [Approach 1: Manual Masking Before Logging](#approach-1-manual-masking-before-logging)
- [Approach 2: Extension Methods](#approach-2-extension-methods)
- [Approach 3: Logging Middleware](#approach-3-logging-middleware)
- [Approach 4: Custom LoggerProvider](#approach-4-custom-loggerprovider)
- [Approach 5: Source Generators (Advanced)](#approach-5-source-generators-advanced)
- [Best Practices](#best-practices)
- [Complete Examples](#complete-examples)
- [Performance Considerations](#performance-considerations)
- [Migration from Serilog](#migration-from-serilog)

## Key Differences from Serilog

### Serilog with IDestructuringPolicy
```csharp
// ? Serilog - Automatic masking via destructuring
Log.Information("User registered: {@Person}", person);
// FluentMaskerPolicy automatically intercepts and masks
```

### Standard ILogger
```csharp
// ? ILogger - No automatic destructuring
_logger.LogInformation("User registered: {Person}", person);
// Just calls ToString(), no masking occurs

// ? ILogger - Manual masking required
var maskedData = MaskPerson(person);
_logger.LogInformation("User registered: {MaskedPerson}", maskedData);
```

**Key Differences:**
- ILogger has no destructuring pipeline
- No built-in policy injection point
- Must manually mask before logging
- Can leverage structured logging with pre-masked data

## Approach 1: Manual Masking Before Logging

The simplest approach: explicitly mask data before logging.

### Basic Implementation

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
        // Mask the sensitive data before logging
        var maskingResult = _personMasker.Mask(person);
        var maskedData = maskingResult.MaskedData;

        // Log with masked data
        _logger.LogInformation("User registered: {MaskedPerson}", maskedData);

        // Business logic...
    }
}
```

### With Structured Logging

```csharp
public void RegisterUser(Person person)
{
    var maskingResult = _personMasker.Mask(person);
    
    // Parse masked JSON and extract individual properties
    var masked = JsonSerializer.Deserialize<Dictionary<string, object>>(maskingResult.MaskedData);

    _logger.LogInformation(
        "User registered: FirstName={FirstName}, Email={Email}, Phone={Phone}",
        masked["FirstName"],
        masked["Email"],
        masked["Phone"]
    );
}
```

### Pros and Cons

**Pros:**
- Simple and explicit
- No magic or hidden behavior
- Easy to understand and debug
- Works with any logging provider

**Cons:**
- Repetitive code
- Easy to forget to mask
- Not enforceable at compile time

## Approach 2: Extension Methods

Create reusable extension methods to reduce boilerplate.

### Extension Method Implementation

```csharp
using Microsoft.Extensions.Logging;
using ITW.FluentMasker;
using System.Text.Json;

public static class LoggerExtensions
{
    private static readonly PersonMasker _personMasker = new();
    private static readonly CreditCardMasker _creditCardMasker = new();
    private static readonly HealthRecordMasker _healthRecordMasker = new();

    /// <summary>
    /// Logs a Person object with automatic masking.
    /// </summary>
    public static void LogPerson(
        this ILogger logger,
        LogLevel logLevel,
        string message,
        Person person)
    {
        if (!logger.IsEnabled(logLevel))
            return;

        var maskingResult = _personMasker.Mask(person);
        logger.Log(logLevel, message + " {MaskedPerson}", maskingResult.MaskedData);
    }

    /// <summary>
    /// Logs a CreditCard object with PCI-DSS compliant masking.
    /// </summary>
    public static void LogCreditCard(
        this ILogger logger,
        LogLevel logLevel,
        string message,
        CreditCard card)
    {
        if (!logger.IsEnabled(logLevel))
            return;

        var maskingResult = _creditCardMasker.Mask(card);
        logger.Log(logLevel, message + " {MaskedCard}", maskingResult.MaskedData);
    }

    /// <summary>
    /// Generic method to log any object with masking.
    /// </summary>
    public static void LogMasked<T>(
        this ILogger logger,
        LogLevel logLevel,
        string message,
        T data,
        AbstractMasker<T> masker)
    {
        if (!logger.IsEnabled(logLevel))
            return;

        var maskingResult = masker.Mask(data);
        logger.Log(logLevel, message + " {MaskedData}", maskingResult.MaskedData);
    }

    /// <summary>
    /// Logs structured properties from a masked object.
    /// </summary>
    public static void LogMaskedStructured<T>(
        this ILogger logger,
        LogLevel logLevel,
        string messageTemplate,
        T data,
        AbstractMasker<T> masker)
    {
        if (!logger.IsEnabled(logLevel))
            return;

        var maskingResult = masker.Mask(data);
        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            maskingResult.MaskedData
        );

        // Extract property values for structured logging
        var values = properties?.Values.Select(v => (object)v.ToString()).ToArray() ?? Array.Empty<object>();
        logger.Log(logLevel, messageTemplate, values);
    }
}
```

### Usage

```csharp
public class UserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public void RegisterUser(Person person)
    {
        // Simple usage
        _logger.LogPerson(LogLevel.Information, "User registered:", person);

        // Generic usage
        var masker = new PersonMasker();
        _logger.LogMasked(LogLevel.Information, "User data:", person, masker);
    }

    public void ProcessPayment(CreditCard card)
    {
        _logger.LogCreditCard(LogLevel.Information, "Payment processed:", card);
    }
}
```

### Pros and Cons

**Pros:**
- Reduces boilerplate
- Consistent API across the application
- Reusable masker instances
- Type-safe

**Cons:**
- Still requires explicit method calls
- Need extension methods for each type
- Can't prevent direct ILogger calls

## Approach 3: Logging Middleware

Wrap ILogger with a masking layer that intercepts all log calls.

### Middleware Implementation

```csharp
using Microsoft.Extensions.Logging;
using ITW.FluentMasker;
using System.Text.Json;

/// <summary>
/// Logger wrapper that automatically masks sensitive objects before logging.
/// </summary>
public class MaskingLogger<T> : ILogger<T>
{
    private readonly ILogger<T> _innerLogger;
    private readonly Dictionary<Type, object> _maskers;

    public MaskingLogger(ILogger<T> innerLogger)
    {
        _innerLogger = innerLogger;
        _maskers = new Dictionary<Type, object>
        {
            [typeof(Person)] = new PersonMasker(),
            [typeof(CreditCard)] = new CreditCardMasker(),
            [typeof(HealthRecord)] = new HealthRecordMasker()
        };
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _innerLogger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => _innerLogger.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        // Check if state is a sensitive type we should mask
        if (state is IReadOnlyList<KeyValuePair<string, object>> properties)
        {
            var maskedProperties = new List<KeyValuePair<string, object>>();

            foreach (var kvp in properties)
            {
                var maskedValue = MaskIfNeeded(kvp.Value);
                maskedProperties.Add(new KeyValuePair<string, object>(kvp.Key, maskedValue));
            }

            // Create new state with masked values
            var maskedState = (TState)(object)maskedProperties;
            _innerLogger.Log(logLevel, eventId, maskedState, exception, formatter);
        }
        else
        {
            _innerLogger.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    private object MaskIfNeeded(object value)
    {
        if (value == null)
            return null!;

        var type = value.GetType();

        if (_maskers.TryGetValue(type, out var masker))
        {
            var maskMethod = masker.GetType().GetMethod("Mask");
            var result = (MaskingResult)maskMethod!.Invoke(masker, new[] { value })!;
            return result.MaskedData;
        }

        return value;
    }
}

/// <summary>
/// Factory for creating MaskingLogger instances.
/// </summary>
public class MaskingLoggerProvider : ILoggerProvider
{
    private readonly ILoggerProvider _innerProvider;

    public MaskingLoggerProvider(ILoggerProvider innerProvider)
    {
        _innerProvider = innerProvider;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var innerLogger = _innerProvider.CreateLogger(categoryName);
        
        // Use reflection to create MaskingLogger<T>
        var categoryType = Type.GetType(categoryName) ?? typeof(object);
        var maskingLoggerType = typeof(MaskingLogger<>).MakeGenericType(categoryType);
        
        return (ILogger)Activator.CreateInstance(maskingLoggerType, innerLogger)!;
    }

    public void Dispose()
    {
        _innerProvider?.Dispose();
    }
}
```

### Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class MaskingLoggerExtensions
{
    public static ILoggingBuilder AddMasking(this ILoggingBuilder builder)
    {
        // Wrap existing logger provider with masking provider
        builder.Services.Decorate<ILoggerProvider, MaskingLoggerProvider>();
        return builder;
    }
}

// In Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddMasking(); // Add masking layer

var app = builder.Build();
```

### Usage

```csharp
public class UserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger; // This is now a MaskingLogger
    }

    public void RegisterUser(Person person)
    {
        // Automatic masking!
        _logger.LogInformation("User registered: {Person}", person);
    }
}
```

### Pros and Cons

**Pros:**
- Transparent masking
- No changes to existing code
- Centralized configuration
- Works for all log calls

**Cons:**
- More complex implementation
- Performance overhead on all log calls
- Harder to debug
- May interfere with structured logging

## Approach 4: Custom LoggerProvider

Create a custom logger provider with built-in masking support.

### Custom Logger Implementation

```csharp
using Microsoft.Extensions.Logging;
using ITW.FluentMasker;
using System.Text.Json;

public class MaskedConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Dictionary<Type, object> _maskers;

    public MaskedConsoleLogger(string categoryName)
    {
        _categoryName = categoryName;
        _maskers = new Dictionary<Type, object>
        {
            [typeof(Person)] = new PersonMasker(),
            [typeof(CreditCard)] = new CreditCardMasker(),
            [typeof(HealthRecord)] = new HealthRecordMasker()
        };
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);

        // Extract and mask structured values
        if (state is IEnumerable<KeyValuePair<string, object>> properties)
        {
            var maskedMessage = message;
            foreach (var property in properties)
            {
                if (property.Key != "{OriginalFormat}" && property.Value != null)
                {
                    var maskedValue = MaskValue(property.Value);
                    var placeholder = $"{{{property.Key}}}";
                    maskedMessage = maskedMessage.Replace(placeholder, maskedValue.ToString());
                }
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{logLevel}] {_categoryName}: {maskedMessage}");
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{logLevel}] {_categoryName}: {message}");
        }

        if (exception != null)
        {
            Console.WriteLine($"Exception: {exception}");
        }
    }

    private object MaskValue(object value)
    {
        if (value == null)
            return "null";

        var type = value.GetType();

        if (_maskers.TryGetValue(type, out var masker))
        {
            var maskMethod = masker.GetType().GetMethod("Mask");
            var result = (MaskingResult)maskMethod!.Invoke(masker, new[] { value })!;
            return result.MaskedData;
        }

        return value;
    }
}

public class MaskedConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new MaskedConsoleLogger(categoryName);

    public void Dispose() { }
}
```

### Registration

```csharp
public static class MaskedConsoleLoggerExtensions
{
    public static ILoggingBuilder AddMaskedConsole(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, MaskedConsoleLoggerProvider>();
        return builder;
    }
}

// Usage
builder.Logging.AddMaskedConsole();
```

### Pros and Cons

**Pros:**
- Complete control over logging behavior
- Can optimize for specific scenarios
- No dependency on other providers
- Custom formatting options

**Cons:**
- Need to implement full ILogger interface
- More code to maintain
- May lose features from built-in providers

## Approach 5: Source Generators (Advanced)

Use source generators to automatically create masking code at compile time.

### Conceptual Example

```csharp
// Attribute to mark classes for masking
[AttributeUsage(AttributeTargets.Class)]
public class AutoMaskAttribute : Attribute { }

// Attribute for properties
[AttributeUsage(AttributeTargets.Property)]
public class MaskPropertyAttribute : Attribute
{
    public string MaskingRule { get; set; }
    
    public MaskPropertyAttribute(string maskingRule)
    {
        MaskingRule = maskingRule;
    }
}

// Usage
[AutoMask]
public class Person
{
    [MaskProperty("MaskStart(2)")]
    public string FirstName { get; set; }

    [MaskProperty("EmailMask")]
    public string Email { get; set; }

    [MaskProperty("Redact")]
    public string SSN { get; set; }
}

// Source generator would create:
// - PersonMasker class
// - Extension methods for ILogger
// - Compile-time validation
```

### Benefits

- Compile-time validation
- Zero runtime overhead
- Automatic code generation
- Type-safe

**Note:** Implementing a source generator is beyond the scope of this guide, but it's a powerful option for large-scale applications.

## Best Practices

### 1. Create a Consistent Logging Interface

Define a standard interface for masked logging:

```csharp
public interface IMaskedLogger
{
    void LogMaskedInformation<T>(string message, T data, AbstractMasker<T> masker);
    void LogMaskedWarning<T>(string message, T data, AbstractMasker<T> masker);
    void LogMaskedError<T>(string message, T data, AbstractMasker<T> masker, Exception? exception = null);
}

public class MaskedLogger : IMaskedLogger
{
    private readonly ILogger _logger;

    public MaskedLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogMaskedInformation<T>(string message, T data, AbstractMasker<T> masker)
    {
        var result = masker.Mask(data);
        _logger.LogInformation(message + " {MaskedData}", result.MaskedData);
    }

    public void LogMaskedWarning<T>(string message, T data, AbstractMasker<T> masker)
    {
        var result = masker.Mask(data);
        _logger.LogWarning(message + " {MaskedData}", result.MaskedData);
    }

    public void LogMaskedError<T>(string message, T data, AbstractMasker<T> masker, Exception? exception = null)
    {
        var result = masker.Mask(data);
        _logger.LogError(exception, message + " {MaskedData}", result.MaskedData);
    }
}
```

### 2. Use Dependency Injection for Maskers

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMaskers(this IServiceCollection services)
    {
        services.AddSingleton<PersonMasker>();
        services.AddSingleton<CreditCardMasker>();
        services.AddSingleton<HealthRecordMasker>();
        services.AddScoped<IMaskedLogger, MaskedLogger>();
        
        return services;
    }
}

// Usage
public class UserService
{
    private readonly IMaskedLogger _logger;
    private readonly PersonMasker _personMasker;

    public UserService(IMaskedLogger logger, PersonMasker personMasker)
    {
        _logger = logger;
        _personMasker = personMasker;
    }

    public void RegisterUser(Person person)
    {
        _logger.LogMaskedInformation("User registered:", person, _personMasker);
    }
}
```

### 3. Handle Null and Edge Cases

```csharp
public static class SafeLoggingExtensions
{
    public static void LogMaskedSafe<T>(
        this ILogger logger,
        LogLevel logLevel,
        string message,
        T? data,
        AbstractMasker<T> masker) where T : class
    {
        if (!logger.IsEnabled(logLevel))
            return;

        if (data == null)
        {
            logger.Log(logLevel, message + " {Data}", "null");
            return;
        }

        try
        {
            var result = masker.Mask(data);
            logger.Log(logLevel, message + " {MaskedData}", result.MaskedData);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mask data of type {Type}", typeof(T).Name);
            logger.Log(logLevel, message + " {Data}", "[MASKING_ERROR]");
        }
    }
}
```

### 4. Create Helper Methods for Common Scenarios

```csharp
public static class CommonLoggingScenarios
{
    private static readonly PersonMasker _personMasker = new();
    private static readonly CreditCardMasker _cardMasker = new();

    public static void LogUserAction(
        this ILogger logger,
        string action,
        Person user)
    {
        var masked = _personMasker.Mask(user);
        logger.LogInformation(
            "User action: {Action}, User: {MaskedUser}",
            action,
            masked.MaskedData
        );
    }

    public static void LogPaymentAttempt(
        this ILogger logger,
        decimal amount,
        CreditCard card,
        bool success)
    {
        var masked = _cardMasker.Mask(card);
        logger.LogInformation(
            "Payment attempt: Amount={Amount}, Card={MaskedCard}, Success={Success}",
            amount,
            masked.MaskedData,
            success
        );
    }
}
```

### 5. Document Masking Requirements

```csharp
/// <summary>
/// Service for user registration.
/// 
/// Logging Policy:
/// - All Person objects MUST be masked before logging using PersonMasker
/// - Use LogMasked extension methods or IMaskedLogger
/// - Never log raw Person objects directly
/// </summary>
public class UserService
{
    // Implementation...
}
```

## Complete Examples

### Example 1: ASP.NET Core API with Extension Methods

```csharp
// Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ITW.FluentMasker;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSingleton<PersonMasker>();
builder.Services.AddSingleton<CreditCardMasker>();

var app = builder.Build();
app.MapControllers();
app.Run();

// Controllers/UserController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly PersonMasker _personMasker;

    public UserController(
        ILogger<UserController> logger,
        PersonMasker personMasker)
    {
        _logger = logger;
        _personMasker = personMasker;
    }

    [HttpPost]
    public IActionResult Register([FromBody] Person person)
    {
        // Log with masking
        _logger.LogMasked(
            LogLevel.Information,
            "User registration received:",
            person,
            _personMasker
        );

        // Process registration...

        return Ok();
    }
}
```

### Example 2: Worker Service with IMaskedLogger

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddMaskers();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();

// Worker.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Worker : BackgroundService
{
    private readonly IMaskedLogger _logger;
    private readonly PersonMasker _personMasker;

    public Worker(IMaskedLogger logger, PersonMasker personMasker)
    {
        _logger = logger;
        _personMasker = personMasker;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var person = FetchNextPerson();

            _logger.LogMaskedInformation(
                "Processing person:",
                person,
                _personMasker
            );

            await Task.Delay(1000, stoppingToken);
        }
    }

    private Person FetchNextPerson() => new Person
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "john@example.com"
    };
}
```

### Example 3: Console Application with Manual Masking

```csharp
using Microsoft.Extensions.Logging;
using ITW.FluentMasker;

class Program
{
    static void Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger<Program>();
        var personMasker = new PersonMasker();

        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "+1 (555) 123-4567",
            SSN = "123-45-6789"
        };

        // Manual masking
        var maskingResult = personMasker.Mask(person);
        logger.LogInformation("User data: {MaskedPerson}", maskingResult.MaskedData);

        // Using extension method
        logger.LogMasked(
            LogLevel.Information,
            "User registration:",
            person,
            personMasker
        );
    }
}
```

## Performance Considerations

### 1. Cache Masker Instances

```csharp
// ? Good - Reuse masker instances
public class UserService
{
    private readonly PersonMasker _personMasker = new();

    public void ProcessUser(Person person)
    {
        var result = _personMasker.Mask(person);
        // ...
    }
}

// ? Bad - Creating new masker each time
public void ProcessUser(Person person)
{
    var masker = new PersonMasker(); // Wasteful!
    var result = masker.Mask(person);
    // ...
}
```

### 2. Check IsEnabled Before Masking

```csharp
public static void LogMaskedOptimized<T>(
    this ILogger logger,
    LogLevel logLevel,
    string message,
    T data,
    AbstractMasker<T> masker)
{
    // Early exit if logging is disabled for this level
    if (!logger.IsEnabled(logLevel))
        return;

    var result = masker.Mask(data);
    logger.Log(logLevel, message + " {MaskedData}", result.MaskedData);
}
```

### 3. Use Lazy Evaluation for Expensive Operations

```csharp
public void LogComplexOperation(Person person)
{
    // Only evaluate if Information level is enabled
    if (_logger.IsEnabled(LogLevel.Information))
    {
        var masked = _personMasker.Mask(person);
        _logger.LogInformation("Operation: {Data}", masked.MaskedData);
    }
}
```

### 4. Consider Batch Masking

```csharp
public void LogMultipleUsers(IEnumerable<Person> people)
{
    if (!_logger.IsEnabled(LogLevel.Information))
        return;

    var maskedPeople = people
        .Select(p => _personMasker.Mask(p).MaskedData)
        .ToArray();

    _logger.LogInformation("Users: {MaskedUsers}", string.Join(", ", maskedPeople));
}
```

## Migration from Serilog

### Before (Serilog with IDestructuringPolicy)

```csharp
// Automatic masking via policy
Log.Information("User registered: {@Person}", person);
Log.Information("Payment: {@CreditCard}", card);
```

### After (ILogger with Extension Methods)

```csharp
// Option 1: Extension methods
_logger.LogMasked(LogLevel.Information, "User registered:", person, _personMasker);
_logger.LogMasked(LogLevel.Information, "Payment:", card, _cardMasker);

// Option 2: IMaskedLogger
_maskedLogger.LogMaskedInformation("User registered:", person, _personMasker);
_maskedLogger.LogMaskedInformation("Payment:", card, _cardMasker);

// Option 3: Manual (most control)
var maskedPerson = _personMasker.Mask(person);
_logger.LogInformation("User registered: {Person}", maskedPerson.MaskedData);
```

### Migration Checklist

- [ ] Replace `Log.` calls with `_logger.` calls
- [ ] Inject `ILogger<T>` into classes
- [ ] Add masker instances (via DI or as fields)
- [ ] Replace `{@Object}` with explicit masking
- [ ] Implement extension methods or IMaskedLogger
- [ ] Update unit tests to account for masking calls
- [ ] Review all log statements for PII exposure

## Comparison Table

| Feature | Manual Masking | Extension Methods | Logging Middleware | Custom Provider |
|---------|---------------|-------------------|-------------------|-----------------|
| Ease of Use | ?? | ???? | ????? | ??? |
| Explicitness | ????? | ???? | ?? | ?? |
| Performance | ????? | ???? | ??? | ???? |
| Flexibility | ????? | ???? | ??? | ????? |
| Maintainability | ?? | ???? | ??? | ??? |

## Related Documentation

- [FluentMasker README](../ITW.FluentMasker/README.md)
- [Serilog Integration](./SerilogIntegration.md)
- [Available Masking Rules](../ITW.FluentMasker/README.md#built-in-masking-rules)
- [Microsoft.Extensions.Logging Documentation](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)

## Conclusion

While standard `ILogger` doesn't support automatic destructuring like Serilog, FluentMasker can still be effectively integrated using several approaches:

1. **Manual Masking** - Simple and explicit, good for small projects
2. **Extension Methods** - Balance of convenience and explicitness
3. **Logging Middleware** - Transparent masking, but more complex
4. **Custom Provider** - Maximum control for specialized scenarios

Choose the approach that best fits your application's size, complexity, and requirements. For most applications, **Extension Methods** (Approach 2) provide the best balance of usability, explicitness, and maintainability.

**Recommendation:** Start with extension methods and consider middleware/custom providers only if you have specific requirements that justify the additional complexity.
