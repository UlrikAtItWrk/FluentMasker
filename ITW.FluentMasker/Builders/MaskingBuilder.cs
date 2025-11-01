using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ITW.FluentMasker.Builders
{
    /// <summary>
    /// Foundation for fluent API with rule chaining.
    /// Accumulates mask rules in order and provides a fluent interface for building mask configurations.
    /// </summary>
    /// <typeparam name="TInput">The input type for the mask rules</typeparam>
    /// <typeparam name="TOutput">The output type for the mask rules</typeparam>
    public class MaskingBuilder<TInput, TOutput>
    {
        private readonly List<IMaskRule<TInput, TOutput>> _rules = new();

        /// <summary>
        /// Internal constructor to ensure instances are created through appropriate factory methods
        /// </summary>
        internal MaskingBuilder() { }

        /// <summary>
        /// Adds a mask rule to the builder.
        /// Rules are executed in the order they are added.
        /// </summary>
        /// <param name="rule">The mask rule to add</param>
        /// <returns>The builder instance for method chaining</returns>
        public MaskingBuilder<TInput, TOutput> AddRule(IMaskRule<TInput, TOutput> rule)
        {
            _rules.Add(rule);
            return this;
        }

        /// <summary>
        /// Builds and returns an immutable list of the accumulated rules.
        /// The returned list preserves the order in which rules were added.
        /// Each call to Build() returns a new independent snapshot of the current rules.
        /// </summary>
        /// <returns>A read-only list of mask rules</returns>
        public IReadOnlyList<IMaskRule<TInput, TOutput>> Build() => _rules.ToList().AsReadOnly();
    }

    /// <summary>
    /// Specialized builder for string masking rules.
    /// Provides a convenient way to chain string-based mask rules.
    /// </summary>
    public class StringMaskingBuilder : MaskingBuilder<string, string>
    {
        private object _pendingSeedProvider;

        /// <summary>
        /// Creates a new instance of the StringMaskingBuilder.
        /// </summary>
        public StringMaskingBuilder() { }

        /// <summary>
        /// Gets or sets the pending seed provider to be applied to the next seeded rule.
        /// This is used by the WithRandomSeed extension method.
        /// </summary>
        internal object PendingSeedProvider
        {
            get => _pendingSeedProvider;
            set => _pendingSeedProvider = value;
        }

        /// <summary>
        /// Adds a mask rule to the builder.
        /// Rules are executed in the order they are added.
        /// If a pending seed provider is set and the rule implements ISeededMaskRule, the seed provider is applied.
        /// </summary>
        /// <param name="rule">The mask rule to add</param>
        /// <returns>The builder instance for method chaining</returns>
        public new StringMaskingBuilder AddRule(IMaskRule<string, string> rule)
        {
            // Apply pending seed provider if rule supports seeding
            if (_pendingSeedProvider != null && rule is ISeededMaskRule<string> seededRule)
            {
                if (_pendingSeedProvider is SeedProvider<string> seedProvider)
                {
                    seededRule.SeedProvider = seedProvider;
                }
                _pendingSeedProvider = null; // Clear after applying
            }

            base.AddRule(rule);
            return this;
        }
    }

    /// <summary>
    /// Specialized builder for numeric masking rules.
    /// Provides a convenient way to chain numeric-based mask rules with seed provider support.
    /// </summary>
    /// <typeparam name="T">The numeric type (int, long, decimal, double, float, etc.)</typeparam>
    public class NumericMaskingBuilder<T> : MaskingBuilder<T, T>
        where T : struct, INumber<T>
    {
        private object _pendingSeedProvider;

        /// <summary>
        /// Creates a new instance of the NumericMaskingBuilder.
        /// </summary>
        public NumericMaskingBuilder() { }

        /// <summary>
        /// Gets or sets the pending seed provider to be applied to the next seeded rule.
        /// This is used by the WithRandomSeed extension method.
        /// </summary>
        internal object PendingSeedProvider
        {
            get => _pendingSeedProvider;
            set => _pendingSeedProvider = value;
        }

        /// <summary>
        /// Adds a numeric mask rule to the builder.
        /// Rules are executed in the order they are added.
        /// If a pending seed provider is set and the rule implements ISeededMaskRule, the seed provider is applied.
        /// </summary>
        /// <param name="rule">The mask rule to add</param>
        /// <returns>The builder instance for method chaining</returns>
        public new NumericMaskingBuilder<T> AddRule(IMaskRule<T, T> rule)
        {
            // Apply pending seed provider if rule supports seeding
            if (_pendingSeedProvider != null && rule is ISeededMaskRule<T> seededRule)
            {
                if (_pendingSeedProvider is SeedProvider<T> seedProvider)
                {
                    seededRule.SeedProvider = seedProvider;
                }
                _pendingSeedProvider = null; // Clear after applying
            }

            base.AddRule(rule);
            return this;
        }
    }

    /// <summary>
    /// Specialized builder for DateTime masking rules.
    /// Provides a convenient way to chain DateTime-based mask rules with seed provider support.
    /// </summary>
    public class DateTimeMaskingBuilder : MaskingBuilder<DateTime, DateTime>
    {
        private object _pendingSeedProvider;

        /// <summary>
        /// Creates a new instance of the DateTimeMaskingBuilder.
        /// </summary>
        public DateTimeMaskingBuilder() { }

        /// <summary>
        /// Gets or sets the pending seed provider to be applied to the next seeded rule.
        /// This is used by the WithRandomSeed extension method.
        /// </summary>
        internal object PendingSeedProvider
        {
            get => _pendingSeedProvider;
            set => _pendingSeedProvider = value;
        }

        /// <summary>
        /// Adds a DateTime mask rule to the builder.
        /// Rules are executed in the order they are added.
        /// If a pending seed provider is set and the rule implements ISeededMaskRule, the seed provider is applied.
        /// </summary>
        /// <param name="rule">The mask rule to add</param>
        /// <returns>The builder instance for method chaining</returns>
        public new DateTimeMaskingBuilder AddRule(IMaskRule<DateTime, DateTime> rule)
        {
            // Apply pending seed provider if rule supports seeding
            if (_pendingSeedProvider != null && rule is ISeededMaskRule<DateTime> seededRule)
            {
                if (_pendingSeedProvider is SeedProvider<DateTime> seedProvider)
                {
                    seededRule.SeedProvider = seedProvider;
                }
                _pendingSeedProvider = null; // Clear after applying
            }

            base.AddRule(rule);
            return this;
        }
    }
}
