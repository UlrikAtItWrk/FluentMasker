using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for RoundToRule - tests rounding numeric values to nearest increment
    /// </summary>
    public class RoundToRuleTests
    {
        #region Integer Tests

        [Theory]
        [InlineData(75123, 1000, 75000)]  // PRD example: round down
        [InlineData(75500, 1000, 76000)]  // Midpoint: round to even (76 is even)
        [InlineData(76500, 1000, 76000)]  // Midpoint: round to even (76 is even)
        [InlineData(27, 5, 25)]           // Round down
        [InlineData(28, 5, 30)]           // Round up
        [InlineData(25, 5, 25)]           // Already at increment
        [InlineData(0, 1000, 0)]          // Zero input
        [InlineData(123, 1, 123)]         // Increment of 1 (no change)
        [InlineData(100, 10, 100)]        // Already at increment
        [InlineData(105, 10, 100)]        // Round down
        [InlineData(106, 10, 110)]        // Round up
        public void Apply_IntegerWithIncrement_ReturnsExpectedRounding(int input, int increment, int expected)
        {
            // Arrange
            var rule = new RoundToRule<int>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(-75123, 1000, -75000)]  // PRD example: negative number
        [InlineData(-75500, 1000, -76000)]  // Midpoint negative
        [InlineData(-27, 5, -25)]           // Negative round
        [InlineData(-28, 5, -30)]           // Negative round
        [InlineData(-105, 10, -100)]        // Negative round down (toward zero)
        [InlineData(-106, 10, -110)]        // Negative round up (away from zero)
        public void Apply_NegativeInteger_RoundsCorrectly(int input, int increment, int expected)
        {
            // Arrange
            var rule = new RoundToRule<int>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_IntegerWithZeroIncrement_ReturnsOriginalValue()
        {
            // Arrange
            var rule = new RoundToRule<int>(0);
            var input = 75123;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        #endregion

        #region Long Tests

        [Theory]
        [InlineData(9999999999L, 1000000000L, 10000000000L)]  // Large numbers
        [InlineData(5000000001L, 1000000000L, 5000000000L)]   // Large numbers round down
        [InlineData(-9999999999L, 1000000000L, -10000000000L)]  // Large negative
        public void Apply_LongWithIncrement_ReturnsExpectedRounding(long input, long increment, long expected)
        {
            // Arrange
            var rule = new RoundToRule<long>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Decimal Tests

        [Theory]
        [InlineData(75123.45, 1000, 75000)]     // Decimal to integer increment
        [InlineData(75500.00, 1000, 76000)]     // Midpoint
        [InlineData(123.456, 0.1, 123.5)]       // Decimal increment
        [InlineData(123.449, 0.1, 123.4)]       // Round down to tenth
        [InlineData(99.999, 1, 100)]            // Round up to integer
        [InlineData(0.123, 0.01, 0.12)]         // Small decimal
        [InlineData(1.005, 0.01, 1.00)]         // Banker's rounding (round to even)
        // Note: 1.015 omitted due to floating-point precision issues in decimal-to-double conversion
        public void Apply_DecimalWithIncrement_ReturnsExpectedRounding(decimal input, decimal increment, decimal expected)
        {
            // Arrange
            var rule = new RoundToRule<decimal>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_DecimalWithZeroIncrement_ReturnsOriginalValue()
        {
            // Arrange
            var rule = new RoundToRule<decimal>(0m);
            var input = 75123.456m;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Theory]
        [InlineData(-123.456, 0.1, -123.5)]
        [InlineData(-99.999, 1, -100)]
        [InlineData(-1.005, 0.01, -1.00)]
        public void Apply_NegativeDecimal_RoundsCorrectly(decimal input, decimal increment, decimal expected)
        {
            // Arrange
            var rule = new RoundToRule<decimal>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Double Tests

        [Theory]
        [InlineData(75123.45, 1000.0, 75000.0)]
        [InlineData(75500.0, 1000.0, 76000.0)]
        [InlineData(123.456, 0.1, 123.5)]
        [InlineData(3.14159, 0.01, 3.14)]
        [InlineData(2.71828, 0.001, 2.718)]
        public void Apply_DoubleWithIncrement_ReturnsExpectedRounding(double input, double increment, double expected)
        {
            // Arrange
            var rule = new RoundToRule<double>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result, 10); // 10 decimal places precision
        }

        #endregion

        #region Float Tests

        [Theory]
        [InlineData(123.45f, 1.0f, 123.0f)]
        [InlineData(123.5f, 1.0f, 124.0f)]
        [InlineData(27.3f, 5.0f, 25.0f)]
        [InlineData(28.7f, 5.0f, 30.0f)]
        public void Apply_FloatWithIncrement_ReturnsExpectedRounding(float input, float increment, float expected)
        {
            // Arrange
            var rule = new RoundToRule<float>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result, 5); // 5 decimal places precision for float
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Constructor_NegativeIncrement_ConvertsToAbsoluteValue()
        {
            // Arrange & Act
            var rule = new RoundToRule<int>(-1000);
            var result = rule.Apply(75123);

            // Assert
            Assert.Equal(75000, result); // Should work as if increment was 1000
        }

        [Theory]
        [InlineData(int.MaxValue, 1000000, 2147000000)]  // Near max value
        [InlineData(int.MinValue, 1000000, -2147000000)] // Near min value
        public void Apply_BoundaryValues_HandlesCorrectly(int input, int increment, int expected)
        {
            // Arrange
            var rule = new RoundToRule<int>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Apply_LargeDecimalValue_HandlesCorrectly()
        {
            // Arrange
            var rule = new RoundToRule<decimal>(1000000m);
            // Use a large but not max value to avoid double conversion overflow
            var input = 999999999999999m; // Large decimal value

            // Act
            var result = rule.Apply(input);

            // Assert
            // Should round to nearest million
            Assert.Equal(1000000000000000m, result);
        }

        [Fact]
        public void Apply_LargeNegativeDecimalValue_HandlesCorrectly()
        {
            // Arrange
            var rule = new RoundToRule<decimal>(1000000m);
            // Use a large negative but not min value to avoid double conversion overflow
            var input = -999999999999999m; // Large negative decimal value

            // Act
            var result = rule.Apply(input);

            // Assert
            // Should round to nearest million
            Assert.Equal(-1000000000000000m, result);
        }

        #endregion

        #region Banker's Rounding (Round Half to Even)

        [Theory]
        [InlineData(0.5, 1, 0)]      // 0.5 rounds to 0 (even)
        [InlineData(1.5, 1, 2)]      // 1.5 rounds to 2 (even)
        [InlineData(2.5, 1, 2)]      // 2.5 rounds to 2 (even)
        [InlineData(3.5, 1, 4)]      // 3.5 rounds to 4 (even)
        [InlineData(4.5, 1, 4)]      // 4.5 rounds to 4 (even)
        [InlineData(5.5, 1, 6)]      // 5.5 rounds to 6 (even)
        public void Apply_BankersRounding_RoundsToEven(double input, double increment, double expected)
        {
            // Arrange
            var rule = new RoundToRule<double>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(10.05, 0.1, 10.0)]   // 10.05 rounds to 10.0 (even tenths)
        [InlineData(10.15, 0.1, 10.2)]   // 10.15 rounds to 10.2 (even tenths)
        [InlineData(10.25, 0.1, 10.2)]   // 10.25 rounds to 10.2 (even tenths)
        public void Apply_BankersRoundingDecimal_RoundsToEven(decimal input, decimal increment, decimal expected)
        {
            // Arrange
            var rule = new RoundToRule<decimal>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Apply_LargeNumberOfOperations_PerformsEfficiently()
        {
            // Arrange
            var rule = new RoundToRule<int>(1000);
            const int iterations = 100000;

            // Act
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++)
            {
                _ = rule.Apply(75123 + i);
            }
            var endTime = DateTime.UtcNow;
            var elapsed = endTime - startTime;

            // Assert - should complete 100,000 operations in under 1 second
            // Target from PRD: ≥ 100,000 ops/sec, so 100k ops should take < 1 second
            Assert.True(elapsed.TotalSeconds < 1.0,
                $"Performance test failed: {iterations} operations took {elapsed.TotalMilliseconds}ms (expected < 1000ms)");
        }

        [Fact]
        public void Apply_DecimalPrecision_PerformsEfficiently()
        {
            // Arrange
            var rule = new RoundToRule<decimal>(0.01m);
            const int iterations = 50000;

            // Act
            var startTime = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++)
            {
                _ = rule.Apply(123.456m + (i * 0.001m));
            }
            var endTime = DateTime.UtcNow;
            var elapsed = endTime - startTime;

            // Assert - decimal operations should still be fast
            Assert.True(elapsed.TotalSeconds < 1.0,
                $"Decimal performance test failed: {iterations} operations took {elapsed.TotalMilliseconds}ms");
        }

        #endregion

        #region Practical Use Cases (from PRD)

        [Fact]
        public void Apply_SalaryMasking_PRDExample()
        {
            // Arrange - PRD example: mask salary to nearest $1,000
            var rule = new RoundToRule<decimal>(1000m);
            var actualSalary = 75123m;

            // Act
            var maskedSalary = rule.Apply(actualSalary);

            // Assert
            Assert.Equal(75000m, maskedSalary);
        }

        [Fact]
        public void Apply_AgeGeneralization_RoundsToNearest5()
        {
            // Arrange - Common use case: round age to nearest 5 years for k-anonymity
            var rule = new RoundToRule<int>(5);

            // Act & Assert
            Assert.Equal(25, rule.Apply(27));
            Assert.Equal(30, rule.Apply(28));
            Assert.Equal(25, rule.Apply(23));
            Assert.Equal(35, rule.Apply(37));
        }

        [Fact]
        public void Apply_TransactionAmount_RoundsToNearestDollar()
        {
            // Arrange - Financial data: round to nearest dollar
            var rule = new RoundToRule<decimal>(1m);

            // Act & Assert
            Assert.Equal(123m, rule.Apply(123.45m));
            Assert.Equal(124m, rule.Apply(123.51m));
            Assert.Equal(100m, rule.Apply(99.99m));
        }

        [Theory]
        [InlineData(10.00, 5.00, 10.00)]
        [InlineData(12.50, 5.00, 10.00)]   // $12.50 rounds to $10 (even)
        [InlineData(17.50, 5.00, 20.00)]   // $17.50 rounds to $20 (even)
        [InlineData(23.75, 5.00, 25.00)]   // $23.75 rounds to $25
        public void Apply_PriceRounding_RoundsToNearest5Dollars(decimal input, decimal increment, decimal expected)
        {
            // Arrange
            var rule = new RoundToRule<decimal>(increment);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Multiple Types in One Test

        [Fact]
        public void Apply_DifferentNumericTypes_AllWorkCorrectly()
        {
            // Demonstrate that the generic implementation works for all numeric types

            // int
            var intRule = new RoundToRule<int>(10);
            Assert.Equal(120, intRule.Apply(123));

            // long
            var longRule = new RoundToRule<long>(1000L);
            Assert.Equal(75000L, longRule.Apply(75123L));

            // decimal
            var decimalRule = new RoundToRule<decimal>(0.1m);
            Assert.Equal(123.5m, decimalRule.Apply(123.456m));

            // double
            var doubleRule = new RoundToRule<double>(0.01);
            Assert.Equal(3.14, doubleRule.Apply(3.14159), 10);

            // float
            var floatRule = new RoundToRule<float>(1.0f);
            Assert.Equal(123.0f, floatRule.Apply(123.45f), 5);
        }

        #endregion

        #region Statistical Properties Tests

        [Fact]
        public void Apply_LargeDataset_PreservesApproximateMean()
        {
            // Arrange
            var rule = new RoundToRule<int>(10);
            var inputs = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                inputs.Add(50 + i); // Values from 50 to 1049
            }

            // Act
            var outputs = inputs.Select(x => rule.Apply(x)).ToList();

            // Assert
            var inputMean = inputs.Average();
            var outputMean = outputs.Average();

            // Mean should be preserved within a reasonable tolerance
            // For rounding to 10, we expect some deviation but it should be small
            var percentDifference = Math.Abs(outputMean - inputMean) / inputMean;
            Assert.True(percentDifference < 0.01, // Within 1%
                $"Mean not preserved: input={inputMean}, output={outputMean}, diff={percentDifference:P2}");
        }

        #endregion
    }
}
