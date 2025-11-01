using System;
using System.Globalization;

namespace ITW.FluentMasker.TypeConverters
{
    /// <summary>
    /// Converter for converting between double and string types.
    /// Uses InvariantCulture for consistent conversion across different locales.
    /// Preserves full precision during conversion.
    /// </summary>
    public class DoubleToStringConverter : ITypeConverter<double, string>
    {
        /// <summary>
        /// Converts a double value to its string representation.
        /// Uses "G17" format to preserve full precision (17 significant digits).
        /// </summary>
        /// <param name="source">The double value to convert</param>
        /// <returns>String representation of the double</returns>
        public string Convert(double source)
        {
            return source.ToString("G17", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a string value back to a double.
        /// </summary>
        /// <param name="target">The string value to convert</param>
        /// <returns>The parsed double value</returns>
        /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
        /// <exception cref="FormatException">Thrown when target is not a valid double</exception>
        public double ConvertBack(string target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return double.Parse(target, CultureInfo.InvariantCulture);
        }
    }
}
