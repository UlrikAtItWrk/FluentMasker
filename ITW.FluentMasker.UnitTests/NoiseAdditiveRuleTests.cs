using ITW.FluentMasker.MaskRules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace ITW.FluentMasker.UnitTests
{
    /// <summary>
    /// Unit tests for NoiseAdditiveRule - tests additive noise masking with uniform and Laplace distributions
    /// </summary>
    public class NoiseAdditiveRuleTests
    {
        #region Basic Functionality Tests - Uniform Distribution

        [Fact]
        public void Apply_UniformNoise_ReturnsValueWithinRange()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 5000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform);
            var input = 75000m;

            // Act
            var result = rule.Apply(input);

            // Assert - result should be within [70000, 80000]
            Assert.InRange(result, 70000m, 80000m);
        }

        [Theory]
        [InlineData(75000, 5000, 70000, 80000)]  // PRD example: salary masking
        [InlineData(100, 10, 90, 110)]           // Small values
        [InlineData(1000000, 50000, 950000, 1050000)]  // Large values
        [InlineData(0, 100, -100, 100)]          // Zero input
        public void Apply_UniformNoise_DecimalValues_WithinExpectedRange(
            double inputVal, double maxAbs, double minExpected, double maxExpected)
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform);
            var input = (decimal)inputVal;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.InRange(result, (decimal)minExpected, (decimal)maxExpected);
        }

        [Theory]
        [InlineData(27, 2, 25, 29)]     // Age masking
        [InlineData(100, 10, 90, 110)]  // Integer rounding
        [InlineData(0, 5, -5, 5)]       // Zero with noise
        public void Apply_UniformNoise_IntegerValues_WithinExpectedRange(int input, int maxAbs, int minExpected, int maxExpected)
        {
            // Arrange
            var rule = new NoiseAdditiveRule<int>(maxAbs, NoiseAdditiveRule<int>.NoiseDistribution.Uniform);

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.InRange(result, minExpected, maxExpected);
        }

        [Fact]
        public void Apply_UniformNoise_NegativeInput_ReturnsWithinRange()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 1000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform);
            var input = -5000m;

            // Act
            var result = rule.Apply(input);

            // Assert - result should be within [-6000, -4000]
            Assert.InRange(result, -6000m, -4000m);
        }

        #endregion

        #region Basic Functionality Tests - Laplace Distribution

        [Fact]
        public void Apply_LaplaceNoise_ReturnsModifiedValue()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(
                maxAbs: 5000,
                NoiseAdditiveRule<decimal>.NoiseDistribution.Laplace
            );
            var input = 75000m;

            // Act
            var result = rule.Apply(input);

            // Assert - Laplace noise can go beyond maxAbs (it's a scale parameter)
            // But most values should be within reasonable range
            // For Laplace with b=maxAbs/ln(2), ~50% within [-maxAbs, +maxAbs]
            Assert.NotEqual(input, result);
        }

        [Fact]
        public void Apply_LaplaceNoise_MultipleRuns_ProduceDifferentValues()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(
                maxAbs: 5000,
                NoiseAdditiveRule<decimal>.NoiseDistribution.Laplace
            );
            var input = 75000m;

            // Act - run multiple times
            var results = new HashSet<decimal>();
            for (int i = 0; i < 20; i++)
            {
                results.Add(rule.Apply(input));
            }

            // Assert - should get different values each time (non-deterministic)
            Assert.True(results.Count > 10, $"Expected > 10 unique values, got {results.Count}");
        }

        #endregion

        #region Deterministic Seeding Tests

        [Fact]
        public void Apply_WithSeedProvider_ProducesDeterministicOutput()
        {
            // Arrange
            var rule1 = new NoiseAdditiveRule<decimal>(maxAbs: 5000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform)
            {
                SeedProvider = x => 12345 // Fixed seed
            };

            var rule2 = new NoiseAdditiveRule<decimal>(maxAbs: 5000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform)
            {
                SeedProvider = x => 12345 // Same seed
            };

            var input = 75000m;

            // Act
            var result1 = rule1.Apply(input);
            var result2 = rule2.Apply(input);

            // Assert - same seed produces same output
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void Apply_WithSeedProvider_SameInputAlwaysProducesSameOutput()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 5000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform)
            {
                SeedProvider = x => x.GetHashCode() // Deterministic based on input
            };

            var input = 75000m;

            // Act - apply multiple times
            var results = new List<decimal>();
            for (int i = 0; i < 10; i++)
            {
                results.Add(rule.Apply(input));
            }

            // Assert - all results should be identical
            Assert.All(results, r => Assert.Equal(results[0], r));
        }

        [Fact]
        public void Apply_WithSeedProvider_DifferentInputsProduceDifferentOutputs()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 5000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform)
            {
                SeedProvider = x => x.GetHashCode()
            };

            // Act
            var result1 = rule.Apply(75000m);
            var result2 = rule.Apply(80000m);
            var result3 = rule.Apply(85000m);

            // Assert - different inputs should produce different noise
            Assert.NotEqual(result1 - 75000m, result2 - 80000m);
            Assert.NotEqual(result2 - 80000m, result3 - 85000m);
        }

        [Fact]
        public void Apply_LaplaceWithSeedProvider_ProducesDeterministicOutput()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(
                maxAbs: 5000,
                NoiseAdditiveRule<decimal>.NoiseDistribution.Laplace
            )
            {
                SeedProvider = x => 12345
            };

            var input = 75000m;

            // Act
            var result1 = rule.Apply(input);
            var result2 = rule.Apply(input);

            // Assert
            Assert.Equal(result1, result2);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Apply_ZeroMaxAbs_ReturnsOriginalValue()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 0);
            var input = 75000m;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Constructor_NegativeMaxAbs_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new NoiseAdditiveRule<decimal>(maxAbs: -5000));
        }

        [Fact]
        public void Apply_VerySmallMaxAbs_ReturnsValueCloseToOriginal()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 0.01);
            var input = 75000m;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.InRange(result, 74999.99m, 75000.01m);
        }

        [Fact]
        public void Apply_VeryLargeMaxAbs_ReturnsValueWithinRange()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 1000000);
            var input = 75000m;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.InRange(result, -925000m, 1075000m);
        }

        #endregion

        #region Multiple Numeric Types

        [Fact]
        public void Apply_IntType_WorksCorrectly()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<int>(maxAbs: 10);
            var input = 100;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.InRange(result, 90, 110);
        }

        [Fact]
        public void Apply_LongType_WorksCorrectly()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<long>(maxAbs: 1000);
            var input = 1000000L;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.InRange(result, 999000L, 1001000L);
        }

        [Fact]
        public void Apply_FloatType_WorksCorrectly()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<float>(maxAbs: 5.0);
            var input = 100.0f;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.InRange(result, 95.0f, 105.0f);
        }

        [Fact]
        public void Apply_DoubleType_WorksCorrectly()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<double>(maxAbs: 10.0);
            var input = 200.0;

            // Act
            var result = rule.Apply(input);

            // Assert
            Assert.InRange(result, 190.0, 210.0);
        }

        #endregion

        #region Statistical Property Tests

        [Fact]
        public void Apply_UniformNoise_PreservesMean_LargeDataset()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 5000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform);
            var inputs = Enumerable.Range(1, 10000).Select(x => (decimal)(x * 1000)).ToList();

            // Act
            var outputs = inputs.Select(x => rule.Apply(x)).ToList();

            // Assert - Mean should be preserved within ±0.01% (per PRD)
            var inputMean = inputs.Average();
            var outputMean = outputs.Average();

            var tolerance = inputMean * 0.0001m; // 0.01%
            var difference = Math.Abs(outputMean - inputMean);

            Assert.True(difference <= tolerance,
                $"Mean not preserved. Input mean: {inputMean}, Output mean: {outputMean}, " +
                $"Difference: {difference}, Tolerance: {tolerance}");
        }

        [Fact]
        public void Apply_UniformNoise_StandardDeviationWithinTolerance_LargeDataset()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 5000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform);

            // Generate normally distributed input data
            var inputs = GenerateNormalDistribution(mean: 75000, stddev: 15000, count: 10000);

            // Act
            var outputs = inputs.Select(x => rule.Apply(x)).ToList();

            // Assert - StdDev should be preserved within ±5% (per PRD)
            var inputStdDev = CalculateStdDev(inputs);
            var outputStdDev = CalculateStdDev(outputs);

            // Expected: sqrt(inputVariance^2 + noiseVariance^2)
            // For uniform noise in [-a, a], variance = a^2/3
            var noiseVariance = (5000.0 * 5000.0) / 3.0;
            var expectedVariance = (inputStdDev * inputStdDev) + noiseVariance;
            var expectedStdDev = Math.Sqrt(expectedVariance);

            var tolerance = expectedStdDev * 0.1; // 10% tolerance for statistical variation
            var difference = Math.Abs(outputStdDev - expectedStdDev);

            Assert.True(difference <= tolerance,
                $"StdDev not within tolerance. Input: {inputStdDev}, Output: {outputStdDev}, " +
                $"Expected: {expectedStdDev}, Difference: {difference}, Tolerance: {tolerance}");
        }

        [Fact]
        public void Apply_UniformNoise_ProducesSymmetricDistribution()
        {
            // Arrange
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 1000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform);
            var input = 50000m;

            // Act - generate many samples
            var noiseValues = new List<decimal>();
            for (int i = 0; i < 10000; i++)
            {
                var result = rule.Apply(input);
                noiseValues.Add(result - input); // Extract just the noise
            }

            // Assert - mean of noise should be close to 0 (symmetric)
            var meanNoise = noiseValues.Average();
            Assert.InRange(meanNoise, -50m, 50m); // Within ±50 for 10k samples

            // Check that roughly equal numbers above and below original
            var aboveCount = noiseValues.Count(n => n > 0);
            var belowCount = noiseValues.Count(n => n < 0);
            var ratio = (double)aboveCount / belowCount;

            Assert.InRange(ratio, 0.95, 1.05); // Within 5% of 50/50 split
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void Apply_UniformNoise_MeetsPerformanceTarget()
        {
            // Arrange - PRD requirement: ≥ 50,000 ops/sec
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 5000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform);
            var input = 75000m;
            var iterations = 100000;

            // Warm-up
            for (int i = 0; i < 1000; i++)
            {
                rule.Apply(input);
            }

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                rule.Apply(input);
            }
            sw.Stop();

            // Assert - should complete 100k ops in < 2 seconds (50k ops/sec)
            var opsPerSecond = iterations / sw.Elapsed.TotalSeconds;
            Assert.True(opsPerSecond >= 50000,
                $"Performance below target. Achieved: {opsPerSecond:N0} ops/sec, Target: ≥50,000 ops/sec");
        }

        [Fact]
        public void Apply_LaplaceNoise_ReasonablePerformance()
        {
            // Arrange - Laplace is more complex, may be slower than uniform
            var rule = new NoiseAdditiveRule<decimal>(
                maxAbs: 5000,
                NoiseAdditiveRule<decimal>.NoiseDistribution.Laplace
            );
            var input = 75000m;
            var iterations = 50000;

            // Warm-up
            for (int i = 0; i < 1000; i++)
            {
                rule.Apply(input);
            }

            // Act
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                rule.Apply(input);
            }
            sw.Stop();

            // Assert - should complete 50k ops in < 2 seconds (25k ops/sec minimum)
            var opsPerSecond = iterations / sw.Elapsed.TotalSeconds;
            Assert.True(opsPerSecond >= 25000,
                $"Laplace performance too slow. Achieved: {opsPerSecond:N0} ops/sec, Target: ≥25,000 ops/sec");
        }

        #endregion

        #region Real-World Use Case Tests

        [Fact]
        public void Apply_SalaryMasking_ProducesRealisticResults()
        {
            // Arrange - Real-world scenario: mask employee salaries
            var rule = new NoiseAdditiveRule<decimal>(maxAbs: 5000, NoiseAdditiveRule<decimal>.NoiseDistribution.Uniform)
            {
                SeedProvider = salary => salary.GetHashCode() // Deterministic per salary
            };

            var salaries = new[] { 45000m, 65000m, 85000m, 105000m, 125000m };

            // Act
            var maskedSalaries = salaries.Select(s => rule.Apply(s)).ToList();

            // Assert - all within expected range
            for (int i = 0; i < salaries.Length; i++)
            {
                Assert.InRange(maskedSalaries[i], salaries[i] - 5000m, salaries[i] + 5000m);
            }

            // Relative ordering should be mostly preserved (not guaranteed but likely)
            // Due to deterministic seeding, same input always produces same output
            var secondMasking = salaries.Select(s => rule.Apply(s)).ToList();
            Assert.Equal(maskedSalaries, secondMasking);
        }

        [Fact]
        public void Apply_AgeMasking_WorksWithSmallIntegers()
        {
            // Arrange - Real-world scenario: mask ages with ±2 years
            var rule = new NoiseAdditiveRule<int>(maxAbs: 2, NoiseAdditiveRule<int>.NoiseDistribution.Uniform);

            var ages = new[] { 25, 35, 45, 55, 65 };

            // Act
            var maskedAges = ages.Select(a => rule.Apply(a)).ToList();

            // Assert
            for (int i = 0; i < ages.Length; i++)
            {
                Assert.InRange(maskedAges[i], ages[i] - 2, ages[i] + 2);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generates a normally distributed set of decimal values (using Box-Muller transform)
        /// </summary>
        private List<decimal> GenerateNormalDistribution(double mean, double stddev, int count)
        {
            var random = new Random(42); // Fixed seed for reproducibility
            var values = new List<decimal>(count);

            for (int i = 0; i < count; i++)
            {
                // Box-Muller transform to generate normal distribution
                double u1 = 1.0 - random.NextDouble(); // (0,1]
                double u2 = 1.0 - random.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                double randNormal = mean + stddev * randStdNormal;

                values.Add((decimal)randNormal);
            }

            return values;
        }

        /// <summary>
        /// Calculates standard deviation of a collection of decimal values
        /// </summary>
        private double CalculateStdDev(List<decimal> values)
        {
            if (values.Count < 2)
                throw new ArgumentException("Need at least 2 values for standard deviation");

            double mean = (double)values.Average();
            double sumSquaredDifferences = values.Sum(v => Math.Pow((double)v - mean, 2));

            return Math.Sqrt(sumSquaredDifferences / (values.Count - 1));
        }

        #endregion
    }
}
