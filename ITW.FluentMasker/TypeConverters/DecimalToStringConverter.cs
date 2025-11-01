using System;
using System.Globalization;

namespace ITW.FluentMasker.TypeConverters
{
    /// <summary>
    /// Converter for converting between decimal and string types.
    /// Uses InvariantCulture for consistent conversion across different locales.
    /// Preserves full precision during conversion.
    /// </summary>
    public class DecimalToStringConverter : ITypeConverter<decimal, string>
    {
        /// <summary>
        /// Converts a decimal value to its string representation.
        /// Uses "G" format to preserve full precision.
        /// </summary>
        /// <param name="source">The decimal value to convert</param>
        /// <returns>String representation of the decimal</returns>
        public string Convert(decimal source)
        {
            return source.ToString("G", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a string value back to a decimal.
        /// </summary>
        /// <param name="target">The string value to convert</param>
        /// <returns>The parsed decimal value</returns>
        /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
        /// <exception cref="FormatException">Thrown when target is not a valid decimal</exception>
        public decimal ConvertBack(string target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return decimal.Parse(target, CultureInfo.InvariantCulture);
        }
    }
}
