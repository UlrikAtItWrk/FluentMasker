# FluentMasker + Serilog Integration Sample

This sample demonstrates how to integrate **FluentMasker** with **Serilog** using a custom `IDestructuringPolicy` to automatically mask sensitive data in your logs.

## Overview

This project shows a reusable pattern for masking PII (Personally Identifiable Information) and sensitive data when logging with Serilog. The masking happens transparently using Serilog's destructuring pipeline, ensuring sensitive data never reaches your log sinks.

## Key Components

### 1. `FluentMaskerPolicy.cs`
A custom `IDestructuringPolicy` implementation that intercepts objects during Serilog's destructuring phase and applies FluentMasker rules before logging.

```csharp
public sealed class FluentMaskerPolicy : IDestructuringPolicy
{
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        if (value is Person person)
        {
            var masker = new PersonMasker();
            var masked = masker.Mask(person);
            result = propertyValueFactory.CreatePropertyValue(masked.MaskedData, destructureObjects: true);
            return true;
        }
        
        // Handle other types...
        
        result = null!;
        return false;
    }
}
```

### 2. Model Classes
- **Person**: Demonstrates PII masking (email, phone, SSN, addresses, dates)
- **CreditCard**: Shows PCI-DSS compliant credit card masking
- **HealthRecord**: Demonstrates HIPAA-compliant health data masking

### 3. Masker Classes
Each model type has a corresponding masker that defines which FluentMasker rules to apply:

- **PersonMasker**: Masks personal information
- **CreditCardMasker**: Protects payment card data
- **HealthRecordMasker**: Anonymizes health records

## Usage

### Configuration

```csharp
// Configure Serilog with the FluentMaskerPolicy
Log.Logger = new LoggerConfiguration()
    .Destructure.With(new FluentMaskerPolicy())
    .WriteTo.Console()
    .CreateLogger();
```

### Logging with Masking

**Important**: Use the `{@Object}` syntax (destructuring) to trigger the policy:

```csharp
var person = new Person
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john.doe@example.com",
    Phone = "+1 (555) 123-4567",
    SSN = "123-45-6789"
};

// ? CORRECT - Triggers masking via IDestructuringPolicy
Log.Information("User registered: {@Person}", person);

// ? WRONG - Just calls ToString(), no masking!
Log.Information("User registered: {Person}", person);
```

## Masking Rules Demonstrated

### Person Masking
- **FirstName**: `MaskStart(2)` - Masks from start, keeps last chars
- **LastName**: `KeepFirst(1).KeepLast(1)` - Shows only first and last char
- **Email**: `EmailMaskRule` - Masks local part, keeps domain
- **Phone**: `PhoneMaskRule` - Preserves format, shows last 4 digits
- **SSN**: `RedactRule` - Completely redacted as `[REDACTED]`
- **Address**: `KeepFirst(4).KeepLast(3)` - Partial address
- **BirthDate**: `DateShiftRule` - Randomly shifted ±30 days
- **Age**: `NoiseAdditiveRule` - Adds random noise ±2 years

### Credit Card Masking (PCI-DSS)
- **CardNumber**: `CardMaskRule` - Shows first 4 and last 4 digits
- **CVV**: `RedactRule` - Never logged (always `***`)
- **CardHolderName**: `KeepFirst(3).KeepLast(3)` - Partial name
- **ExpiryDate**: `TimeBucketRule` - Rounded to day

### Health Record Masking (HIPAA)
- **PatientId**: `HashRule(SHA256)` - One-way hash for anonymization
- **Diagnosis**: `TruncateRule` - Limited to first 10 characters
- **Medication**: `MaskPercentageRule` - 50% masked from middle
- **LastVisit**: `TimeBucketRule` - Rounded to week buckets
- **BillingAmount**: `RoundToRule` - Rounded to nearest $100

## Running the Sample

1. Build and run the project
2. Observe the console output showing:
   - Original unmasked data (for comparison only)
   - Masked data as it appears in logs
   - Explanation of masking rules applied

## Example Output

```
EXAMPLE 1: Person Object Masking
Original data (NOT logged):
  Name: John Doe
  Email: john.doe@example.com
  Phone: +1 (555) 123-4567
  SSN: 123-45-6789

Logged with masking (using {@Person}):
[15:30:22] INF User registration: {"FirstName":"**hn","LastName":"D*e","Email":"jo**@example.com","Phone":"+* (***) ***-4567","SSN":"[REDACTED]","Address":"123************eld","BirthDate":"1990-05-18T00:00:00","Age":33}
```

## Extending the Sample

### Adding New Types

1. Create your model class
2. Create a masker class that inherits from `AbstractMasker<T>`
3. Add a handler in `FluentMaskerPolicy.TryDestructure`

```csharp
// In FluentMaskerPolicy.cs
if (value is Employee employee)
{
    var masker = new EmployeeMasker();
    var masked = masker.Mask(employee);
    result = propertyValueFactory.CreatePropertyValue(masked.MaskedData, destructureObjects: true);
    return true;
}
```

### Customizing Masking Rules

Edit the masker classes to adjust which rules are applied to which properties. See the FluentMasker README for all available rules.

## Benefits

- **Automatic**: Masking happens transparently at the logging layer
- **Reusable**: One policy applies to all log statements
- **Type-Safe**: Compiler-checked property expressions
- **Flexible**: Easy to customize rules per property
- **Compliant**: Supports GDPR, HIPAA, PCI-DSS requirements
- **Performance**: Uses compiled expression trees for fast property access

## References

- [FluentMasker GitHub](https://github.com/UlrikAtItWrk/FluentMasker)
- [Serilog Documentation](https://serilog.net/)
- [IDestructuringPolicy API](https://github.com/serilog/serilog/blob/dev/src/Serilog/Core/IDestructuringPolicy.cs)

## License

This sample code is provided as-is for demonstration purposes.
