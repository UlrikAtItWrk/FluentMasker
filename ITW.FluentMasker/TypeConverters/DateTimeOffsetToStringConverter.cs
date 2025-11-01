using System;
using System.Globalization;

namespace ITW.FluentMasker.TypeConverters
{
    /// <summary>
    /// Converter for converting between DateTimeOffset and string types.
    /// Uses ISO 8601 format (roundtrip format "O") for consistent, unambiguous conversion.
    /// Preserves timezone offset information.
    /// </summary>
    public class DateTimeOffsetToStringConverter : ITypeConverter<DateTimeOffset, string>
    {
        private const string DateTimeOffsetFormat = "O"; // ISO 8601 roundtrip format

        /// <summary>
        /// Converts a DateTimeOffset value to its ISO 8601 string representation.
        /// Format: yyyy-MM-ddTHH:mm:ss.fffffffzzz
        /// </summary>
        /// <param name="source">The DateTimeOffset value to convert</param>
        /// <returns>ISO 8601 string representation of the DateTimeOffset with timezone</returns>
        public string Convert(DateTimeOffset source)
        {
            return source.ToString(DateTimeOffsetFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts an ISO 8601 string value back to a DateTimeOffset.
        /// </summary>
        /// <param name="target">The string value to convert</param>
        /// <returns>The parsed DateTimeOffset value with timezone information</returns>
        /// <exception cref="ArgumentNullException">Thrown when target is null</exception>
        /// <exception cref="FormatException">Thrown when target is not a valid DateTimeOffset</exception>
        public DateTimeOffset ConvertBack(string target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return DateTimeOffset.Parse(target, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
    }
}
