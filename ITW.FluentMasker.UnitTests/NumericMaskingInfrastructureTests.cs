using System;
using System.Collections.Generic;
using System.Linq;
using ITW.FluentMasker.MaskRules;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for numeric masking infrastructure including INumericMaskRule,
    /// NumericMaskRuleBase, and seed provider functionality.
    /// </summary>
    public class NumericMaskingInfrastructureTests
    {
        #region Test Helper Classes

        /// <summary>
        /// Concrete implementation of NumericMaskRuleBase for testing purposes.
        /// Simply adds a fixed value to the input.
        /// </summary>
        private class TestNumericMaskRule<T> : NumericMaskRuleBase<T>
            where T : struct, System.Numerics.INumber<T>
        {
            private readonly T _addValue;

            public TestNumericMaskRule(T addValue)
            {
                _addValue = addValue;
            }

            public override T Apply(T input)
            {
                return input + _addValue;
            }
        }

        /// <summary>
        /// Test rule that uses the GetRandom method for seeding.
        /// Generates a random value between 0 and maxValue.
        /// </summary>
        private class TestRandomNumericMaskRule<T> : NumericMaskRuleBase<T>
            where T : struct, System.Numerics.INumber<T>
        {
            private readonly double _maxValue;

            public TestRandomNumericMaskRule(double maxValue)
            {
                _maxValue = maxValue;
            }

            public override T Apply(T input)
            {
                var rng = GetRandom(input);
                double randomValue = rng.NextDouble() * _maxValue;
                return input + T.CreateChecked(randomValue);
            }
        }

        #endregion

        #region Interface Compilation Tests

        [Fact]
        public void NumericMaskRule_SupportsIntType()
        {
            // Arrange & Act
            var rule = new TestNumericMaskRule<int>(10);
            var result = rule.Apply(5);

            // Assert
            Assert.Equal(15, result);
        }

        [Fact]
        public void NumericMaskRule_SupportsLongType()
        {
            // Arrange & Act
            var rule = new TestNumericMaskRule<long>(100L);
            var result = rule.Apply(50L);

            // Assert
            Assert.Equal(150L, result);
        }

        [Fact]
        public void NumericMaskRule_SupportsFloatType()
        {
            // Arrange & Act
            var rule = new TestNumericMaskRule<float>(1.5f);
            var result = rule.Apply(2.5f);

            // Assert
            Assert.Equal(4.0f, result, precision: 5);
        }

        [Fact]
        public void NumericMaskRule_SupportsDoubleType()
        {
            // Arrange & Act
            var rule = new TestNumericMaskRule<double>(10.5);
            var result = rule.Apply(5.5);

            // Assert
            Assert.Equal(16.0, result, precision: 10);
        }

        [Fact]
        public void NumericMaskRule_SupportsDecimalType()
        {
            // Arrange & Act
            var rule = new TestNumericMaskRule<decimal>(10.5m);
            var result = rule.Apply(5.5m);

            // Assert
            Assert.Equal(16.0m, result);
        }

        #endregion

        #region Seed Provider Tests

        [Fact]
        public void SeedProvider_ProducesDeterministicOutput_WithSameSeed()
        {
            // Arrange
            var rule = new TestRandomNumericMaskRule<decimal>(1000);
            rule.SeedProvider = value => 12345; // Fixed seed

            // Act
            var result1 = rule.Apply(75000m);
            var result2 = rule.Apply(75000m);

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void SeedProvider_ProducesDifferentOutput_WithDifferentSeeds()
        {
            // Arrange
            var rule1 = new TestRandomNumericMaskRule<decimal>(1000);
            rule1.SeedProvider = value => 12345;

            var rule2 = new TestRandomNumericMaskRule<decimal>(1000);
            rule2.SeedProvider = value => 54321;

            // Act
            var result1 = rule1.Apply(75000m);
            var result2 = rule2.Apply(75000m);

            // Assert - results should be different with different seeds
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void SeedProvider_BasedOnInputValue_ProducesConsistentOutput()
        {
            // Arrange
            var rule = new TestRandomNumericMaskRule<decimal>(1000);
            rule.SeedProvider = value => ToDouble(value).GetHashCode();

            decimal input = 75000m;

            // Act
            var result1 = rule.Apply(input);
            var result2 = rule.Apply(input);

            // Assert - same input with same seed provider should produce same output
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void SeedProvider_WhenNull_ProducesNonDeterministicOutput()
        {
            // Arrange
            var rule = new TestRandomNumericMaskRule<decimal>(1000);
            rule.SeedProvider = null; // Non-deterministic

            // Act - Apply multiple times
            var results = new List<decimal>();
            for (int i = 0; i < 10; i++)
            {
                results.Add(rule.Apply(75000m));
            }

            // Assert - at least some results should be different
            var uniqueResults = results.Distinct().Count();
            Assert.True(uniqueResults > 1, "Non-deterministic seeding should produce varying results");
        }

        #endregion

        #region Type Conversion Tests

        [Fact]
        public void ToDouble_ConvertsIntCorrectly()
        {
            // Arrange
            int value = 42;

            // Act
            double result = NumericMaskRuleBase<int>.ToDouble(value);

            // Assert
            Assert.Equal(42.0, result);
        }

        [Fact]
        public void FromDouble_ConvertsToIntCorrectly()
        {
            // Arrange
            double value = 42.7;

            // Act
            int result = NumericMaskRuleBase<int>.FromDouble(value);

            // Assert
            Assert.Equal(42, result); // Truncates decimal part
        }

        [Fact]
        public void ToDecimal_ConvertsDoubleCorrectly()
        {
            // Arrange
            double value = 123.456;

            // Act
            decimal result = NumericMaskRuleBase<double>.ToDecimal(value);

            // Assert
            Assert.Equal(123.456m, result, precision: 3);
        }

        [Fact]
        public void FromDecimal_ConvertsToDoubleCorrectly()
        {
            // Arrange
            decimal value = 123.456m;

            // Act
            double result = NumericMaskRuleBase<double>.FromDecimal(value);

            // Assert
            Assert.Equal(123.456, result, precision: 3);
        }

        [Fact]
        public void ToLong_ConvertsIntCorrectly()
        {
            // Arrange
            int value = 42;

            // Act
            long result = NumericMaskRuleBase<int>.ToLong(value);

            // Assert
            Assert.Equal(42L, result);
        }

        [Fact]
        public void FromLong_ConvertsToIntCorrectly()
        {
            // Arrange
            long value = 42L;

            // Act
            int result = NumericMaskRuleBase<int>.FromLong(value);

            // Assert
            Assert.Equal(42, result);
        }

        #endregion

        #region Statistical Validation Tests

        [Fact]
        public void CalculateMean_ComputesCorrectAverage()
        {
            // Arrange
            var values = new[] { 10, 20, 30, 40, 50 };

            // Act
            double mean = NumericMaskRuleBase<int>.CalculateMean(values);

            // Assert
            Assert.Equal(30.0, mean);
        }

        [Fact]
        public void CalculateMean_ThrowsOnEmptyCollection()
        {
            // Arrange
            var values = Array.Empty<int>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => NumericMaskRuleBase<int>.CalculateMean(values));
        }

        [Fact]
        public void CalculateStdDev_ComputesCorrectValue()
        {
            // Arrange
            var values = new[] { 2, 4, 4, 4, 5, 5, 7, 9 };

            // Act
            double stdDev = NumericMaskRuleBase<int>.CalculateStdDev(values);

            // Assert - Expected std dev is approximately 2.138
            Assert.Equal(2.138, stdDev, precision: 2);
        }

        [Fact]
        public void CalculateStdDev_ThrowsOnSingleValue()
        {
            // Arrange
            var values = new[] { 42 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => NumericMaskRuleBase<int>.CalculateStdDev(values));
        }

        [Fact]
        public void CalculateVariance_ComputesCorrectValue()
        {
            // Arrange
            var values = new[] { 2, 4, 4, 4, 5, 5, 7, 9 };

            // Act
            double variance = NumericMaskRuleBase<int>.CalculateVariance(values);

            // Assert - Variance is stdDev^2, so approximately 4.571
            Assert.Equal(4.571, variance, precision: 2);
        }

        [Fact]
        public void ValidateMeanPreservation_ReturnsTrueWhenWithinTolerance()
        {
            // Arrange
            double mean1 = 75000.0;
            double mean2 = 75005.0; // 0.0067% difference

            // Act
            bool isValid = NumericMaskRuleBase<decimal>.ValidateMeanPreservation(mean1, mean2, tolerancePercent: 0.01);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateMeanPreservation_ReturnsFalseWhenOutsideTolerance()
        {
            // Arrange
            double mean1 = 75000.0;
            double mean2 = 76000.0; // 1.33% difference

            // Act
            bool isValid = NumericMaskRuleBase<decimal>.ValidateMeanPreservation(mean1, mean2, tolerancePercent: 0.01);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateStdDevPreservation_ReturnsTrueWhenWithinTolerance()
        {
            // Arrange
            double stdDev1 = 15000.0;
            double stdDev2 = 15600.0; // 4% difference

            // Act
            bool isValid = NumericMaskRuleBase<decimal>.ValidateStdDevPreservation(stdDev1, stdDev2, tolerancePercent: 5.0);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateStdDevPreservation_ReturnsFalseWhenOutsideTolerance()
        {
            // Arrange
            double stdDev1 = 15000.0;
            double stdDev2 = 16000.0; // 6.67% difference

            // Act
            bool isValid = NumericMaskRuleBase<decimal>.ValidateStdDevPreservation(stdDev1, stdDev2, tolerancePercent: 5.0);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateMeanPreservation_HandlesZeroMeanCorrectly()
        {
            // Arrange
            double mean1 = 0.0;
            double mean2 = 0.005;

            // Act
            bool isValid = NumericMaskRuleBase<decimal>.ValidateMeanPreservation(mean1, mean2, tolerancePercent: 0.01);

            // Assert
            Assert.True(isValid); // Absolute difference check when mean is zero
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void NumericMaskRuleBase_IntegrationTest_WithSeedProvider()
        {
            // Arrange
            var rule = new TestRandomNumericMaskRule<decimal>(5000);
            rule.SeedProvider = salary => salary.GetHashCode();

            var salaries = new[] { 75000m, 80000m, 75000m, 90000m, 75000m };

            // Act
            var maskedSalaries = salaries.Select(s => rule.Apply(s)).ToList();

            // Assert
            // Same input salary (75000) should produce same masked output
            Assert.Equal(maskedSalaries[0], maskedSalaries[2]);
            Assert.Equal(maskedSalaries[0], maskedSalaries[4]);

            // Different input salaries should produce different masked outputs
            Assert.NotEqual(maskedSalaries[0], maskedSalaries[1]);
            Assert.NotEqual(maskedSalaries[0], maskedSalaries[3]);
        }

        [Fact]
        public void NumericMaskRuleBase_StatisticalPreservation_LargeDataset()
        {
            // Arrange
            var rule = new TestNumericMaskRule<decimal>(0); // No change rule for baseline
            var values = Enumerable.Range(1, 10000).Select(x => (decimal)x).ToList();

            // Act
            var maskedValues = values.Select(v => rule.Apply(v)).ToList();

            // Calculate statistics
            double inputMean = NumericMaskRuleBase<decimal>.CalculateMean(values);
            double outputMean = NumericMaskRuleBase<decimal>.CalculateMean(maskedValues);

            double inputStdDev = NumericMaskRuleBase<decimal>.CalculateStdDev(values);
            double outputStdDev = NumericMaskRuleBase<decimal>.CalculateStdDev(maskedValues);

            // Assert - no-op rule should preserve statistics exactly
            Assert.Equal(inputMean, outputMean, precision: 10);
            Assert.Equal(inputStdDev, outputStdDev, precision: 10);
        }

        #endregion

        #region Helper Methods for Tests

        /// <summary>
        /// Helper method to access protected ToDouble method
        /// </summary>
        private static double ToDouble<T>(T value) where T : struct, System.Numerics.INumber<T>
        {
            return NumericMaskRuleBase<T>.ToDouble(value);
        }

        #endregion
    }
}
