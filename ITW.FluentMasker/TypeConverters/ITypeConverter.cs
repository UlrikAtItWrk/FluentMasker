using System;

namespace ITW.FluentMasker.TypeConverters
{
    /// <summary>
    /// Interface for bidirectional type conversion between source and target types.
    /// Enables automatic conversion of property values during masking operations.
    /// </summary>
    /// <typeparam name="TSource">The source type to convert from</typeparam>
    /// <typeparam name="TTarget">The target type to convert to</typeparam>
    public interface ITypeConverter<TSource, TTarget>
    {
        /// <summary>
        /// Converts a value from source type to target type.
        /// </summary>
        /// <param name="source">The source value to convert</param>
        /// <returns>The converted value in target type</returns>
        TTarget Convert(TSource source);

        /// <summary>
        /// Converts a value from target type back to source type.
        /// Used for round-trip conversions and validation.
        /// </summary>
        /// <param name="target">The target value to convert back</param>
        /// <returns>The converted value in source type</returns>
        TSource ConvertBack(TTarget target);
    }
}
