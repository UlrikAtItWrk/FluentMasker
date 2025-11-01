using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ITW.FluentMasker
{
    // Keep existing interface for backward compatibility
    // Existing rules (MaskFirstRule, MaskLastRule) continue to work without modification
    public interface IMaskRule
    {
        string Apply(string input);
    }

    // New generic interface - enables type-safe masking for any type
    // TInput: The input type to be masked (e.g., int, DateTime, string)
    // TOutput: The output type after masking (usually same as TInput)
    public interface IMaskRule<TInput, TOutput>
    {
        TOutput Apply(TInput input);
    }

    // Bridge interface - allows string rules to implement both old and new interfaces
    // This provides a smooth migration path for string-based masking rules
    public interface IStringMaskRule : IMaskRule, IMaskRule<string, string>
    {
        // Explicit interface implementation routes old API calls to new generic implementation
        string IMaskRule.Apply(string input) => ((IMaskRule<string, string>)this).Apply(input);
    }

    // Specialized interfaces for different data types
    // These provide compile-time type safety and enable custom implementations per type

    /// <summary>
    /// Mask rule for numeric types (int, long, decimal, double, float, etc.)
    /// Uses INumber&lt;T&gt; constraint for generic numeric operations.
    /// </summary>
    /// <typeparam name="T">The numeric type (must implement INumber&lt;T&gt;)</typeparam>
    public interface INumericMaskRule<T> : IMaskRule<T, T>
        where T : struct, INumber<T>
    { }

    /// <summary>
    /// Mask rule for DateTime values
    /// </summary>
    public interface IDateTimeMaskRule : IMaskRule<DateTime, DateTime> { }

    /// <summary>
    /// Mask rule for DateTimeOffset values (includes timezone information)
    /// </summary>
    public interface IDateTimeOffsetMaskRule : IMaskRule<DateTimeOffset, DateTimeOffset> { }

    /// <summary>
    /// Delegate for providing deterministic seed values based on input.
    /// Used for deterministic masking where the same input should always produce the same masked output.
    /// </summary>
    /// <typeparam name="T">The type of input value</typeparam>
    /// <param name="value">The input value to generate a seed from</param>
    /// <returns>A seed value for random number generation</returns>
    /// <example>
    /// <code>
    /// SeedProvider&lt;decimal&gt; seedProvider = salary => salary.GetHashCode();
    /// </code>
    /// </example>
    public delegate int SeedProvider<T>(T value);

    /// <summary>
    /// Interface for mask rules that support deterministic seeding.
    /// When a seed provider is set, the rule will produce consistent output for the same input.
    /// </summary>
    /// <typeparam name="T">The type of input value</typeparam>
    /// <remarks>
    /// Deterministic masking is useful for:
    /// - HIPAA compliance (consistent date shifting per patient)
    /// - Reproducible analytics (same noise added to aggregated data)
    /// - Testing and debugging
    /// </remarks>
    public interface ISeededMaskRule<T>
    {
        /// <summary>
        /// Gets or sets the seed provider for deterministic masking.
        /// If null, the rule will use non-deterministic random masking.
        /// </summary>
        SeedProvider<T>? SeedProvider { get; set; }
    }
}
