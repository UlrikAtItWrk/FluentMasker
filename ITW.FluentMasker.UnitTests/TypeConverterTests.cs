using System;
using ITW.FluentMasker.TypeConverters;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    public class TypeConverterTests
    {
        #region IntToStringConverter Tests

        [Fact]
        public void IntToStringConverter_Convert_ReturnsCorrectString()
        {
            // Arrange
            var converter = new IntToStringConverter();
            int value = 12345;

            // Act
            string result = converter.Convert(value);

            // Assert
            Assert.Equal("12345", result);
        }

        [Fact]
        public void IntToStringConverter_ConvertBack_ReturnsCorrectInt()
        {
            // Arrange
            var converter = new IntToStringConverter();
            string value = "12345";

            // Act
            int result = converter.ConvertBack(value);

            // Assert
            Assert.Equal(12345, result);
        }

        [Fact]
        public void IntToStringConverter_RoundTrip_PreservesValue()
        {
            // Arrange
            var converter = new IntToStringConverter();
            int original = 67890;

            // Act
            string stringValue = converter.Convert(original);
            int roundTrip = converter.ConvertBack(stringValue);

            // Assert
            Assert.Equal(original, roundTrip);
        }

        [Fact]
        public void IntToStringConverter_ConvertBack_ThrowsOnNull()
        {
            // Arrange
            var converter = new IntToStringConverter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => converter.ConvertBack(null!));
        }

        #endregion

        #region DecimalToStringConverter Tests

        [Fact]
        public void DecimalToStringConverter_Convert_ReturnsCorrectString()
        {
            // Arrange
            var converter = new DecimalToStringConverter();
            decimal value = 123.456m;

            // Act
            string result = converter.Convert(value);

            // Assert
            Assert.Equal("123.456", result);
        }

        [Fact]
        public void DecimalToStringConverter_ConvertBack_ReturnsCorrectDecimal()
        {
            // Arrange
            var converter = new DecimalToStringConverter();
            string value = "123.456";

            // Act
            decimal result = converter.ConvertBack(value);

            // Assert
            Assert.Equal(123.456m, result);
        }

        [Fact]
        public void DecimalToStringConverter_RoundTrip_PreservesValue()
        {
            // Arrange
            var converter = new DecimalToStringConverter();
            decimal original = 9876.54321m;

            // Act
            string stringValue = converter.Convert(original);
            decimal roundTrip = converter.ConvertBack(stringValue);

            // Assert
            Assert.Equal(original, roundTrip);
        }

        [Fact]
        public void DecimalToStringConverter_ConvertBack_ThrowsOnNull()
        {
            // Arrange
            var converter = new DecimalToStringConverter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => converter.ConvertBack(null!));
        }

        #endregion

        #region DoubleToStringConverter Tests

        [Fact]
        public void DoubleToStringConverter_Convert_ReturnsCorrectString()
        {
            // Arrange
            var converter = new DoubleToStringConverter();
            double value = 123.456;

            // Act
            string result = converter.Convert(value);

            // Assert
            Assert.Equal("123.456", result);
        }

        [Fact]
        public void DoubleToStringConverter_ConvertBack_ReturnsCorrectDouble()
        {
            // Arrange
            var converter = new DoubleToStringConverter();
            string value = "123.456";

            // Act
            double result = converter.ConvertBack(value);

            // Assert
            Assert.Equal(123.456, result);
        }

        [Fact]
        public void DoubleToStringConverter_RoundTrip_PreservesValue()
        {
            // Arrange
            var converter = new DoubleToStringConverter();
            double original = 9876.54321;

            // Act
            string stringValue = converter.Convert(original);
            double roundTrip = converter.ConvertBack(stringValue);

            // Assert
            Assert.Equal(original, roundTrip);
        }

        [Fact]
        public void DoubleToStringConverter_ConvertBack_ThrowsOnNull()
        {
            // Arrange
            var converter = new DoubleToStringConverter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => converter.ConvertBack(null!));
        }

        #endregion

        #region DateTimeToStringConverter Tests

        [Fact]
        public void DateTimeToStringConverter_Convert_ReturnsCorrectString()
        {
            // Arrange
            var converter = new DateTimeToStringConverter();
            var value = new DateTime(2024, 10, 31, 14, 30, 45, DateTimeKind.Utc);

            // Act
            string result = converter.Convert(value);

            // Assert
            Assert.Contains("2024-10-31", result);
            Assert.Contains("14:30:45", result);
        }

        [Fact]
        public void DateTimeToStringConverter_ConvertBack_ReturnsCorrectDateTime()
        {
            // Arrange
            var converter = new DateTimeToStringConverter();
            string value = "2024-10-31T14:30:45.0000000Z";

            // Act
            DateTime result = converter.ConvertBack(value);

            // Assert
            Assert.Equal(2024, result.Year);
            Assert.Equal(10, result.Month);
            Assert.Equal(31, result.Day);
            Assert.Equal(14, result.Hour);
            Assert.Equal(30, result.Minute);
            Assert.Equal(45, result.Second);
        }

        [Fact]
        public void DateTimeToStringConverter_RoundTrip_PreservesValue()
        {
            // Arrange
            var converter = new DateTimeToStringConverter();
            var original = new DateTime(2024, 12, 25, 10, 15, 30, DateTimeKind.Utc);

            // Act
            string stringValue = converter.Convert(original);
            DateTime roundTrip = converter.ConvertBack(stringValue);

            // Assert
            Assert.Equal(original, roundTrip);
        }

        [Fact]
        public void DateTimeToStringConverter_ConvertBack_ThrowsOnNull()
        {
            // Arrange
            var converter = new DateTimeToStringConverter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => converter.ConvertBack(null!));
        }

        #endregion

        #region DateTimeOffsetToStringConverter Tests

        [Fact]
        public void DateTimeOffsetToStringConverter_Convert_ReturnsCorrectString()
        {
            // Arrange
            var converter = new DateTimeOffsetToStringConverter();
            var value = new DateTimeOffset(2024, 10, 31, 14, 30, 45, TimeSpan.FromHours(2));

            // Act
            string result = converter.Convert(value);

            // Assert
            Assert.Contains("2024-10-31", result);
            Assert.Contains("14:30:45", result);
        }

        [Fact]
        public void DateTimeOffsetToStringConverter_ConvertBack_ReturnsCorrectDateTimeOffset()
        {
            // Arrange
            var converter = new DateTimeOffsetToStringConverter();
            string value = "2024-10-31T14:30:45.0000000+02:00";

            // Act
            DateTimeOffset result = converter.ConvertBack(value);

            // Assert
            Assert.Equal(2024, result.Year);
            Assert.Equal(10, result.Month);
            Assert.Equal(31, result.Day);
            Assert.Equal(14, result.Hour);
            Assert.Equal(30, result.Minute);
            Assert.Equal(45, result.Second);
            Assert.Equal(TimeSpan.FromHours(2), result.Offset);
        }

        [Fact]
        public void DateTimeOffsetToStringConverter_RoundTrip_PreservesValue()
        {
            // Arrange
            var converter = new DateTimeOffsetToStringConverter();
            var original = new DateTimeOffset(2024, 12, 25, 10, 15, 30, TimeSpan.FromHours(-5));

            // Act
            string stringValue = converter.Convert(original);
            DateTimeOffset roundTrip = converter.ConvertBack(stringValue);

            // Assert
            Assert.Equal(original, roundTrip);
        }

        [Fact]
        public void DateTimeOffsetToStringConverter_ConvertBack_ThrowsOnNull()
        {
            // Arrange
            var converter = new DateTimeOffsetToStringConverter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => converter.ConvertBack(null!));
        }

        #endregion

        #region TypeConverterRegistry Tests

        [Fact]
        public void TypeConverterRegistry_Instance_ReturnsSingleton()
        {
            // Arrange & Act
            var instance1 = TypeConverterRegistry.Instance;
            var instance2 = TypeConverterRegistry.Instance;

            // Assert
            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void TypeConverterRegistry_HasBuiltInConverters()
        {
            // Arrange
            var registry = TypeConverterRegistry.Instance;

            // Act & Assert
            Assert.True(registry.HasConverter<int, string>());
            Assert.True(registry.HasConverter<decimal, string>());
            Assert.True(registry.HasConverter<double, string>());
            Assert.True(registry.HasConverter<DateTime, string>());
            Assert.True(registry.HasConverter<DateTimeOffset, string>());
        }

        [Fact]
        public void TypeConverterRegistry_Get_ReturnsRegisteredConverter()
        {
            // Arrange
            var registry = TypeConverterRegistry.Instance;

            // Act
            var converter = registry.Get<int, string>();

            // Assert
            Assert.NotNull(converter);
            Assert.IsType<IntToStringConverter>(converter);
        }

        [Fact]
        public void TypeConverterRegistry_Get_ReturnsNullForUnregisteredConverter()
        {
            // Arrange
            var registry = TypeConverterRegistry.Instance;

            // Act - Using a type pair that should never be registered
            var converter = registry.Get<Guid, byte[]>();

            // Assert
            Assert.Null(converter);
        }

        [Fact]
        public void TypeConverterRegistry_Register_CustomConverter_CanBeRetrieved()
        {
            // Arrange
            var registry = TypeConverterRegistry.Instance;
            var customConverter = new CustomTestConverter();

            // Act
            registry.Register(customConverter);
            var retrieved = registry.Get<bool, string>();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Same(customConverter, retrieved);
        }

        [Fact]
        public void TypeConverterRegistry_Register_ThrowsOnNullConverter()
        {
            // Arrange
            var registry = TypeConverterRegistry.Instance;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => registry.Register<int, string>(null!));
        }

        #endregion

        #region Helper Classes

        private class CustomTestConverter : ITypeConverter<bool, string>
        {
            public string Convert(bool source) => source.ToString();
            public bool ConvertBack(string target) => bool.Parse(target);
        }

        #endregion
    }
}
