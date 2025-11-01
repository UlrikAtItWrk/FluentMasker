using System;
using System.Globalization;

namespace ITW.FluentMasker.TypeConverters
{
    /// <summary>
    /// Converter for converting between DateTime and string types.
    /// Uses ISO 8601 format (roundtrip format "O") for consistent, unambiguous conversion.
    /// </summary>
    public class DateTimeToStringConverter : ITypeConverter<DateTime, string>
    {
        private const string DateTimeFormat = "O"; // ISO 8601 roundtrip format

        /// <summary>
        /// Converts a DateTime value to its ISO 8601 string representation.
        /// Format: yyyy-MM-ddTHH:mm:ss.fffffffK
        /// </summary>
        /// <param name="source">The DateTime value to convert</param>
        /// <returns>ISO 8601 string representation of the DateTime</returns>
        public string Convert(DateTime source)
        {
            return source.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts an ISO 8601 string value back to a DateTime.
        /// </summary>
        /// <param name="target">The string value to convert</param>
        /// <returns>The parsed DateTime value</returns>
        /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
        /// <exception cref="FormatException">Thrown when target is not a valid DateTime</exception>
        public DateTime ConvertBack(string target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return DateTime.Parse(target, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}
