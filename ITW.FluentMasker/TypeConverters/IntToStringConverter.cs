using System;
using System.Globalization;

namespace ITW.FluentMasker.TypeConverters
{
    /// <summary>
    /// Converter for converting between int and string types.
    /// Uses InvariantCulture for consistent conversion across different locales.
    /// </summary>
    public class IntToStringConverter : ITypeConverter<int, string>
    {
        /// <summary>
        /// Converts an integer value to its string representation.
        /// </summary>
        /// <param name="source">The integer value to convert</param>
        /// <returns>String representation of the integer</returns>
        public string Convert(int source)
        {
            return source.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a string value back to an integer.
        /// </summary>
        /// <param name="target">The string value to convert</param>
        /// <returns>The parsed integer value</returns>
        /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
        /// <exception cref="FormatException">Thrown when target is not a valid integer</exception>
        public int ConvertBack(string target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return int.Parse(target, CultureInfo.InvariantCulture);
        }
    }
}
