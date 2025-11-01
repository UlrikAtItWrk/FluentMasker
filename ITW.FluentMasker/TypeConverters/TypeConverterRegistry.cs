using System;
using System.Collections.Generic;

namespace ITW.FluentMasker.TypeConverters
{
    /// <summary>
    /// Registry for managing type converters.
    /// Allows registration and retrieval of converters for automatic type conversion during masking.
    /// </summary>
    public class TypeConverterRegistry
    {
        private readonly Dictionary<(Type, Type), object> _converters = new();
        private static readonly Lazy<TypeConverterRegistry> _instance =
            new Lazy<TypeConverterRegistry>(() => new TypeConverterRegistry());

        /// <summary>
        /// Gets the singleton instance of the TypeConverterRegistry.
        /// </summary>
        public static TypeConverterRegistry Instance => _instance.Value;

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// Registers built-in converters automatically.
        /// </summary>
        private TypeConverterRegistry()
        {
            RegisterBuiltInConverters();
        }

        /// <summary>
        /// Registers a type converter for the specified source and target types.
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TTarget">The target type</typeparam>
        /// <param name="converter">The converter instance to register</param>
        public void Register<TSource, TTarget>(ITypeConverter<TSource, TTarget> converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));

            _converters[(typeof(TSource), typeof(TTarget))] = converter;
        }

        /// <summary>
        /// Retrieves a type converter for the specified source and target types.
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TTarget">The target type</typeparam>
        /// <returns>The registered converter, or null if no converter is registered</returns>
        public ITypeConverter<TSource, TTarget>? Get<TSource, TTarget>()
        {
            return _converters.TryGetValue((typeof(TSource), typeof(TTarget)), out var converter)
                ? (ITypeConverter<TSource, TTarget>)converter
                : null;
        }

        /// <summary>
        /// Checks if a converter is registered for the specified source and target types.
        /// </summary>
        /// <typeparam name="TSource">The source type</typeparam>
        /// <typeparam name="TTarget">The target type</typeparam>
        /// <returns>True if a converter is registered, false otherwise</returns>
        public bool HasConverter<TSource, TTarget>()
        {
            return _converters.ContainsKey((typeof(TSource), typeof(TTarget)));
        }

        /// <summary>
        /// Registers all built-in converters.
        /// Called automatically during initialization.
        /// </summary>
        private void RegisterBuiltInConverters()
        {
            Register(new IntToStringConverter());
            Register(new DecimalToStringConverter());
            Register(new DoubleToStringConverter());
            Register(new DateTimeToStringConverter());
            Register(new DateTimeOffsetToStringConverter());
        }
    }
}
